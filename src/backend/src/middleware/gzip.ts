import type { MiddlewareHandler } from "hono";
import { gunzipSync } from "node:zlib";

export const MAX_COMPRESSED_BYTES = 10 * 1024 * 1024;
export const MAX_DECOMPRESSED_BYTES = 10 * 1024 * 1024;

/**
 * BrainRequestCompression middleware:
 * 1. Always sets `x-clicky-accepts-gzip: 1` on every response so the C# client
 *    (`BrainRequestCompression.ObserveResponse`) knows it can compress future payloads.
 * 2. Automatically decompresses incoming requests with `Content-Encoding: gzip`
 *    so downstream `c.req.json()` / `c.req.text()` / `c.req.arrayBuffer()` read
 *    the decompressed body transparently, while enforcing 10 MB compressed and
 *    10 MB decompressed payload caps (`AUD-C-002` / `AUD-L-002`).
 */
export const gzipMiddleware: MiddlewareHandler = async (c, next) => {
  c.header("x-clicky-accepts-gzip", "1");

  const contentEncoding = c.req.header("content-encoding")?.toLowerCase() ?? "";
  if (contentEncoding.includes("gzip")) {
    const contentLengthHeader = c.req.header("content-length");
    if (
      contentLengthHeader &&
      Number(contentLengthHeader) > MAX_COMPRESSED_BYTES
    ) {
      return c.json({ error: "Compressed payload too large" }, 413);
    }

    try {
      const rawBuffer = Buffer.from(await c.req.raw.arrayBuffer());
      if (rawBuffer.byteLength > MAX_COMPRESSED_BYTES) {
        return c.json({ error: "Compressed payload too large" }, 413);
      }

      if (rawBuffer.byteLength > 0) {
        const decompressed = gunzipSync(rawBuffer, {
          maxOutputLength: MAX_DECOMPRESSED_BYTES,
        });
        const headers = new Headers(c.req.raw.headers);
        headers.delete("content-encoding");
        headers.set("content-length", String(decompressed.byteLength));

        const decompressedRequest = new Request(c.req.raw.url, {
          method: c.req.raw.method,
          headers,
          body: decompressed,
        });

        // Replace Hono's raw Request with the decompressed Request
        Object.defineProperty(c.req, "raw", {
          value: decompressedRequest,
          writable: true,
          configurable: true,
        });
      }
    } catch (err: any) {
      if (
        err?.code === "ERR_BUFFER_TOO_LARGE" ||
        String(err?.message ?? "")
          .toLowerCase()
          .includes("maxoutputlength") ||
        String(err?.message ?? "")
          .toLowerCase()
          .includes("buffer is too large")
      ) {
        return c.json(
          { error: "Decompressed payload exceeds 10MB limit" },
          413
        );
      }
      return c.json(
        {
          error: "invalid_gzip_payload",
          message:
            err instanceof Error ? err.message : "Failed to gunzip request body",
        },
        400
      );
    }
  }

  await next();
  // Ensure header persists even if route overwrote headers
  c.header("x-clicky-accepts-gzip", "1");
};
