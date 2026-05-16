import { spawn } from "node:child_process";
import * as crypto from "node:crypto";
import * as fs from "node:fs";
import * as os from "node:os";
import * as path from "node:path";

import { CODEX_TIMEOUT_SECONDS, codexBin } from "./config.js";

export function run(prompt: string): Promise<string> {
  return new Promise((resolve) => {
    const tmpPath = path.join(os.tmpdir(), `codex-out-${crypto.randomUUID().replace(/-/g, "")}.txt`);
    const args = [
      "exec",
      "--skip-git-repo-check",
      "--ephemeral",
      "--color", "never",
      "--sandbox", "read-only",
      "--output-last-message", tmpPath,
      prompt,
    ];

    let child;
    try {
      child = spawn(codexBin(), args, { stdio: ["ignore", "ignore", "pipe"] });
    } catch (e) {
      cleanup(tmpPath);
      resolve(`ERROR: codex invocation failed: ${(e as Error).message}`);
      return;
    }

    let stderr = "";
    child.stderr?.on("data", (chunk: Buffer) => {
      stderr += chunk.toString("utf8");
    });

    let timedOut = false;
    const timer = setTimeout(() => {
      timedOut = true;
      child.kill("SIGKILL");
    }, CODEX_TIMEOUT_SECONDS * 1000);

    child.on("error", (e) => {
      clearTimeout(timer);
      cleanup(tmpPath);
      resolve(`ERROR: codex invocation failed: ${e.message}`);
    });

    child.on("close", (code) => {
      clearTimeout(timer);
      try {
        if (timedOut) {
          resolve("ERROR: codex timed out after 180s");
          return;
        }
        if (code !== 0) {
          resolve(`ERROR: codex exited with code ${code}: ${stderr.trim()}`);
          return;
        }
        let stat;
        try {
          stat = fs.statSync(tmpPath);
        } catch {
          resolve("ERROR: codex produced no output file");
          return;
        }
        if (stat.size === 0) {
          resolve("ERROR: codex output file empty");
          return;
        }
        const data = fs.readFileSync(tmpPath, "utf8").replace(/[ \t\r\n]+$/, "");
        if (data === "") {
          resolve("ERROR: codex output file empty");
          return;
        }
        resolve(data);
      } finally {
        cleanup(tmpPath);
      }
    });
  });
}

function cleanup(p: string): void {
  try {
    fs.unlinkSync(p);
  } catch {
    /* ignore */
  }
}
