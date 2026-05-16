import * as crypto from "node:crypto";
import * as fs from "node:fs";
import * as path from "node:path";

import { cacheSubdir } from "./config.js";

interface Entry {
  content: string;
  timestamp: string;
}

export function keyHash(tool: string, payload: string): string {
  return crypto.createHash("sha256").update(`${tool}::${payload}`).digest("hex").slice(0, 32);
}

function filePath(tool: string, payload: string): string {
  return path.join(cacheSubdir(), `${tool}-${keyHash(tool, payload)}.json`);
}

export function get(tool: string, payload: string, ttlSeconds: number): string | null {
  try {
    const p = filePath(tool, payload);
    const stat = fs.statSync(p);
    const ageMs = Date.now() - stat.mtimeMs;
    if (ageMs > ttlSeconds * 1000) return null;
    const data = fs.readFileSync(p, "utf8");
    const entry = JSON.parse(data) as Entry;
    return typeof entry.content === "string" ? entry.content : null;
  } catch {
    return null;
  }
}

export function put(tool: string, payload: string, content: string): void {
  try {
    const p = filePath(tool, payload);
    const entry: Entry = { content, timestamp: new Date().toISOString() };
    fs.writeFileSync(p, JSON.stringify(entry), "utf8");
  } catch (e) {
    process.stderr.write(`[codex-web-mcp] cache write failed: ${(e as Error).message}\n`);
  }
}
