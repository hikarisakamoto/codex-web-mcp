import * as fs from "node:fs";
import * as path from "node:path";
import * as url from "node:url";

let resolvedDir: string | null = null;
const tplCache = new Map<string, string>();

function isPromptsDir(p: string): boolean {
  try {
    return fs.statSync(p).isDirectory();
  } catch {
    return false;
  }
}

function walkUp(start: string, attempted: string[]): string | null {
  let cur = start;
  for (let i = 0; i < 10; i++) {
    const cand = path.join(cur, "prompts");
    attempted.push(cand);
    if (isPromptsDir(cand)) return cand;
    const parent = path.dirname(cur);
    if (parent === cur) break;
    cur = parent;
  }
  return null;
}

export function resolveDir(): string {
  if (resolvedDir !== null) return resolvedDir;
  const attempted: string[] = [];
  const env = process.env.CODEX_WEB_PROMPTS;
  if (env) {
    attempted.push(env);
    if (isPromptsDir(env)) {
      resolvedDir = env;
      return resolvedDir;
    }
  }
  // From the executing script's directory.
  try {
    const here = path.dirname(url.fileURLToPath(import.meta.url));
    const found = walkUp(here, attempted);
    if (found) {
      resolvedDir = found;
      return resolvedDir;
    }
  } catch {
    /* ignore */
  }
  // From cwd.
  try {
    const found = walkUp(process.cwd(), attempted);
    if (found) {
      resolvedDir = found;
      return resolvedDir;
    }
  } catch {
    /* ignore */
  }
  throw new Error(`prompts directory not found; tried: ${attempted.join(", ")}`);
}

export function render(toolName: string, vars: Record<string, string>): string {
  let tpl = tplCache.get(toolName);
  if (tpl === undefined) {
    const dir = resolveDir();
    tpl = fs.readFileSync(path.join(dir, `${toolName}.md`), "utf8");
    tplCache.set(toolName, tpl);
  }
  let out = tpl;
  for (const [k, v] of Object.entries(vars)) {
    out = out.split(`{${k}}`).join(v);
  }
  return out;
}
