import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import readline from "node:readline";

export interface OcrWordBox {
  text: string;
  x: number;
  y: number;
  w: number;
  h: number;
}

export interface OcrLineBox {
  id: string;
  text: string;
  x: number;
  y: number;
  w: number;
  h: number;
  rightX: number;
  bottomY: number;
  cy: number;
  words: OcrWordBox[];
}

export interface OcrScreenResult {
  width: number;
  height: number;
  lines: OcrLineBox[];
  elapsedMs: number;
}

const MAX_CACHE_ENTRIES = 32;
const ocrCache = new Map<string, OcrScreenResult>();
const inFlightOcr = new Map<string, Promise<OcrScreenResult | null>>();

function computeOcrCacheKey(imageDataUrl: string): string {
  const trimmed = imageDataUrl.trim();
  return `${trimmed.length}_${trimmed.slice(-120)}`;
}

export function getCachedWindowsOcr(imageDataUrl: string): OcrScreenResult | null {
  if (!imageDataUrl) return null;
  const key = computeOcrCacheKey(imageDataUrl);
  return ocrCache.get(key) ?? null;
}

function setCachedWindowsOcr(imageDataUrl: string, result: OcrScreenResult): void {
  const key = computeOcrCacheKey(imageDataUrl);
  if (ocrCache.has(key)) {
    ocrCache.delete(key);
  } else if (ocrCache.size >= MAX_CACHE_ENTRIES) {
    const oldestKey = ocrCache.keys().next().value;
    if (oldestKey !== undefined) {
      ocrCache.delete(oldestKey);
    }
  }
  ocrCache.set(key, result);
}

interface PendingOcrRequest {
  resolve: (raw: any) => void;
  reject: (err: Error) => void;
  timer: NodeJS.Timeout;
}

let daemonProc: ChildProcessWithoutNullStreams | null = null;
let daemonRl: readline.Interface | null = null;
let daemonReadyPromise: Promise<boolean> | null = null;
let daemonIsReady = false;
let reqSeq = 0;
let ocrDispatchChain: Promise<void> = Promise.resolve();
const pendingRequests = new Map<string, PendingOcrRequest>();

const POWERSHELL_OCR_DAEMON_SCRIPT = `
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Storage.StorageFile, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Storage.Streams.RandomAccessStream, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Globalization.Language, Windows.Foundation, ContentType = WindowsRuntime]

$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
  $_.Name -eq "AsTask" -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name.StartsWith("IAsyncOperation")
})[0]

function Await($WinRtTask, $ResultType) {
  $asTask = $asTaskGeneric.MakeGenericMethod($ResultType)
  $netTask = $asTask.Invoke($null, @($WinRtTask))
  $netTask.Wait(-1) | Out-Null
  return ,$netTask.Result
}

$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
if ($null -eq $engine) {
  $lang = New-Object Windows.Globalization.Language("en-US")
  $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage($lang)
}

[Console]::Out.WriteLine('{"ready":true}')
[Console]::Out.Flush()

while (($reqLine = [Console]::In.ReadLine()) -ne $null) {
  $reqLine = $reqLine.Trim()
  if ($reqLine.Length -eq 0) { continue }
  $reqId = ""
  $imgPath = ""
  try {
    $parts = $reqLine.Split([char]9, 2)
    if ($parts.Length -lt 2) { continue }
    $reqId = $parts[0]
    $imgPath = $parts[1]
    if (-not [System.IO.File]::Exists($imgPath)) {
      [Console]::Out.WriteLine('{"id":"' + $reqId + '","error":"File not found"}')
      [Console]::Out.Flush()
      continue
    }

    $fsStream = $null
    $winrtStream = $null
    $decoder = $null
    $bitmap = $null
    $ocrResult = $null
    $pw = 1280
    $ph = 720
    try {
      $fsStream = [System.IO.File]::OpenRead($imgPath)
      $winrtStream = [System.IO.WindowsRuntimeStreamExtensions]::AsRandomAccessStream($fsStream)
      $decoder = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($winrtStream)) ([Windows.Graphics.Imaging.BitmapDecoder])
      $bitmap = Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
      $pw = [Math]::Max(1, [int]$bitmap.PixelWidth)
      $ph = [Math]::Max(1, [int]$bitmap.PixelHeight)
      $ocrResult = Await ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
    } finally {
      if ($null -ne $bitmap) {
        try { $bitmap.Dispose() } catch {}
      }
      if ($null -ne $winrtStream) {
        try { $winrtStream.Dispose() } catch {}
      }
      if ($null -ne $fsStream) {
        try { $fsStream.Dispose() } catch {}
      }
      if ($imgPath.Length -gt 0) {
        try { [System.IO.File]::Delete($imgPath) } catch {}
      }
    }

    $sx = 1280.0 / [double]$pw
    $sy = 720.0 / [double]$ph
    $sb = New-Object System.Text.StringBuilder(16384)
    [void]$sb.Append('{"id":"').Append($reqId).Append('","w":1280,"h":720,"lines":[')

    if ($null -ne $ocrResult -and $null -ne $ocrResult.Lines) {
      $lineIdx = 0
      foreach ($line in $ocrResult.Lines) {
        $wordsArr = $line.Words
        if ($wordsArr.Count -eq 0) { continue }
        if ($lineIdx -gt 0) { [void]$sb.Append(',') }
        $lineIdx++
        $minX = 999999
        $minY = 999999
        $maxR = 0
        $maxB = 0
        $wsb = New-Object System.Text.StringBuilder(512)
        $wIdx = 0
        foreach ($w in $wordsArr) {
          $rect = $w.BoundingRect
          $wx = [int][Math]::Round([double]$rect.X * $sx)
          $wy = [int][Math]::Round([double]$rect.Y * $sy)
          $wr = [int][Math]::Round(([double]$rect.X + [double]$rect.Width) * $sx)
          $wb = [int][Math]::Round(([double]$rect.Y + [double]$rect.Height) * $sy)
          $ww = [Math]::Max(1, $wr - $wx)
          $wh = [Math]::Max(1, $wb - $wy)
          if ($wx -lt $minX) { $minX = $wx }
          if ($wy -lt $minY) { $minY = $wy }
          if ($wr -gt $maxR) { $maxR = $wr }
          if ($wb -gt $maxB) { $maxB = $wb }
          if ($wIdx -gt 0) { [void]$wsb.Append(',') }
          $wIdx++
          $wt = ([string]$w.Text).Replace('\\', '\\\\').Replace('"', '\\"')
          [void]$wsb.Append('{"text":"').Append($wt).Append('","x":').Append($wx).Append(',"y":').Append($wy).Append(',"w":').Append($ww).Append(',"h":').Append($wh).Append('}')
        }
        $lw = [Math]::Max(1, $maxR - $minX)
        $lh = [Math]::Max(1, $maxB - $minY)
        $lcy = [int][Math]::Round(($minY + $maxB) / 2.0)
        $lt = ([string]$line.Text).Replace('\\', '\\\\').Replace('"', '\\"')
        [void]$sb.Append('{"id":"OCR_').Append($lineIdx).Append('","text":"').Append($lt).Append('","x":').Append($minX).Append(',"y":').Append($minY).Append(',"w":').Append($lw).Append(',"h":').Append($lh).Append(',"rightX":').Append($maxR).Append(',"bottomY":').Append($maxB).Append(',"cy":').Append($lcy).Append(',"words":[').Append($wsb.ToString()).Append(']}')
      }
    }

    [void]$sb.Append(']}')
    [Console]::Out.WriteLine($sb.ToString())
    [Console]::Out.Flush()
  } catch {
    $em = ([string]$_.Exception.Message).Replace('\\', '\\\\').Replace('"', '\\"')
    [Console]::Out.WriteLine('{"id":"' + $reqId + '","error":"' + $em + '"}')
    [Console]::Out.Flush()
  }
}
`;

function updateDaemonRefState(): void {
  if (!daemonProc) return;
  if (pendingRequests.size > 0 || !daemonIsReady) {
    daemonProc.ref();
    (daemonProc.stdout as any)?.ref?.();
    (daemonProc.stderr as any)?.ref?.();
  } else {
    daemonProc.unref();
    (daemonProc.stdout as any)?.unref?.();
    (daemonProc.stderr as any)?.unref?.();
  }
}

let daemonScriptPath: string | null = null;

function cleanupDaemon(): void {
  daemonIsReady = false;
  daemonReadyPromise = null;
  if (daemonRl) {
    try {
      daemonRl.close();
    } catch {
      // ignore
    }
    daemonRl = null;
  }
  if (daemonProc) {
    try {
      daemonProc.stdin?.destroy();
      daemonProc.stdout?.destroy();
      daemonProc.stderr?.destroy();
      daemonProc.unref();
      daemonProc.kill();
    } catch {
      // ignore
    }
    daemonProc = null;
  }
  if (daemonScriptPath) {
    try {
      fs.unlinkSync(daemonScriptPath);
    } catch {
      // ignore
    }
    daemonScriptPath = null;
  }
  for (const [, pending] of pendingRequests) {
    clearTimeout(pending.timer);
    pending.reject(new Error("WinRT OCR daemon exited"));
  }
  pendingRequests.clear();
}

export function closeWinOcrDaemon(): void {
  cleanupDaemon();
}

function ensureWinOcrDaemon(): Promise<boolean> {
  if (process.platform !== "win32") {
    return Promise.resolve(false);
  }
  if (daemonProc && daemonIsReady) {
    return Promise.resolve(true);
  }
  if (daemonReadyPromise) {
    return daemonReadyPromise;
  }

  daemonReadyPromise = new Promise<boolean>((resolveReady) => {
    try {
      const scriptPath = path.join(
        os.tmpdir(),
        `kursor_win_ocr_daemon_${process.pid}.ps1`
      );
      daemonScriptPath = scriptPath;
      fs.writeFileSync(scriptPath, POWERSHELL_OCR_DAEMON_SCRIPT, "utf8");

      const child = spawn(
        "powershell.exe",
        [
          "-NoProfile",
          "-NonInteractive",
          "-Mta",
          "-ExecutionPolicy",
          "Bypass",
          "-File",
          scriptPath,
        ],
        {
          stdio: ["pipe", "pipe", "pipe"],
          windowsHide: true,
        }
      );
      daemonProc = child;
      child.stderr.on("data", () => {});

      const startupTimer = setTimeout(() => {
        if (!daemonIsReady) {
          cleanupDaemon();
          resolveReady(false);
        }
      }, 20000);

      const rl = readline.createInterface({
        input: child.stdout,
        crlfDelay: Infinity,
      });
      daemonRl = rl;

      rl.on("line", (line) => {
        const trimmed = line.trim();
        if (!trimmed.startsWith("{")) return;
        try {
          const msg = JSON.parse(trimmed);
          if (msg.ready === true) {
            daemonIsReady = true;
            clearTimeout(startupTimer);
            updateDaemonRefState();
            resolveReady(true);
            return;
          }
          if (typeof msg.id === "string") {
            const pending = pendingRequests.get(msg.id);
            if (pending) {
              clearTimeout(pending.timer);
              pendingRequests.delete(msg.id);
              updateDaemonRefState();
              pending.resolve(msg);
            }
          }
        } catch {
          // ignore malformed stdout line
        }
      });

      child.on("error", () => {
        clearTimeout(startupTimer);
        cleanupDaemon();
        resolveReady(false);
      });

      child.on("exit", () => {
        clearTimeout(startupTimer);
        cleanupDaemon();
        resolveReady(false);
      });
    } catch {
      cleanupDaemon();
      resolveReady(false);
    }
  });

  return daemonReadyPromise;
}

export function prewarmWinOcrDaemon(): void {
  void ensureWinOcrDaemon();
}

export async function runWindowsOcr(
  imageDataUrl: string
): Promise<OcrScreenResult | null> {
  if (!imageDataUrl || process.platform !== "win32") return null;

  const cached = getCachedWindowsOcr(imageDataUrl);
  if (cached) return cached;

  const cacheKey = computeOcrCacheKey(imageDataUrl);
  const existingInFlight = inFlightOcr.get(cacheKey);
  if (existingInFlight) return existingInFlight;

  const task = (async (): Promise<OcrScreenResult | null> => {
    const startMs = performance.now();
    const ready = await ensureWinOcrDaemon();
    if (!ready || !daemonProc) return null;

    const trimmed = imageDataUrl.trim();
    const mimeMatch = trimmed.match(/^data:image\/([a-zA-Z0-9.+-]+);base64,/);
    const isPng = mimeMatch ? mimeMatch[1].toLowerCase().includes("png") : false;
    const base64 = trimmed.replace(/^data:image\/[a-zA-Z0-9.+-]+;base64,/, "");
    if (!base64) return null;

    const buf = Buffer.from(base64, "base64");
    if (buf.length < 16 || buf.length > 10 * 1024 * 1024) return null;

    const ext =
      isPng || (buf[0] === 0x89 && buf[1] === 0x50 && buf[2] === 0x4e && buf[3] === 0x47)
        ? "png"
        : "jpg";
    const id = `${Date.now()}_${++reqSeq}`;
    const tmpFile = path.join(os.tmpdir(), `kursor_ocr_${id}.${ext}`);
    let timedOutInFlight = false;

    try {
      fs.writeFileSync(tmpFile, buf);

      const rawResp = await new Promise<any>((resolve, reject) => {
        ocrDispatchChain = ocrDispatchChain
          .catch(() => {})
          .then(async () => {
            const daemonReady = await ensureWinOcrDaemon();
            if (!daemonReady || !daemonProc) {
              reject(new Error("WinRT OCR daemon not available"));
              return;
            }
            await new Promise<void>((resolveSlot) => {
              const timer = setTimeout(() => {
                timedOutInFlight = true;
                pendingRequests.delete(id);
                cleanupDaemon();
                resolveSlot();
                reject(new Error("WinRT OCR request timed out after 15000ms"));
              }, 15000);

              pendingRequests.set(id, {
                resolve: (msg) => {
                  resolveSlot();
                  resolve(msg);
                },
                reject: (err) => {
                  resolveSlot();
                  reject(err);
                },
                timer,
              });
              updateDaemonRefState();
              try {
                daemonProc!.stdin.write(`${id}\t${tmpFile}\n`);
              } catch (err) {
                clearTimeout(timer);
                pendingRequests.delete(id);
                updateDaemonRefState();
                resolveSlot();
                reject(err instanceof Error ? err : new Error(String(err)));
              }
            });
          });
      });

      if (!rawResp || rawResp.error || !Array.isArray(rawResp.lines)) {
        return null;
      }

      const lines: OcrLineBox[] = rawResp.lines.map((l: any, idx: number) => ({
        id: typeof l.id === "string" ? l.id : `OCR_${idx + 1}`,
        text: String(l.text ?? "").trim(),
        x: Number(l.x ?? 0),
        y: Number(l.y ?? 0),
        w: Number(l.w ?? 1),
        h: Number(l.h ?? 1),
        rightX: Number(l.rightX ?? Number(l.x ?? 0) + Number(l.w ?? 1)),
        bottomY: Number(l.bottomY ?? Number(l.y ?? 0) + Number(l.h ?? 1)),
        cy: Number(l.cy ?? Math.round(Number(l.y ?? 0) + Number(l.h ?? 1) / 2)),
        words: Array.isArray(l.words)
          ? l.words.map((w: any) => ({
              text: String(w.text ?? ""),
              x: Number(w.x ?? 0),
              y: Number(w.y ?? 0),
              w: Number(w.w ?? 1),
              h: Number(w.h ?? 1),
            }))
          : [],
      }));

      lines.sort((a, b) => {
        if (Math.abs(a.cy - b.cy) <= 8) return a.x - b.x;
        return a.cy - b.cy;
      });

      const elapsedMs = Math.round(performance.now() - startMs);
      const result: OcrScreenResult = {
        width: 1280,
        height: 720,
        lines,
        elapsedMs,
      };
      setCachedWindowsOcr(imageDataUrl, result);
      return result;
    } catch {
      return null;
    } finally {
      if (!timedOutInFlight) {
        try {
          if (fs.existsSync(tmpFile)) {
            await fs.promises.unlink(tmpFile);
          }
        } catch {
          // ignore
        }
      }
    }
  })();

  inFlightOcr.set(cacheKey, task);
  try {
    return await task;
  } finally {
    inFlightOcr.delete(cacheKey);
  }
}
