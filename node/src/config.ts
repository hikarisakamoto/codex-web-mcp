import * as os from "node:os";
import * as path from "node:path";
import * as fs from "node:fs";

export const CODEX_TIMEOUT_SECONDS = 180;

export function codexBin(): string {
  const v = process.env.CODEX_BIN;
  if (v) return v;
  return process.platform === "win32" ? "codex.exe" : "codex";
}

export function cacheDir(): string {
  const v = process.env.CODEX_WEB_CACHE;
  if (v) {
    fs.mkdirSync(v, { recursive: true });
    return v;
  }
  let base: string;
  if (process.platform === "win32") {
    base = process.env.LOCALAPPDATA ?? path.join(os.homedir(), "AppData", "Local");
  } else if (process.platform === "darwin") {
    base = path.join(os.homedir(), "Library", "Caches");
  } else {
    base = process.env.XDG_CACHE_HOME ?? path.join(os.homedir(), ".cache");
  }
  const dir = path.join(base, "codex-web-mcp");
  fs.mkdirSync(dir, { recursive: true });
  return dir;
}

export function cacheSubdir(): string {
  const d = path.join(cacheDir(), "cache");
  fs.mkdirSync(d, { recursive: true });
  return d;
}

export function logPath(): string {
  return path.join(cacheDir(), "calls.log");
}

export function appendCallLog(line: string): void {
  try {
    fs.appendFileSync(logPath(), line + "\n", "utf8");
  } catch {
    /* swallow */
  }
}
