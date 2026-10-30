# TimingShow — User Guide

> Version 1.9.3 · For *A Dance of Fire and Ice* · Licensed under MIT

TimingShow is a **timing information overlay** for *A Dance of Fire and Ice*. It surfaces the per-hit deviation the game already computes but never shows you — how many milliseconds early or late you were — and displays it live, on-screen, in whatever style you prefer. It can also record every hit of a run to disk for later analysis, and ships an offline chart viewer for reviewing those records.

This guide is written for **players**. It explains what each feature does, what every toggle actually changes, and how to troubleshoot problems.

> **If you are a developer** — here to compile it, change the code, add a judgement, or just find out which game functions it patches — this document will not give you what you need. Read the [Developer Guide](DeveloperGuide.en.md) instead.

---

## Table of contents

1. [What it is](#1-what-it-is)
2. [Installing and uninstalling](#2-installing-and-uninstalling)
3. [Five-minute setup](#3-five-minute-setup)
4. [On-screen numbers](#4-on-screen-numbers)
5. [Charts: XACC line graph and timing scatter plot](#5-charts-xacc-line-graph-and-timing-scatter-plot)
6. [Local co-op (multiplayer)](#6-local-co-op-multiplayer)
7. [Timing logs](#7-timing-logs)
8. [Analysing logs: web analyser and built-in chart window](#8-analysing-logs-web-analyser-and-built-in-chart-window)
9. [Level editor hit markers](#9-level-editor-hit-markers)
10. [XPerfect support and its two modes](#10-xperfect-support-and-its-two-modes)
11. [Settings reference](#11-settings-reference)
12. [Files and data locations](#12-files-and-data-locations)
13. [FAQ](#13-faq)
14. [Known limitations](#14-known-limitations)
15. [Credits and licence](#15-credits-and-licence)

---

## 1. What it is

### 1.1 The problem it solves

The game tells you that you hit "Perfect" — it does not tell you that you were **4.7 ms late**. For anyone trying to improve accuracy, that number is the actionable part: a consistent +8 ms average is a different problem from a ±40 ms scatter, and they need different practice. TimingShow exposes the deviation value the game already calculates, and gives you several ways to look at it.

| Capability | Description |
|---|---|
| Live numbers | Last hit deviation, running average, UR (instability), Perfect ratio — on the HUD, the song title, or the judgement text |
| Trend visualisation | In-game XACC line graph and timing scatter plot |
| Records | Every hit written to a log file for later analysis |
| Built-in analyser | Nine statistical views inside the log chart window |
| Editor assistance | See every hit of the run you just finished, drawn on the editor track |
| Local co-op | Per-player statistics and colour coding for 1–4 players |

### 1.2 Supported game versions

- Works with both the **legacy judgement system** (a `HitMargin` enum without `XPerfect` / `PerfectMinus` / `PerfectPlus`) and the **3.4.0+ judgement system**.
- The mod detects the running version itself and switches algorithms and displayed judgement types automatically — no manual selection. The detection result is logged as `Detected version: ...`.
- If you see `Unrecognized HitMargin enum version`, the game changed its judgement enum in a way the mod has not adapted to yet. Readings may be inaccurate; update the mod.

### 1.3 Two loaders

The mod ships in two independent forms:

- **UnityModManager (UMM)** — the settings page lives inside the UMM window.
- **MelonLoader (Melon)** — the mod draws its own settings window, opened with `F9` by default.

Functionality is identical. **Install exactly one** (reason in the [FAQ](#13-faq)).

---

## 2. Installing and uninstalling

### 2.1 Prerequisites

- A legitimate copy of *A Dance of Fire and Ice*.
- Either [UnityModManager](https://www.nexusmods.com/site/mods/21) (UMM) **or** [MelonLoader](https://melonwiki.xyz/) (Melon) already installed for the game.
- The release archive `TimingShow.zip` (GitHub Releases, fixed filename).

### 2.2 UMM installation

1. Make sure UMM has been installed into the game at least once (a `Mods` folder exists in the game directory).
2. Either use UMM's **Install Mod** and pick `TimingShow.zip`, or extract the four files manually to:

   ```
   <game>\Mods\TimingShow\
       Info.json
       TimingShow.Loader.UMM.dll
       TimingShow.dll
       lang.json
   ```

   Note the `Mods\TimingShow\` subfolder — not directly into `Mods\`.

3. Launch the game. **TimingShow** appears in the UMM mod list; enable it.
4. Open the UMM window (default `Ctrl+F10`), select TimingShow on the left; the settings page appears on the right.

### 2.3 MelonLoader installation

1. Make sure Melon is installed for the game and that you have **launched the game once** (Melon creates its folders on first launch).
2. Copy these three files **flat** into:

   ```
   <game>\Mods\
       TimingShow.Loader.Melon.dll
       TimingShow.dll
       lang.json
   ```

   `Info.json` is UMM-specific; Melon does not need it (harmless if present).

3. Launch the game and press **F9** to open the settings window.

### 2.4 Uninstalling

- UMM: uninstall the mod through UMM, or delete `<game>\Mods\TimingShow\` entirely.
- Melon: delete `Mods\TimingShow.Loader.Melon.dll`, `Mods\TimingShow.dll`, and `Mods\lang.json` (only if that `lang.json` belongs to this mod).
- Optional cleanup:
  - `<game>\Mods\TimingShow\` (default location for settings, logs and cache)
  - `%TEMP%\TimingShow\` (extracted font cache; recreated automatically)

> ⚠️ **Never install both the UMM and Melon builds.** Each loader registers its own full set of game hooks, so the HUD is drawn twice and statistics are accumulated twice (numbers jump, visuals flicker, logs get duplicated). Remove one completely before switching loaders.

### 2.5 Opening and saving settings

| Loader | Open settings | Save |
|---|---|---|
| UMM | `Ctrl+F10` → select TimingShow | Click UMM's **Save** button |
| Melon | `F9` (rebindable in settings) | **Save and close** at the bottom of the window; also saved on game exit |

**Settings do not autosave.** If the process is killed, every change since the last save is lost.

---

## 3. Five-minute setup

A recommended first-run order that gets you visible results immediately:

1. **Pick a language** — `简体中文` / `English` / `한국어` at the very top of the page.
2. **Enable the timing HUD** — tick `Show Timing HUD`. Hit a couple of notes; a line like `Timing - 12.3ms` appears at the centre of the screen.
3. **Position it** — expand the group and drag `X Offset` / `Y Offset` (range −0.5 … 0.5, origin at screen centre, positive Y is up).
4. **Add more numbers** — tick `Show Avg Offset HUD`, `Show UR HUD`, `Show Ratio HUD`. They stack downwards by default.
5. **See the shape** — tick `Show Timing Scatter Plot`. Each dot is one hit; the green band is the Perfect window, the narrow blue band is XPerfect.
6. **Start recording** — tick `Enable Timing Logging`. One log file per run from then on.
7. **Review** — scroll down to the `Log list`, expand it, click **View Graph** on a row to open the built-in chart window.

---

## 4. On-screen numbers

### 4.1 The four text HUDs

Each HUD occupies one line of text, independent of the others; enable any combination.

| HUD | Toggle | Shows | Meaning |
|---|---|---|---|
| Timing | Show Timing HUD | Deviation of the last hit | e.g. `Timing - 12.3ms` — positive means **late**, negative means **early** |
| Avg | Show Avg Offset HUD | Arithmetic mean of the run so far | e.g. `Avg - 8.4ms` — a systematic late bias |
| UR | Show UR HUD | Instability of this run | lower is steadier; `UR = standard deviation × 10` |
| Ratio | Show Ratio HUD | Judgement ratio | e.g. `Ratio - 12.5:1`; numerator/denominator configurable |

**About UR.** Unstable Rate borrows osu!'s definition, `UR = std dev × 10`. It measures *consistency*, not accuracy: a player who is always 10 ms early has a poor Avg but can have a perfectly good UR. It is arguably the most useful single number to watch while practising.

**The three Ratio modes** (the `Ratio Mode` buttons):

| Mode | Numerator / denominator |
|---|---|
| Perfect (default) | Normal Perfect count / everything else |
| +/- Perfect | Perfect family (including Perfect−, Perfect+, Early/Late Perfect) / everything else |
| XPerfect | XPerfect count / everything else |

A zero denominator renders as `infinity`; a zero total renders as `0`.

#### Options shared by all four HUDs

| Control | Meaning |
|---|---|
| `Display Format` | Free text; `{0}` is replaced with the value. Defaults: `Timing - {0}ms`, `Avg - {0}ms`, `UR - {0}`, `Ratio - {0}:1`. Units may live in the format string or not |
| `Decimal Places` | 0–5 |
| `X Offset` / `Y Offset` | −0.5 … 0.5, **proportional to the screen centre**; +X right, +Y up |
| `Scale` | 0.2 … 3.0; base font size is `24 × scale` |
| `Bold Text` | Bold |
| `Alignment` | Left / Centre / Right |
| `Show in Angle` | Show the angular deviation (`°`) instead of time (`ms`). Angle is what the game actually uses internally; milliseconds are derived from angle, BPM and song speed |
| `Change color with judgment` | Colour the text by judgement type (Perfect = green, early/late = yellow, way off = orange-red …) |
| `Enable XPerfect` | Legacy versions only. Distinguishes XPerfect hits inside the Perfect family with their own colour |
| `Bounce On Hit` | 500 ms scale-up-and-settle animation on every value change (peak +28%) |
| `Use Custom Font` / `Font:` | Pick a `.ttf` / `.otf` file for the HUD; the toggle is switched on automatically |
| `Show each player separately in co-op` | One line per player in co-op (see section 6) |

> **What happens if the format string is invalid?** The mod catches the exception and falls back to showing the bare value; nothing crashes. The bad format is remembered for the session — you must restart the game after fixing it.

### 4.2 Replacing the song title

With `Replace Title` enabled, the **song title text** in the top-left corner is replaced by the live value.

| Option | Meaning |
|---|---|
| `Decimal Places` | Precision |
| `Font Size` | Percentage of the original size; 100 leaves it unchanged |
| `Change color with judgment` | Colour by judgement |
| `Enable XPerfect` | Distinguish XPerfect on legacy versions |
| `Show in Angle` | Angle instead of milliseconds |
| `Bounce On Hit` | Title pulses on each hit |

This is the most discreet layout: the title is naturally prominent and never covers the chart.

### 4.3 Replacing the judgement text (planet)

With `Replace Judgment Display` enabled, the judgement word popping out of the planet (`Perfect`, `Early`, …) is replaced by the deviation value.

> ⚠️ This option **requires re-entering the editor or restarting the level** to fully take effect (the game caches the judgement text component). The label says so. If you do not want to restart manually, enable `Auto-reload Editor`, which restarts the current level when you toggle this option.

| Option | Meaning |
|---|---|
| `Decimal Places` | Precision |
| `Font Size` | Percentage |
| `Show in Angle` | Angle / milliseconds |
| `Enable XPerfect` | Distinguish XPerfect on legacy versions |
| `Replacement Settings` (sub-foldout) | Choose **which judgements** show a number; the rest stay as-is |

The fourteen entries are: `Early!!`, `Too Early!`, `Slightly Early`, `Perfect`, `Perfect−`, `Perfect+`, `XPerfect`, `Slightly Late`, `Too Late!`, `Too Late!!`, `Multi Press!`, `Multi Press`, `Miss...`, `Too Fast!!!`, `Auto Play`.

All default to on except `Auto Play`. A popular configuration is to keep the Perfect family numeric and leave the failure family untouched, so you can tell "I hit it badly" from "I missed it entirely" at a glance.

> Available judgement types differ by game version. `Perfect−` only exists on 3.4+, `Perfect` only on legacy, and `XPerfect` / `Perfect+` only when the game natively supports XPerfect. Unused entries are hidden automatically.

### 4.4 Death screen

With `Show on Player Death` enabled, the "Try calibrating…" line on the death screen becomes a run summary.

| Option | Meaning |
|---|---|
| `Decimal Places` | Precision |
| `Show Average Offset` / `Show UR` / `Show XACC` / `Show Ratio` | Four independent items |
| `Font Size` | Percentage |

Something like this, one line per player in co-op:

```
Avg 8.42ms    UR 96.315    XACC: 98.7421%    Ratio: 12.5:1
```

`XACC` here is the game's internal weighted accuracy (`percentXAcc`), matching the XACC line graph of [5.1](#51-xacc-line-graph).

### 4.5 Results screen

With `Show in Results Screen` enabled, run statistics are appended to the completion results text.

| Option | Meaning |
|---|---|
| `Decimal Places` | Precision |
| `Show Average Offset` / `Show UR` / `Show Ratio` | Three items (there is no XACC item here) |
| `Show all players on every results page` | Co-op only: results pages rotate per player. Enabled, every page shows everyone's stats; disabled, each page shows only that player's |
| `Font Size` | Percentage |

---

## 5. Charts: XACC line graph and timing scatter plot

Both charts overlay the game, with adjustable position, size, colours and font. Neither intercepts the mouse during play.

### 5.1 XACC line graph

With `Show XACC Line Graph` enabled, a curve appears: x-axis is hit count, y-axis is XACC (weighted accuracy, 0–100%).

| Option | Meaning |
|---|---|
| `Only show on death/clear` | Hidden during play, shown at the end |
| `Max Sample Points` | Keep only the most recent N points (default 250); during play it behaves as a scrolling tail window |
| `X Offset` / `Y Offset` | 0–1 screen fractions, **Y measured from the top** (opposite from the text HUDs) |
| `Graph Width` / `Graph Height` | Pixels |
| `Scale` | Internal font and line-width scaling |
| `Show Border` | Draw the outer frame |
| `Background / Line / Grid / Axis Text / XACC Value / Border Color` | Six colours |
| `Use Custom Font` / `Font:` | Font inside the graph |
| `Show Performance Info` | Overlay per-frame render cost (debug) |

Data comes from the game's running `percentXAcc`. While playing, only the last N points are drawn; once the level ends, excess points are downsampled by uniform stride so the curve shape is preserved.

### 5.2 Timing scatter plot

With `Show Timing Scatter Plot` enabled, **each dot is one hit**. The vertical axis is deviation (ms or angle); the horizontal axis is time by default.

#### Judgement window bands

The most useful part is the background banding, which visualises where the judgement boundaries actually are:

- Green translucent band = Perfect window
- Yellow-green band = Early/Late Perfect window
- Orange-red band = the wider pass window
- Blue band = XPerfect window (narrowest)

The band widths are not constants: they are derived from **BPM × song speed × pitch × difficulty**, because the game judges in *angle* units, so higher BPM means less time for the same angle.

| Option | Meaning |
|---|---|
| `Compute Windows From Difficulty and BPM` | On by default; reads the live BPM and difficulty. Off switches to a fixed threshold BPM from the slider, useful for comparing charts across BPMs |
| `Window Threshold BPM` | Basis for manual mode (100–600) |
| `Show Judgment Window Bands` | Master switch |
| `Show XP Band` | Controls the innermost blue band |
| `Perfect / E-L Perfect / Early-Late Band Color` | Three bands, independently colourable |
| `XP Band Color` | Blue band colour |

#### Other display options

| Option | Meaning |
|---|---|
| `Only show on death/clear` | Hidden during play |
| `Use Hit Index for X Axis` | Switch the x-axis from time to hit ordinal. Useful together with auto-scroll off for static inspection |
| `Always Scroll (May Affect Performance)` | The viewport follows the newest point every frame. This is the **only continuous performance cost** the mod has; disable it on weaker machines |
| `Window Sample Count` | How much history takes part in drawing during play (default 200) |
| `Max Render Points` | Decimation ceiling (default 800, range 20–8000). Above the ceiling, points are bucketed per horizontal pixel keeping the minimum and maximum of each bucket — **spikes are never smoothed away** |
| `Point Size` | 1–12 |
| `Color by Judgment` | Colour dots by judgement; off uses a single colour |
| `Show 0 ms Reference Line` | Draw `y = 0` |
| `Show Average Line` | Draw the running mean curve |
| `Ignore Outliers in Overview` | Tukey 1.5×IQR clipping so one absurd hit cannot flatten the y-axis |
| `Show Border` / `Border Color` | Outer frame |
| Background / Grid / Point / Zero / Avg / Axis Text colours | Seven colours |
| `Show Performance Info` | Debug overlay |
| `Use Custom Font` / `Font:` | — |

#### Interaction

The scatter plot only accepts the mouse when the **level has ended or the game is paused**, so you cannot mis-click while playing:

| Action | Effect |
|---|---|
| Drag | Pan the viewport |
| Mouse wheel | Zoom anchored at the cursor |
| Hover | Detail panel for that hit: time, deviation, judgement, player, floor |

After pausing, the plot becomes interactive; a `drag to pan · wheel to zoom` hint is shown for 8 seconds.

---

## 6. Local co-op (multiplayer)

The game supports 1–4 players locally (pause menu → Player Select). TimingShow supports it fully:

| Element | Behaviour in co-op |
|---|---|
| Timing / Avg / UR / Ratio HUDs | One line per player; can be merged into a single comma-separated line via `Show each player separately` |
| Song title | e.g. `Timing - 12.3ms,15.6ms,8.2ms` |
| Judgement text | Each player sees their own value |
| Death screen | All players compared |
| Results screen | Configurable as per-player pages or all players on every page |
| XACC line graph | One curve per player |
| Timing scatter plot | Points coloured per player |
| Log chart window | The scatter view has a clickable player legend to isolate one player |
| Editor hit markers | Offset and coloured per player along the track |

The four player colours are fixed:

| Player | Colour |
|---|---|
| P1 | Red `#FF5959` |
| P2 | Blue `#4FC4F7` |
| P3 | Yellow `#FFD64F` |
| P4 | Purple `#B387FF` |

> A player who has not hit anything yet shows `--` instead of a misleading `0.0ms`.
>
> Statistics are per **player**. A player who joins mid-run starts accumulating from that moment.

---

## 7. Timing logs

### 7.1 Enabling recording

Tick `Enable Timing Logging`. It takes effect **from the next run** (the current run needs a restart to create a session).

| Option | Meaning |
|---|---|
| `Decimal Places` | Value precision in the log (default 4) |
| `Show in Angle` | Record angles instead of milliseconds. **The deviation field then carries degrees**, which most external tools will misinterpret as milliseconds |
| `Record During Autoplay` | Autoplay is not recorded by default |
| `Use JSON Log Format` | See below |
| `Save Path:` | Log directory, default `<game>\Mods\TimingShow\Logs` |
| `Buffer Size (KB)` | Write buffer, default 64. A larger buffer means more records lost if the game crashes, because flushing happens only at session end |
| `Open Logs Folder` | Open the log directory in the file manager |

A file is created only if at least one hit was recorded — a run with zero judgements leaves no empty file behind.

### 7.2 The two formats

| | JSON (`.json`) | Binary (`.tlog.gz`, default) |
|---|---|---|
| Readability | Plain text, inspectable and editable | Requires the mod's reader or a custom tool |
| Size | Larger (≈45–90 bytes per hit) | Smaller (gzip-compressed, ≈24–50 bytes per hit) |
| Compatibility | Web analyser and built-in chart window | Built-in chart window; web analyser too (served as raw bytes over local HTTP) |
| Structure version | `formatVersion: 8` | `binaryFormatVersion: 9` |

**Which to pick?** Long-term archiving and disk space → binary (the default). Manual inspection or third-party scripting → JSON.

> The format is chosen **per run**: flipping the switch mid-run only affects the next one.

### 7.3 File naming

```
<unix timestamp>_<song name>.json
<unix timestamp>_<song name>.tlog.gz
e.g. 1738742903_Final Destination.json
```

- The timestamp is Unix seconds.
- Characters illegal in filenames are replaced with `_`.
- **Two runs of the same song started within the same second overwrite each other** (the file is opened in `Create` mode). Not a practical concern for normal play; relevant for scripted batch testing.

### 7.4 Automatic cleanup

Tick `Auto-clean` and set "keep the first N files" (default 50).

- Fires **at the end of every run**.
- Ordering follows the log list's current sort mode (time / size / song name); default is by time, keeping the newest N.
- Only the **first level** of the log directory is scanned; no recursion.
- Files currently being written are skipped.

### 7.5 Log list

The `Log list` foldout in the settings page is a complete log manager.

| Control | Function |
|---|---|
| `Refresh` | Rescan the directory |
| `Sort: Time / Size / songName` | Click to cycle; default newest-first |
| `Search` | Substring match on filename, song name and full path; `×` clears |
| Summary | "N files, X MB" over the currently visible entries |
| `View Graph` per row | Open in the **built-in chart window** (section 8) |
| `Open File` per row | Open with the system default application |
| `Delete` per row | **Two-step confirmation**: first click turns the button red; a second click within 3 seconds actually deletes |
| `Web Analyzer` per row | Only present when the local analyser service is enabled; see [8.1](#81-web-analyser-recommended) |

The list is virtualised, so a huge directory will not freeze it; however at most 60 rows are drawn per frame, so fast scrolling may show blank rows. That is by design.

---

## 8. Analysing logs: web analyser and built-in chart window

### 8.1 Web analyser (recommended)

Online analyser: [https://sleepingcui.github.io/adofai_offset_analyzer](https://sleepingcui.github.io/adofai_offset_analyzer)

You can drag a log file into the page manually, or jump straight there from the game:

1. Expand `Advanced`, tick `Enable Analyzer Local Service`.
2. Optionally set `Port (0=random)` and `Timeout (sec)` (defaults: 0 = OS-assigned, 60 s).
3. Click **Web Analyzer** on a log list row.

How it works: the mod starts a temporary read-only HTTP server on `127.0.0.1`, generates a single-use token URL, and points the browser at the analyser page, which then fetches the log through that URL. The server shuts down after the timeout.

> Security notes: the listener binds to the loopback address only, accepts only requests carrying the one-time token, and only ever serves the single file that token refers to. Nothing is uploaded to the mod author or any third party.
>
> Known quirk: browsers sometimes send an `OPTIONS` preflight request first, and the server accepts exactly one connection, so the first attempt can fail — click **Web Analyzer** again.

### 8.2 Built-in chart window

Clicking **View Graph** on a log list row reads the whole log and opens a chart window. No network required, good for offline review.

#### Basic operations

| Action | How |
|---|---|
| Move the window | Drag the title bar |
| Resize | Drag the bottom-right corner |
| Close | `✕` on the title bar (`Escape` only closes drop-down menus) |
| Switch view | Drop-down menu at the top (9 views) |
| Isolate a player | Player drop-down in co-op; the scatter view also accepts legend clicks |
| Open settings | Gear button; `Basic` and `Colors` pages |
| Remember size | Window size is saved on every resize and restored next time |

`UI Scale` inside the window (0.75–3.0) scales font sizes and row heights uniformly; on high-resolution displays, 1.5 or more is recommended.

#### The nine views

| View | What it shows | Why you care |
|---|---|---|
| **Overview** (default) | Four cards (total hits / max combo / UR / Ratio) + large XACC + judgement distribution bars + file metadata | Start here for an instant impression of the run |
| **Scatter** | Per-hit deviation with judgement bands and a player legend | Overall shape; spot systematic bias |
| **Distribution** | Deviation histogram + fitted normal curve + mean and ±σ lines | Is your error random? It should look like a bell centred on 0 |
| **XACC** | Cumulative weighted accuracy over hit index, one curve per player | Did the run improve or collapse as it went |
| **Delta** | Histogram of the difference between consecutive hits | Is your correction pattern regular? A skewed Δ means systematic chasing |
| **Rolling** | Sliding-window mean with a ±1σ band; window size 5–500 | Trends without single-hit noise; the slider above the plot adjusts it live |
| **Return map** | Previous deviation vs current deviation, with the y=x diagonal and a correlation coefficient | Is your error predictable? Negative correlation means you are **over-correcting** (was early, so you hit late); positive means the error **persists** |
| **Combo** | Strip chart whose colour encodes combo length; grey segments are breaks | Spot where breaks cluster and where your longest streak sits |
| **Autocorrelation** | ACF bars for lag 1–50 with a 95% confidence band | Stricter periodicity detection. A lag outside the band implies a repeating mistake pattern |

#### Scatter range filter

The scatter view has a **Range Filter** row at its top-left: `[min] ~ [max]` inputs in `ms` (or `°` for angle logs).

- Click a field and type; negative numbers and decimals are accepted. `Enter`, `Escape`, or clicking elsewhere ends editing.
- An empty field means "no limit on that side"; both empty means no filtering.
- Points outside the range are not drawn, and the average line and automatic y-axis use only in-range points. The right side shows a live "N pts in range" count.
- **Reset** clears both fields.
- The filter only lives inside this window: it survives view switches and resets when the window is closed.

#### Statistics panel

Four rows of statistics, depending on the active view:

- Scatter: total hits, UR, Ratio, XACC
- Distribution: mean, standard deviation, skewness, excess kurtosis
- Delta: pair count, mean absolute delta, median absolute delta, mean delta
- Rolling: head mean, tail mean, drift (positive = drifting later)
- Return map: pair count, correlation, verdict (over-correcting / drift / independent)
- Combo: streak count, longest streak, average streak
- Autocorrelation: sample count, lag-1 coefficient, strongest lag, its coefficient

#### Settings panel

Opened by the gear button; changes take effect and are saved only when you press **Apply**.

**Basic page**: UI scale, corner rounding, point size, max render points (log slider), ignore outliers, x-axis by hit index, colour by judgement, distinguish players, show 0 ms line, show average line, show grid, show judgement bands, show XPerfect band, window threshold BPM, use custom font.

**Colors page**: ten colour slots (background / grid / points / 0 ms line / average line / axis text / Perfect band / E-L band / Early-Late band / XP band), each with R/G/B sliders; the button beside each slot resets RGB only and preserves alpha. A live preview at the bottom shows the hex value and alpha.

### 8.3 Which to use

| Need | Recommendation |
|---|---|
| A quick look at overall performance | Built-in window → Overview |
| Deep statistics, exports, comparing runs | Web analyser (more capable, can save reports) |
| Offline | Built-in window |
| Verify a specific judgement was recorded correctly | Open the `.json` log directly |

### 8.4 No log needed: the last-hit report

With "Enable timing logging" turned off, neither path above is available — there is no file to analyse. That is what the **report window** is for:

1. At the bottom of the settings panel, set "Hit report key" to some key (default `F10`; `None` disables the hotkey).
2. Finish a run: clearing the level, failing, or being sent back to the level editor all count.
3. Press the key and the window appears with exactly the same content as the built-in chart window (nine views, per-player filter, stats panel), except the data comes from memory instead of a file. Press it again to close.

The data stays in memory for the whole run, so having logging off changes nothing. It is a **single-run snapshot**: starting the next level does not wipe the previous report immediately, it is replaced once the next run finishes. The "View last hit report" button in the settings panel does the same thing and is greyed out when there is nothing to show.

---

## 9. Level editor hit markers

Added in 1.9.x: **after finishing a run, return to the level editor and every hit of that run is drawn on the track.**

### Workflow

1. Play a run normally inside the editor (completing, dying, or quitting back to the editor all work).
2. Back in the editor, the mod draws that run's hits on their corresponding floors.
3. Point size, colour and hover detail are all configurable.

### What you see

- Dot colour = judgement colour (same as the HUD).
- Dot **position** is anchored to the floor that was hit, offset in the early/late direction — the offset magnitude is controlled by `Angle offset scale` (0 = all dots sit on the floor centre, 1 = offset by the true angle).
- Hovering a dot shows player, deviation, angle, judgement, BPM × speed, and floor number. A `~` prefix on the floor number means the id is estimated by the mod (older game versions do not expose the exact id).

### Settings

| Option | Meaning |
|---|---|
| `Show editor hit markers` | Master switch |
| `Point Size` | 1–100 px (at 100% zoom); circles scale with the editor zoom |
| `Angle offset scale` | 0–3, see above |
| `Show hover details` | Off leaves just the dots |
| `Clear hit markers` | Immediately remove what is currently drawn |
| `Import log and show` | Load a historical log (`.json` / `.tlog` / `.tlog.gz`). Requires the log to belong to the level currently open in the editor |

### Limitations

- Only the **run that just ended** is available (the data is held in memory). Leaving the level, starting another run, or disabling the mod clears it.
- Importing a historical log requires floor information in the format: JSON v8 or newer, binary v9 or newer. Older logs report "the log contains no floor information".
- A log recorded on a **different level** than the one being edited is rejected with a mismatch message.

---

## 10. XPerfect support and its two modes

XPerfect is the **stricter-than-Perfect** judgement tier.

### When the game supports it natively (3.4.0+)

The game's judgement enum contains `XPerfect` and the mod simply uses the game's result — no configuration at all. In this case the `Enable XPerfect` and `Enable XPerfect Hook Mode` options are not shown, because they are unnecessary.

### On older game versions

The legacy enum has no XPerfect, so the mod offers two paths:

**Path A — built-in algorithm (default)**

The mod applies the published algorithm from the [XPerfect mod](https://github.com/8100print/XPerfect) (MIT): compare each hit deviation against a BPM-dependent boundary, with a 15° floor.

Use this when you do not have the XPerfect mod installed, or when you want a pure-algorithm reference for consistency.

**Path B — hook mode**

Tick `Advanced` → `Enable XPerfect Hook Mode`. The mod reflectively hooks an installed XPerfect mod and reads its verdict directly.

- A status is shown to the right: green = hooked; red = failed (with reason); blue = not applicable (this game version does not need it).
- Requires XPerfect to be loaded, and this mod to load after it (`LoadAfter: XPerfect` is already set in `Info.json`).
- If XPerfect is absent, the failure is logged and not retried.

> The two paths can disagree at the boundary. If you are comparing data, stick to one mode.

---

## 11. Settings reference

Top-to-bottom order of the settings page. Defaults are the field initialisers in the source.

### General

| Item | Default | Notes |
|---|---|---|
| Language | English | 简体中文 / English / 한국어 |

### Replace title

| Item | Default | Item | Default |
|---|---|---|---|
| Replace Title | off | Decimal Places | 1 |
| Font Size | 100 | Change color with judgment | off |
| Enable XPerfect | off | Show in Angle | off |
| Bounce On Hit | off | | |

### Replace judgement display

| Item | Default | Item | Default |
|---|---|---|---|
| Replace Judgment Display | off | Decimal Places | 1 |
| Font Size | 100 | Show in Angle | off |
| Enable XPerfect | off | Auto-reload Editor | off |
| 14 replacement switches | all on (except Auto Play) | | |

### Death / results screen

| Item | Default | Item | Default |
|---|---|---|---|
| Show on Player Death | off | Show in Results Screen | off |
| Decimal Places (death) | 1 | Decimal Places (results) | 1 |
| Avg / UR / XACC / Ratio | all on | Avg / UR / Ratio | all on |
| Font Size (death) | 60 | Font Size (results) | 60 |
| | | Show all players on every results page | on |

### The four HUDs

| Item | Timing | Avg | UR | Ratio |
|---|---|---|---|---|
| Enabled | off | off | off | off |
| Default Y offset | 0 | −0.025 | −0.05 | −0.10 |
| Default format | `Timing - {0}ms` | `Avg - {0}ms` | `UR - {0}` | `Ratio - {0}:1` |
| X/Y range | ±0.5 | ±0.5 | ±0.5 | ±0.5 |
| Default scale | 1.0 | 1.0 | 1.0 | 1.0 |
| Bold / align / decimals | off / left / 1 | same | same | same |
| Judge colour / angle / bounce | all off | all off | all off | all off |
| Co-op per player | on | on | on | on |
| Ratio mode | — | — | — | Perfect |

### XACC line graph

| Item | Default | Item | Default |
|---|---|---|---|
| Enabled | off | Only on death/clear | off |
| X / Y offset | 0.05 / 0.50 | Width / Height | 260 / 100 |
| Scale | 1.0 | Max sample points | 250 |
| Show border | off | Show Performance Info | off |

### Timing scatter plot

| Item | Default | Item | Default |
|---|---|---|---|
| Enabled | off | Only on death/clear | off |
| X / Y offset | 0.05 / 0.20 | Width / Height | 320 / 140 |
| Scale | 1.0 | Window sample count | 200 |
| Max render points | 800 | Point size | 3 |
| X axis by hit index | off | Always scroll | off |
| Color by judgment | on | 0 ms reference line | on |
| Average line | on | Show border | off |
| Judgement bands | on | Auto window from BPM | on |
| Window threshold BPM | 310 | XPerfect band | on |
| Ignore outliers | off | Show Performance Info | off |

### Editor hit markers

| Item | Default | Item | Default |
|---|---|---|---|
| Show editor hit markers | **on** | Point size | 35 |
| Angle offset scale | 1 | Show hover details | on |

### Logging

| Item | Default | Item | Default |
|---|---|---|---|
| Enable Timing Logging | off | Decimal Places | 4 |
| Show in Angle | off | Record During Autoplay | off |
| Use JSON Log Format | off (binary) | Save Path | `<game>\Mods\TimingShow\Logs` |
| Buffer Size (KB) | 64 | Auto-clean | off |
| Keep first N files | 50 | | |

### Log chart window

| Item | Default | Item | Default |
|---|---|---|---|
| UI Scale | 1.25 | Overview ignore outliers | off |
| X axis by hit index | off | Color by judgment | on |
| Distinguish players | on | 0 ms reference line | on |
| Average line | on | Max render points | 800 |
| Point size | 3 | | |

> `Show grid`, `Show judgement bands`, `Show XPerfect band`, corner rounding, the four band colours, the ten colour slots and the window size are editable **only from the settings panel inside the chart window**.

### Advanced / Debug

| Item | Default | Meaning |
|---|---|---|
| Enable XPerfect Hook Mode | off | See section 10 |
| Auto-reload Editor | off | Restart the level when toggling judgement replacement |
| Enable Analyzer Local Service | off | See 8.1 |
| Port | 0 (random) | 0–65535 |
| Timeout (sec) | 60 | 5–3600 |

The `Debug` section adds ten diagnostic switches (bounds assertions, performance report, input tracing, mesh-bounds collection, assertion interval/tolerance/limit/throttle, scatter sample warning threshold, log list rows-per-frame budget). **Ordinary users should leave them alone** — they exist to dump internal state into the game log when diagnosing a problem. These labels are untranslated in the Chinese UI.

---

## 12. Files and data locations

| Content | Path |
|---|---|
| Settings | `<mod directory>\Settings.json` (UMM = `Mods\TimingShow\Settings.json`; Melon = `Mods\Settings.json`) |
| Logs (default) | `<game>\Mods\TimingShow\Logs\` |
| Log metadata cache | `<game>\Mods\TimingShow\Cache\logmeta.cache` |
| Performance report | `<log directory>\PerfLogs\perf_*.txt/.csv` (requires the Debug performance report) |
| Extracted font cache | `%TEMP%\TimingShow\` (or Unity's `temporaryCachePath\TimingShow`) |
| Game log | `%USERPROFILE%\AppData\LocalLow\7th Beat Games\A Dance of Fire and Ice\Player.log` |

`lang.json` lives next to the mod DLLs. **Do not delete it** — without it the settings UI degrades to raw key names (`Toggle_TimingHUD` and friends).

---

## 13. FAQ

**Q: I installed it and nothing appears.**
First confirm the mod actually loaded (ticked in the UMM list, or the Melon console shows TimingShow). Then check that the items you want are enabled — every HUD is off by default.

**Q: The value is stuck at `--`.**
`--` means "this player has not hit anything yet". That only happens in co-op. Seeing it in single player indicates a player-attribution anomaly; please report it with the log.

**Q: I changed a setting and nothing happened.**
Settings must be **saved** (UMM: Save; Melon: Save and close). Also note that the log format, log directory and buffer size only apply to the **next run**.

**Q: I changed the font but in-game glyphs look the same.**
Fonts are packed into the DLL at build time; changing font assets requires a **full rebuild** (incremental builds skip the embedding step). In a development environment, use `/t:Rebuild`.

**Q: What if I install both the UMM and Melon builds?**
Duplicate HUD drawing, doubled statistics and duplicated log writes. Keep exactly one.

**Q: Why was no log file created?**
Three usual causes: (1) `Enable Timing Logging` was off; (2) the run contained zero hits (empty sessions are deleted); (3) the run was autoplay and `Record During Autoplay` was off.

**Q: The log analysis disagrees with what I saw in game.**
Check whether `Show in Angle` was on — the log then stores degrees, not milliseconds. Also, judgement codes mean different things across game versions (legacy `Perfect` and modern `Perfect−` share a code); always interpret alongside `hitMarginVersion` in the log header.

**Q: The scatter plot does not respond to the mouse.**
The in-game scatter plot accepts the mouse only when **paused or after the level ends**. The log chart window is always interactive, but opening its settings panel suspends scatter interaction.

**Q: The `Web Analyzer` button is missing.**
It only appears when `Enable Analyzer Local Service` is on.

**Q: `Web Analyzer` opens a broken page.**
A browser `OPTIONS` preflight may have consumed the single connection — click again. Or the port is in use; try another port or leave it at 0 (random).

**Q: Lots of English in the Chinese UI.**
The Debug section, the two `Show Performance Info` toggles and the UR label are untranslated. Known gap; no functional impact.

**Q: What is `Unrecognized HitMargin enum version`?**
The game changed its judgement enum and the mod has not adapted yet. Readings may be wrong; wait for a mod update.

**Q: How do I report a bug?**
Include (1) `Player.log`; (2) the log file from the affected run (or a screenshot); (3) game version, loader (UMM/Melon) and mod version.

---

## 14. Known limitations

**Functionality**

- `Replace Judgment Display` patches the game's single shared judgement text component, hence the "requires re-entering the editor / restarting the level" caveat.
- The results screen has **no XACC item** (asymmetric with the death screen) — a deliberate trade-off.
- The in-game scatter plot shows **no player legend and no per-player filtering** (only the log chart window's scatter view does), though co-op points are still coloured per player.
- Editor hit markers keep only the run that just ended, in memory; there is no cross-run history browser.
- Title replacement rewrites the single shared song-title text, so in co-op it becomes one joined line rather than per-player columns.

**Numerics**

- UR and all standard deviations in the mod use the **population** formula (divide by n), which differs slightly from the sample formula (divide by n−1) used by many statistics packages.
- "XACC" exists in three different senses here (the game's native `percentXAcc`, the per-judgement weighted value in the XACC view, and the grouped weighting on the overview page). The numbers can differ; state which one you mean when comparing.
- On legacy versions, XPerfect classification depends on an external mod or the local algorithm, which may differ at the boundary.
- In very old logs, judgement code 3 means both `Perfect` and `Perfect−`; interpret together with the version field.

**Performance**

- `Always Scroll` forces the scatter plot to **redraw every frame** — the mod's only sustained per-frame cost.
- `Max Render Points` has a hard ceiling imposed by Unity's 65535-vertex UGUI limit (the effective cap is 6540 points); beyond it the request is silently clamped and circles degrade to polygons.
- The log list draws at most 60 rows per frame; fast scrolling can show blank rows.
- Large logs are parsed on a background thread; the UI only shows a "reading…" hint and the load cannot be cancelled.

**UI**

- Numeric text fields (e.g. buffer size) use "invalid input reverts to the default", so you cannot type digit by digit (clearing the field immediately resets it). Select the whole field and type over it.
- Settings are written only on exit/save and the write is not atomic. Killing the process can lose changes or corrupt `Settings.json` (which then falls back to defaults).
- The chart window has no DPI awareness; on high-resolution displays raise `UI Scale` manually.

---

## 15. Credits and licence

This mod references the following projects:

- [Overlayer](https://github.com/c3nb/Overlayer)
- [ADOFAI_ProgressDisplayer2](https://github.com/FLOWERs-Modding/ADOFAI_ProgressDisplayer2)
- [XPerfect](https://github.com/8100print/XPerfect) (source of the XPerfect judgement algorithm)

Fonts:

- Source Han Sans SC (SIL Open Font License 1.1)
- Gmarket Sans Medium (SIL Open Font License 1.1)

Licence: MIT. Font licences are embedded in the mod as `OFL.txt`.
