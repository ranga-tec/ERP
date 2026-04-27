import { spawn } from "node:child_process";
import { access, copyFile, mkdir, readdir, unlink, writeFile } from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(scriptDir, "..", "..");
const defaultOutputDir = path.join(repoRoot, "output", "playwright", "video-tutorials", "item-create-demo");
const defaultVoiceDir = path.join(defaultOutputDir, "voices");

const defaultNarrationTemplate = [
  "From the Items screen, click Create Item.",
  "Enter a unique SKU and a clear item name.",
  "For this example, keep the default spare part type and leave tracking as none.",
  "Confirm the unit of measure, enter a default unit cost, and leave the optional brand, category, and finance mappings blank.",
  "Finally, click Create Item.",
  "ISS saves the record and opens the item summary page, where you can review the SKU, name, type, and default cost.",
].join(" ");

async function main() {
  const options = parseArgs(process.argv.slice(2));
  if (options.help) {
    printHelp();
    return;
  }

  if (!options.storageStateSource && (!options.email || !options.password)) {
    console.error("ISS credentials are required.");
    console.error("Set ISS_VIDEO_EMAIL and ISS_VIDEO_PASSWORD, or pass --email and --password.");
    process.exitCode = 1;
    return;
  }

  const timestamp = formatTimestamp(new Date());
  const sku = options.sku ?? `DEMO-ITEM-${timestamp}`;
  const itemName = options.name ?? `Demo Item ${timestamp}`;
  const baseName = `iss-demo-create-item-${timestamp}`;
  const narrationText = options.narrationText
    .replaceAll("{sku}", sku)
    .replaceAll("{name}", itemName);

  await mkdir(options.outputDir, { recursive: true });
  await mkdir(options.voiceDir, { recursive: true });

  const narrationScriptPath = path.join(options.outputDir, `${baseName}.txt`);
  const narrationWavPath = path.join(options.outputDir, `${baseName}-narration.wav`);
  const rawVideoPath = path.join(options.outputDir, `${baseName}-raw.webm`);
  const finalVideoPath = path.join(options.outputDir, `${baseName}.mp4`);
  const posterPath = path.join(options.outputDir, `${baseName}.png`);
  const storageStatePath = path.join(options.outputDir, `${baseName}-storage-state.json`);

  if (options.loginOnly) {
    await mkdir(options.outputDir, { recursive: true });
    const browser = await chromium.launch({
      headless: !options.headed,
      args: ["--force-device-scale-factor=1"],
    });

    try {
      console.log("Logging in and saving storage state...");
      await loginAndPersistState(browser, { ...options, storageStatePath });
    } finally {
      await browser.close();
    }

    console.log(`Created storage state: ${storageStatePath}`);
    return;
  }

  await writeFile(narrationScriptPath, `${narrationText}\n`, "utf8");

  console.log(`Preparing voice '${options.voice}'...`);
  const voice = await ensureVoiceFiles(options.voice, options.voiceDir);
  console.log("Synthesizing narration...");
  await synthesizeNarration(voice, narrationScriptPath, narrationWavPath, options);

  try {
    console.log("Recording browser demo...");
    await recordDemo({
      ...options,
      sku,
      itemName,
      rawVideoPath,
      storageStatePath,
    });

    console.log("Muxing narration with browser capture...");
    await muxNarration(rawVideoPath, narrationWavPath, finalVideoPath);
    console.log("Extracting preview frame...");
    await extractPoster(finalVideoPath, posterPath);
  } finally {
    await safeDelete(storageStatePath);
  }

  console.log(`Created item demo video: ${finalVideoPath}`);
  console.log(`Created narration audio: ${narrationWavPath}`);
  console.log(`Created preview frame: ${posterPath}`);
  console.log(`Created live demo item: ${sku} / ${itemName}`);
}

function parseArgs(argv) {
  const argMap = new Map();
  const flags = new Set();

  for (let index = 0; index < argv.length; index += 1) {
    const current = argv[index];
    if (!current.startsWith("--")) {
      continue;
    }

    const withoutPrefix = current.slice(2);
    const separatorIndex = withoutPrefix.indexOf("=");
    if (separatorIndex >= 0) {
      argMap.set(withoutPrefix.slice(0, separatorIndex), withoutPrefix.slice(separatorIndex + 1));
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

  return {
    help: flags.has("help"),
    loginOnly: flags.has("login-only"),
    headed: flags.has("headed") || envBool("ISS_VIDEO_HEADED", false),
    baseUrl: (argMap.get("base-url") ?? process.env.ISS_VIDEO_BASE_URL ?? "http://localhost:3000").replace(/\/+$/, ""),
    email: argMap.get("email") ?? process.env.ISS_VIDEO_EMAIL ?? "",
    password: argMap.get("password") ?? process.env.ISS_VIDEO_PASSWORD ?? "",
    outputDir: path.resolve(argMap.get("output-dir") ?? process.env.ISS_VIDEO_ITEM_OUTPUT_DIR ?? defaultOutputDir),
    voiceDir: path.resolve(argMap.get("voice-dir") ?? process.env.ISS_VIDEO_VOICE_DIR ?? defaultVoiceDir),
    voice: argMap.get("voice") ?? process.env.ISS_VIDEO_VOICE ?? "en_US-lessac-medium",
    storageStateSource: argMap.get("storage-state") ?? process.env.ISS_VIDEO_STORAGE_STATE ?? "",
    sku: argMap.get("sku") ?? process.env.ISS_VIDEO_ITEM_SKU ?? "",
    name: argMap.get("name") ?? process.env.ISS_VIDEO_ITEM_NAME ?? "",
    narrationText: argMap.get("narration-text") ?? process.env.ISS_VIDEO_NARRATION ?? defaultNarrationTemplate,
    timeoutMs: parseNumber(argMap.get("timeout-ms") ?? process.env.ISS_VIDEO_TIMEOUT_MS, 45000),
    typingDelayMs: parseNumber(argMap.get("typing-delay-ms") ?? process.env.ISS_VIDEO_TYPING_DELAY_MS, 85),
    moveDurationMs: parseNumber(argMap.get("move-duration-ms") ?? process.env.ISS_VIDEO_MOVE_DURATION_MS, 850),
    pauseMs: parseNumber(argMap.get("pause-ms") ?? process.env.ISS_VIDEO_ITEM_PAUSE_MS, 1000),
    voiceLengthScale: parseFloatSafe(argMap.get("voice-length-scale") ?? process.env.ISS_VIDEO_VOICE_LENGTH_SCALE, 1.04),
    voiceSentenceSilence: parseFloatSafe(argMap.get("voice-sentence-silence") ?? process.env.ISS_VIDEO_VOICE_SENTENCE_SILENCE, 0.18),
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

function parseFloatSafe(raw, fallback) {
  const value = Number.parseFloat(raw);
  return Number.isFinite(value) && value > 0 ? value : fallback;
}

function formatTimestamp(date) {
  const year = String(date.getFullYear());
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  const hour = String(date.getHours()).padStart(2, "0");
  const minute = String(date.getMinutes()).padStart(2, "0");
  const second = String(date.getSeconds()).padStart(2, "0");
  return `${year}${month}${day}-${hour}${minute}${second}`;
}

async function ensureVoiceFiles(voiceName, voiceDir) {
  const modelPath = path.join(voiceDir, `${voiceName}.onnx`);
  const configPath = path.join(voiceDir, `${voiceName}.onnx.json`);

  try {
    await access(modelPath);
    await access(configPath);
    return { modelPath, configPath };
  } catch {
    await runCommand("python", ["-m", "piper.download_voices", "--download-dir", voiceDir, voiceName], {
      label: "Download Piper voice",
    });
    await access(modelPath);
    await access(configPath);
    return { modelPath, configPath };
  }
}

async function synthesizeNarration(voice, inputPath, outputPath, options) {
  await runCommand(
    "piper",
    [
      "-m",
      voice.modelPath,
      "-c",
      voice.configPath,
      "-i",
      inputPath,
      "-f",
      outputPath,
      "--length-scale",
      String(options.voiceLengthScale),
      "--sentence-silence",
      String(options.voiceSentenceSilence),
      "--volume",
      "1.15",
    ],
    { label: "Synthesize narration" },
  );
}

async function recordDemo(options) {
  const browser = await chromium.launch({
    headless: !options.headed,
    args: ["--force-device-scale-factor=1"],
  });

  try {
    if (options.storageStateSource) {
      console.log(`Reusing storage state from ${options.storageStateSource}...`);
      await copyFile(path.resolve(options.storageStateSource), options.storageStatePath);
    } else {
      console.log("Logging in and saving storage state...");
      await loginAndPersistState(browser, options);
    }
    console.log("Opening recorded session...");

    const context = await browser.newContext({
      viewport: { width: 1600, height: 900 },
      screen: { width: 1600, height: 900 },
      ignoreHTTPSErrors: true,
      storageState: options.storageStatePath,
      recordVideo: {
        dir: path.dirname(options.rawVideoPath),
        size: { width: 1600, height: 900 },
      },
      colorScheme: "light",
      locale: "en-LK",
      timezoneId: "Asia/Colombo",
      reducedMotion: "reduce",
    });

    const page = await context.newPage();
    page.setDefaultTimeout(options.timeoutMs);
    const video = page.video();
    const videoDir = path.dirname(options.rawVideoPath);
    const existingRecordings = new Set(await listRecordingFiles(videoDir));

    try {
      console.log("Loading Items screen...");
      await page.goto(`${options.baseUrl}/master-data/items`, { waitUntil: "domcontentloaded" });
      await waitForHeading(page, "Items", options.timeoutMs);
      await ensureOverlay(page, "Create a new item");
      await setBanner(page, "Open the item creation form");
      await wait(options.pauseMs + 500);

      const createItemLink = page.getByRole("link", { name: "Create Item", exact: true });
      console.log("Opening Create Item form...");
      await clickWithCursor(page, createItemLink, options);

      await page.waitForURL(/\/master-data\/items\/create$/, { timeout: options.timeoutMs });
      await waitForHeading(page, "Create Item", options.timeoutMs);
      await ensureOverlay(page, "Create a new item");
      await setBanner(page, "Enter the main item details");
      await wait(options.pauseMs);

      console.log("Completing required item fields...");
      await typeIntoField(page, "SKU", options.sku, options);
      await typeIntoField(page, "Name", options.itemName, options);
      await hoverField(page, "Type", options, 800);
      await hoverField(page, "Tracking", options, 800);
      await ensureUnitOfMeasure(page, options);
      await typeIntoField(page, "Default Unit Cost", "1250", options);

      await setBanner(page, "Optional fields can be left blank");
      await hoverField(page, "Brand", options, 700);
      await hoverField(page, "Category", options, 700);
      await hoverField(page, "Income / Revenue Account", options, 700);
      await hoverField(page, "Expense Account", options, 700);

      const submitButton = page.getByRole("button", { name: "Create Item", exact: true });
      await submitButton.scrollIntoViewIfNeeded();
      await wait(400);
      console.log("Submitting item...");
      await clickWithCursor(page, submitButton, options);

      await page.waitForURL(/\/master-data\/items\/[^/]+$/, { timeout: options.timeoutMs });
      await waitForHeading(page, options.itemName, options.timeoutMs);
      await ensureOverlay(page, "Review the saved item");
      await setBanner(page, "Review the saved record");
      await wait(options.pauseMs + 1200);

      console.log("Showing saved item summary...");
      await hoverSummaryValue(page, "SKU", options, 900);
      await hoverSummaryValue(page, "Name", options, 900);
      await hoverSummaryValue(page, "Default Cost", options, 1100);
      await wait(options.pauseMs + 1600);
    } finally {
      console.log("Finalizing recorded browser video...");
      try {
        await promiseWithTimeout(context.close(), 15000, "Timed out while closing the recorded browser context.");
      } catch (error) {
        console.warn(String(error));
      }

      if (video) {
        try {
          await promiseWithTimeout(video.saveAs(options.rawVideoPath), 15000, "Timed out while saving the recorded video.");
        } catch (error) {
          console.warn(String(error));
        }
      }

      try {
        await access(options.rawVideoPath);
      } catch {
        const fallbackRecording = await findNewestRecording(videoDir, existingRecordings);
        if (fallbackRecording) {
          await copyFile(fallbackRecording, options.rawVideoPath);
        }
      }
    }
  } finally {
    await browser.close();
  }
}

async function loginAndPersistState(browser, options) {
  const context = await browser.newContext({
    viewport: { width: 1600, height: 900 },
    ignoreHTTPSErrors: true,
    colorScheme: "light",
    locale: "en-LK",
    timezoneId: "Asia/Colombo",
    reducedMotion: "reduce",
  });

  try {
    const page = await context.newPage();
    page.setDefaultTimeout(options.timeoutMs);
    await page.goto(`${options.baseUrl}/login`, { waitUntil: "domcontentloaded" });
    const emailInput = page.locator('input[type="email"]').first();
    const passwordInput = page.locator('input[type="password"]').first();
    await emailInput.waitFor({ state: "visible" });
    await emailInput.fill(options.email);
    await passwordInput.fill(options.password);
    await page.getByRole("button", { name: "Sign in", exact: true }).click();
    await page.waitForURL((url) => !url.pathname.startsWith("/login"), { timeout: options.timeoutMs });
    await context.storageState({ path: options.storageStatePath });
    console.log("Storage state saved.");
  } finally {
    await context.close();
  }
}

async function ensureOverlay(page, title) {
  await page.evaluate(({ titleText }) => {
    const root = document.documentElement;

    let banner = document.getElementById("__iss-demo-banner");
    if (!banner) {
      banner = document.createElement("div");
      banner.id = "__iss-demo-banner";
      Object.assign(banner.style, {
        position: "fixed",
        top: "18px",
        left: "18px",
        zIndex: "2147483647",
        padding: "10px 14px",
        borderRadius: "14px",
        background: "rgba(15, 23, 42, 0.86)",
        color: "#f8fafc",
        fontFamily: "Segoe UI, sans-serif",
        fontSize: "16px",
        fontWeight: "700",
        letterSpacing: "0.01em",
        boxShadow: "0 14px 32px rgba(15, 23, 42, 0.28)",
        backdropFilter: "blur(12px)",
      });
      root.appendChild(banner);
    }

    let titleNode = document.getElementById("__iss-demo-banner-title");
    if (!titleNode) {
      titleNode = document.createElement("div");
      titleNode.id = "__iss-demo-banner-title";
      banner.appendChild(titleNode);
    }

    let subtitle = document.getElementById("__iss-demo-banner-subtitle");
    if (!subtitle) {
      subtitle = document.createElement("div");
      subtitle.id = "__iss-demo-banner-subtitle";
      Object.assign(subtitle.style, {
        marginTop: "4px",
        fontSize: "12px",
        fontWeight: "500",
        color: "rgba(226, 232, 240, 0.88)",
      });
      banner.appendChild(subtitle);
    }

    titleNode.textContent = titleText;
    subtitle.textContent = "ISS guided demo";

    let cursor = document.getElementById("__iss-demo-cursor");
    if (!cursor) {
      cursor = document.createElement("div");
      cursor.id = "__iss-demo-cursor";
      Object.assign(cursor.style, {
        position: "fixed",
        top: "0",
        left: "0",
        width: "28px",
        height: "28px",
        borderRadius: "999px",
        border: "3px solid rgba(220, 38, 38, 0.95)",
        boxShadow: "0 0 0 6px rgba(248, 113, 113, 0.25), 0 14px 28px rgba(15, 23, 42, 0.24)",
        background: "rgba(255, 255, 255, 0.78)",
        transform: "translate(110px, 110px)",
        transition: "transform 120ms linear",
        pointerEvents: "none",
        zIndex: "2147483647",
      });

      const dot = document.createElement("div");
      Object.assign(dot.style, {
        position: "absolute",
        top: "50%",
        left: "50%",
        width: "8px",
        height: "8px",
        borderRadius: "999px",
        background: "rgba(220, 38, 38, 0.98)",
        transform: "translate(-50%, -50%)",
      });
      cursor.appendChild(dot);
      root.appendChild(cursor);
    }

    if (!window.__issDemoCursorState) {
      window.__issDemoCursorState = { x: 110, y: 110 };
    }

    window.__issDemoMoveCursor = (x, y, click = false) => {
      window.__issDemoCursorState = { x, y };
      const element = document.getElementById("__iss-demo-cursor");
      if (!element) {
        return;
      }

      element.style.transform = `translate(${x - 14}px, ${y - 14}px)`;
      if (click) {
        element.animate(
          [
            { transform: `translate(${x - 14}px, ${y - 14}px) scale(1)` },
            { transform: `translate(${x - 14}px, ${y - 14}px) scale(0.82)` },
            { transform: `translate(${x - 14}px, ${y - 14}px) scale(1)` },
          ],
          { duration: 260, easing: "ease-out" },
        );
      }
    };

    window.__issDemoSetBannerText = (nextText) => {
      const target = document.getElementById("__iss-demo-banner-subtitle");
      if (target) {
        target.textContent = nextText;
      }
    };
  }, { titleText: title });
}

async function setBanner(page, text) {
  await page.evaluate((nextText) => {
    window.__issDemoSetBannerText?.(nextText);
  }, text);
}

async function moveCursorToLocator(page, locator, options, durationMs = options.moveDurationMs) {
  const box = await locator.boundingBox();
  if (!box) {
    throw new Error("Target element is not visible for cursor movement.");
  }

  const targetX = box.x + Math.min(Math.max(box.width * 0.45, 18), box.width - 10);
  const targetY = box.y + Math.min(Math.max(box.height * 0.5, 16), box.height - 8);
  const start = await page.evaluate(() => window.__issDemoCursorState ?? { x: 110, y: 110 });
  const steps = Math.max(16, Math.round(durationMs / 40));

  for (let index = 1; index <= steps; index += 1) {
    const progress = index / steps;
    const eased = progress < 0.5
      ? 2 * progress * progress
      : 1 - Math.pow(-2 * progress + 2, 2) / 2;
    const nextX = start.x + (targetX - start.x) * eased;
    const nextY = start.y + (targetY - start.y) * eased;
    await page.mouse.move(nextX, nextY, { steps: 1 });
    await page.evaluate(({ x, y }) => {
      window.__issDemoMoveCursor?.(x, y, false);
    }, { x: nextX, y: nextY });
    await wait(Math.max(18, Math.round(durationMs / steps)));
  }

  return { x: targetX, y: targetY };
}

async function clickWithCursor(page, locator, options) {
  const target = await moveCursorToLocator(page, locator, options);
  await page.evaluate(({ x, y }) => {
    window.__issDemoMoveCursor?.(x, y, true);
  }, target);
  await page.mouse.click(target.x, target.y, { delay: 80 });
  await wait(options.pauseMs);
}

async function typeIntoField(page, label, value, options) {
  const control = fieldControl(page, label);
  await control.scrollIntoViewIfNeeded();
  await clickWithCursor(page, control, options);
  await control.press("ControlOrMeta+A");
  await page.keyboard.type(value, { delay: options.typingDelayMs });
  await wait(options.pauseMs);
}

async function hoverField(page, label, options, durationMs) {
  const control = fieldControl(page, label);
  await control.scrollIntoViewIfNeeded();
  await moveCursorToLocator(page, control, options, durationMs);
  await wait(Math.round(durationMs / 2));
}

async function ensureUnitOfMeasure(page, options) {
  const control = fieldControl(page, "UoM");
  await control.scrollIntoViewIfNeeded();
  const currentValue = (await control.inputValue()).trim();
  if (currentValue) {
    await hoverField(page, "UoM", options, 900);
    return;
  }

  await typeIntoField(page, "UoM", "PCS", options);
}

async function hoverSummaryValue(page, label, options, durationMs) {
  const value = page.locator(`div:text-is("${label}")`).first().locator("xpath=following-sibling::div[1]");
  await value.scrollIntoViewIfNeeded();
  await moveCursorToLocator(page, value, options, durationMs);
  await wait(Math.round(durationMs / 2));
}

function fieldControl(page, labelText) {
  const labelPattern = new RegExp(`^${escapeRegex(labelText)}$`);
  const sibling = page
    .locator("label")
    .filter({ hasText: labelPattern })
    .first()
    .locator("xpath=following-sibling::*[1]");

  return sibling.locator(
    "xpath=self::*[self::input or self::textarea or self::select or @role='combobox'] | .//*[self::input or self::textarea or self::select or @role='combobox']",
  ).first();
}

function escapeRegex(text) {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

async function waitForHeading(page, heading, timeoutMs) {
  await page.getByRole("heading", { name: heading, exact: true }).waitFor({
    state: "visible",
    timeout: timeoutMs,
  });
}

function wait(durationMs) {
  return new Promise((resolve) => setTimeout(resolve, durationMs));
}

async function muxNarration(videoPath, audioPath, outputPath) {
  await runCommand(
    "ffmpeg",
    [
      "-y",
      "-i",
      videoPath,
      "-i",
      audioPath,
      "-filter_complex",
      "[1:a]apad[a]",
      "-map",
      "0:v:0",
      "-map",
      "[a]",
      "-c:v",
      "libx264",
      "-pix_fmt",
      "yuv420p",
      "-c:a",
      "aac",
      "-b:a",
      "192k",
      "-movflags",
      "+faststart",
      "-shortest",
      outputPath,
    ],
    { label: "Mux narration into demo video" },
  );
}

async function extractPoster(videoPath, outputPath) {
  await runCommand(
    "ffmpeg",
    [
      "-y",
      "-i",
      videoPath,
      "-vf",
      "thumbnail,scale=1600:-1",
      "-frames:v",
      "1",
      outputPath,
    ],
    { label: "Extract preview frame" },
  );
}

async function runCommand(command, args, options = {}) {
  await new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      cwd: repoRoot,
      stdio: ["ignore", "pipe", "pipe"],
      shell: false,
    });

    let stderr = "";

    child.stdout.on("data", (chunk) => {
      const text = chunk.toString();
      if (text.trim()) {
        process.stdout.write(text);
      }
    });

    child.stderr.on("data", (chunk) => {
      const text = chunk.toString();
      stderr += text;
      if (options.forwardStderr ?? true) {
        process.stderr.write(text);
      }
    });

    child.on("error", (error) => reject(error));
    child.on("close", (code) => {
      if (code === 0) {
        resolve(undefined);
        return;
      }

      reject(new Error(`${options.label ?? command} failed with exit code ${code}.\n${stderr}`));
    });
  });
}

async function safeDelete(targetPath) {
  try {
    await unlink(targetPath);
  } catch {
    // Best-effort cleanup for auth state files.
  }
}

async function listRecordingFiles(directoryPath) {
  try {
    const entries = await readdir(directoryPath, { withFileTypes: true });
    return entries
      .filter((entry) => entry.isFile() && /^page@.+\.webm$/i.test(entry.name))
      .map((entry) => path.join(directoryPath, entry.name));
  } catch {
    return [];
  }
}

async function findNewestRecording(directoryPath, existingRecordings) {
  const files = await listRecordingFiles(directoryPath);
  const candidates = files.filter((filePath) => !existingRecordings.has(filePath));
  return candidates.at(-1) ?? null;
}

function promiseWithTimeout(promise, timeoutMs, message) {
  let timeoutId;
  const timeoutPromise = new Promise((_, reject) => {
    timeoutId = setTimeout(() => reject(new Error(message)), timeoutMs);
  });

  return Promise.race([
    promise.finally(() => clearTimeout(timeoutId)),
    timeoutPromise,
  ]);
}

function printHelp() {
  console.log(`ISS create-item demo generator

Usage:
  node scripts/video-demo-item-create.mjs --base-url https://your-iss-url --email user@example.com --password secret
  node scripts/video-demo-item-create.mjs --login-only --base-url https://your-iss-url --email user@example.com --password secret

Environment variables:
  ISS_VIDEO_BASE_URL                 App base URL
  ISS_VIDEO_EMAIL                    Demo user email
  ISS_VIDEO_PASSWORD                 Demo user password
  ISS_VIDEO_HEADED                   true/false, default false
  ISS_VIDEO_ITEM_OUTPUT_DIR          Output directory for the generated files
  ISS_VIDEO_VOICE_DIR                Piper voice download directory
  ISS_VIDEO_VOICE                    Piper voice name, default en_US-lessac-medium
  ISS_VIDEO_STORAGE_STATE            Existing Playwright storage state JSON to reuse
  ISS_VIDEO_ITEM_SKU                 Override demo SKU
  ISS_VIDEO_ITEM_NAME                Override demo item name
  ISS_VIDEO_NARRATION                Override narration text
  ISS_VIDEO_TIMEOUT_MS               Default 45000
  ISS_VIDEO_TYPING_DELAY_MS          Default 85
  ISS_VIDEO_MOVE_DURATION_MS         Default 850
  ISS_VIDEO_ITEM_PAUSE_MS            Default 1000
  ISS_VIDEO_VOICE_LENGTH_SCALE       Default 1.04
  ISS_VIDEO_VOICE_SENTENCE_SILENCE   Default 0.18

Flags:
  --base-url <url>                   Override base URL
  --email <email>                    Override demo user email
  --password <value>                 Override demo user password
  --output-dir <path>                Override output directory
  --voice-dir <path>                 Override Piper voice directory
  --voice <name>                     Override Piper voice selection
  --storage-state <path>             Reuse an existing storage state JSON instead of logging in
  --login-only                       Only create a fresh storage state file, then exit
  --sku <value>                      Override created SKU
  --name <value>                     Override created item name
  --narration-text <text>            Override narration text
  --headed                           Run the browser headed instead of headless
  --help                             Show this message
`);
}

await main();
