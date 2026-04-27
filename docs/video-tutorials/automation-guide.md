# ISS Demo Automation Guide

This guide explains how to run repeatable browser walkthroughs for ISS video recording.

The runner lives in the frontend app and uses Playwright to:

- open the ISS app
- sign in with demo credentials
- navigate through a named module flow
- pause on each screen long enough for recording
- optionally save a browser video automatically

## Files

- `frontend/scripts/video-demo-runner.mjs`
- `frontend/scripts/video-demo-flows.mjs`

## Install

From `frontend/`:

```powershell
npm install
npx playwright install chromium
```

## Required Credentials

Set the demo login before running a flow:

```powershell
$env:ISS_VIDEO_EMAIL="admin@iss.local"
$env:ISS_VIDEO_PASSWORD="your-demo-password"
```

Set the app URL:

```powershell
$env:ISS_VIDEO_BASE_URL="http://localhost:3000"
```

Or for a hosted environment:

```powershell
$env:ISS_VIDEO_BASE_URL="https://erp-production-e16a.up.railway.app"
```

## Basic Commands

List the available flows:

```powershell
npm run video:demo:list
```

Run the orientation flow in a visible browser:

```powershell
node scripts/video-demo-runner.mjs --flow orientation --headed
```

Run the procurement walkthrough and save a browser video:

```powershell
node scripts/video-demo-runner.mjs --flow procurement --record-video
```

Keep the browser open at the end:

```powershell
node scripts/video-demo-runner.mjs --flow finance --record-video --keep-open
```

## Available Flows

- `orientation`
- `overview`
- `master-data`
- `procurement`
- `sales`
- `service`
- `inventory`
- `finance`
- `audit`
- `reporting`
- `admin`
- `full-tour`

## Recording Tips

- Use `--record-video` if you want a clean browser-only capture.
- Use an external screen recorder if you want:
  - microphone narration live
  - desktop-level recording
  - cursor effects outside the browser
- For cleaner final videos, record actions with this runner first, then add voiceover afterward.

## Useful Environment Variables

```powershell
$env:ISS_VIDEO_HEADED="true"
$env:ISS_VIDEO_RECORD="true"
$env:ISS_VIDEO_KEEP_OPEN="false"
$env:ISS_VIDEO_SLOW_MO="250"
$env:ISS_VIDEO_STEP_PAUSE="2400"
$env:ISS_VIDEO_TIMEOUT_MS="45000"
```

## Output

When `--record-video` is enabled, videos are written to:

```text
output/playwright/video-tutorials
```

## Suggested First Pass

Use this sequence:

1. `orientation`
2. `master-data`
3. `procurement`
4. `inventory`
5. `sales`
6. `finance`
7. `reporting`
8. `service`
9. `audit`
10. `admin`

That sequence matches the tutorial pack and keeps the business story coherent across recordings.
