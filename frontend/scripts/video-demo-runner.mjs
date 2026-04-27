import { mkdir } from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import readline from "node:readline/promises";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";
import { listFlowNames, videoFlows } from "./video-demo-flows.mjs";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(scriptDir, "..", "..");
const defaultRecordDir = path.join(repoRoot, "output", "playwright", "video-tutorials");

async function main() {
  const options = parseArgs(process.argv.slice(2));
  if (options.help) {
    printHelp();
    return;
  }

  if (options.list) {
    printFlowList();
    return;
  }

  const flow = videoFlows[options.flow];
  if (!flow) {
    console.error(`Unknown flow '${options.flow}'.`);
    printFlowList();
    process.exitCode = 1;
    return;
  }

  if (!options.email || !options.password) {
    console.error("ISS video demo credentials are required.");
    console.error("Set ISS_VIDEO_EMAIL and ISS_VIDEO_PASSWORD, or pass --email and --password.");
    process.exitCode = 1;
    return;
  }

  const recordVideoDir = options.recordVideo ? path.resolve(options.recordVideoDir) : null;
  if (recordVideoDir) {
    await mkdir(recordVideoDir, { recursive: true });
  }

  const browser = await chromium.launch({
    headless: !options.headed,
    slowMo: options.slowMo,
  });

  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 },
    screen: { width: 1920, height: 1080 },
    ignoreHTTPSErrors: true,
    recordVideo: recordVideoDir
      ? {
          dir: recordVideoDir,
          size: { width: 1920, height: 1080 },
        }
      : undefined,
    colorScheme: "light",
    locale: "en-LK",
    timezoneId: "Asia/Colombo",
    reducedMotion: "reduce",
  });

  const page = await context.newPage();
  page.setDefaultTimeout(options.timeoutMs);

  try {
    await login(page, options);

    console.log(`Running flow '${options.flow}' - ${flow.title}`);
    for (const step of flow.steps) {
      await runStep(page, step, options);
    }

    console.log(`Flow '${options.flow}' completed.`);

    if (options.keepOpen) {
      const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
      await rl.question("Press Enter to close the browser...");
      rl.close();
    }
  } finally {
    await context.close();
    await browser.close();
  }

  if (recordVideoDir) {
    console.log(`Video output directory: ${recordVideoDir}`);
  }
}

function parseArgs(argv) {
  const argMap = new Map();
  const flags = new Set();
  const positionals = [];

  for (let index = 0; index < argv.length; index += 1) {
    const current = argv[index];
    if (!current.startsWith("--")) {
      positionals.push(current);
      continue;
    }

    const withoutPrefix = current.slice(2);
    const separatorIndex = withoutPrefix.indexOf("=");
    if (separatorIndex >= 0) {
      const key = withoutPrefix.slice(0, separatorIndex);
      const value = withoutPrefix.slice(separatorIndex + 1);
      argMap.set(key, value);
      continue;
    }

    const next = argv[index + 1];
    if (next && !next.startsWith("--")) {
      argMap.set(withoutPrefix, next);
      index += 1;
      continue;
    }

    flags.add(withoutPrefix);
  }

  const flow = argMap.get("flow") ?? positionals[0] ?? "orientation";
  const headed = flags.has("headed") || envBool("ISS_VIDEO_HEADED", true);
  const keepOpen = flags.has("keep-open") || envBool("ISS_VIDEO_KEEP_OPEN", false);
  const recordVideo = flags.has("record-video") || argMap.has("record-video-dir") || envBool("ISS_VIDEO_RECORD", false);

  return {
    help: flags.has("help"),
    list: flags.has("list"),
    flow,
    headed,
    keepOpen,
    recordVideo,
    recordVideoDir: argMap.get("record-video-dir") ?? process.env.ISS_VIDEO_RECORD_DIR ?? defaultRecordDir,
    baseUrl: (argMap.get("base-url") ?? process.env.ISS_VIDEO_BASE_URL ?? "http://localhost:3000").replace(/\/+$/, ""),
    email: argMap.get("email") ?? process.env.ISS_VIDEO_EMAIL ?? "",
    password: argMap.get("password") ?? process.env.ISS_VIDEO_PASSWORD ?? "",
    slowMo: parseNumber(argMap.get("slow-mo") ?? process.env.ISS_VIDEO_SLOW_MO, 250),
    pauseMs: parseNumber(argMap.get("pause-ms") ?? process.env.ISS_VIDEO_STEP_PAUSE, 2400),
    timeoutMs: parseNumber(argMap.get("timeout-ms") ?? process.env.ISS_VIDEO_TIMEOUT_MS, 45000),
  };
}

function envBool(name, fallback) {
  const raw = process.env[name];
  if (!raw) {
    return fallback;
  }

  const normalized = raw.trim().toLowerCase();
  if (normalized === "true" || normalized === "1" || normalized === "yes") {
    return true;
  }

  if (normalized === "false" || normalized === "0" || normalized === "no") {
    return false;
  }

  return fallback;
}

function parseNumber(raw, fallback) {
  const value = Number(raw);
  return Number.isFinite(value) && value >= 0 ? value : fallback;
}

async function login(page, options) {
  await page.goto(`${options.baseUrl}/login`, { waitUntil: "domcontentloaded" });
  const emailInput = page.locator('input[type="email"]').first();
  const passwordInput = page.locator('input[type="password"]').first();
  await emailInput.waitFor({ state: "visible" });
  await emailInput.fill(options.email);
  await passwordInput.fill(options.password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await page.waitForURL((url) => !url.pathname.startsWith("/login"), { timeout: options.timeoutMs });
  await waitForHeading(page, "Dashboard", options.timeoutMs);
  await pause(options.pauseMs);
}

async function runStep(page, step, options) {
  const pauseMs = step.pauseMs ?? options.pauseMs;

  switch (step.type) {
    case "goto":
      await page.goto(`${options.baseUrl}${step.path}`, { waitUntil: "domcontentloaded" });
      await waitForHeading(page, step.heading, options.timeoutMs);
      await pause(pauseMs);
      break;
    case "fill":
      await page.getByLabel(step.label).fill(step.value);
      await pause(pauseMs);
      break;
    case "clickLink":
      await page.getByRole("link", { name: step.name, exact: true }).click();
      await pause(pauseMs);
      break;
    case "assertHeading":
      await waitForHeading(page, step.heading, options.timeoutMs);
      await pause(pauseMs);
      break;
    default:
      throw new Error(`Unsupported step type '${step.type}'.`);
  }
}

async function waitForHeading(page, heading, timeoutMs) {
  await page.getByRole("heading", { name: heading, exact: true }).waitFor({
    state: "visible",
    timeout: timeoutMs,
  });
}

function pause(durationMs) {
  return new Promise((resolve) => setTimeout(resolve, durationMs));
}

function printFlowList() {
  console.log("Available flows:");
  for (const name of listFlowNames()) {
    console.log(`- ${name}`);
  }
}

function printHelp() {
  console.log(`ISS video demo runner

Usage:
  npm run video:demo:help
  npm run video:demo:list
  node scripts/video-demo-runner.mjs --flow orientation --headed
  node scripts/video-demo-runner.mjs --flow procurement --record-video

Environment variables:
  ISS_VIDEO_BASE_URL     App base URL, default http://localhost:3000
  ISS_VIDEO_EMAIL        Demo user email
  ISS_VIDEO_PASSWORD     Demo user password
  ISS_VIDEO_HEADED       true/false, default true
  ISS_VIDEO_RECORD       true/false, default false
  ISS_VIDEO_RECORD_DIR   Video output directory
  ISS_VIDEO_SLOW_MO      Default 250
  ISS_VIDEO_STEP_PAUSE   Default 2400
  ISS_VIDEO_TIMEOUT_MS   Default 45000
  ISS_VIDEO_KEEP_OPEN    true/false, default false

Flags:
  --flow <name>          Flow to run
  --base-url <url>       Override base URL
  --email <email>        Override demo user email
  --password <value>     Override demo user password
  --headed               Force headed browser mode
  --record-video         Save browser video to output/playwright/video-tutorials
  --record-video-dir     Override output directory
  --keep-open            Keep the browser open after the flow
  --list                 Show available flows
  --help                 Show this message
`);
}

await main();
