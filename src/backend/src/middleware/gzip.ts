import type { MiddlewareHandler } from "hono";
import { gunzipSync } from "node:zlib";

/**
 * BrainRequestCompression middleware:
 * 1. Always sets `x-clicky-accepts-gzip: 1` on every response so the C# client
 *    (`BrainRequestCompression.ObserveResponse`) knows it can compress future payloads.
 * 2. Automatically decompresses incoming requests with `Content-Encoding: gzip`
 *    so downstream `c.req.json()` / `c.req.text()` / `c.req.arrayBuffer()` read
 *    the decompressed body transparently.
 */
export const gzipMiddleware: MiddlewareHandler = async (c, next) => {
  c.header("x-clicky-accepts-gzip", "1");

  const contentEncoding = c.req.header("content-encoding")?.toLowerCase() ?? "";
  if (contentEncoding.includes("gzip")) {
    try {
      const compressedBuffer = Buffer.from(await c.req.raw.arrayBuffer());
      if (compressedBuffer.length > 0) {
        const decompressed = gunzipSync(compressedBuffer);
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
    } catch (err) {
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
