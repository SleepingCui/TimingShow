# TimingShow

[简体中文](docs/README.zh-CN.md)

A hit-timing analysis mod for *A Dance of Fire and Ice*. Supports MelonLoader and UnityModManager (UMM).

## Features

### In-game display

- Show Timing, UR, Perfect (XPerfect) Ratio, and XACC graphs.
- Display results on song titles, planet judgement text, the HUD, death screens, and level-complete screens.
- Customize displayed values, colors, position, scale, format, and precision.
- Track XPerfect, average offset (Avg), and local co-op statistics by player.
- Add a hit bump animation to the HUD.

### Logs and analysis

- Record Timing logs and view detailed reports with offset scatter plots, distributions, XACC, and other metrics in [Offset Analyzer](https://sleepingcui.github.io/adofai_offset_analyzer).
- View a live Timing scatter plot during a run, or open a log's scatter plot from the log list. Both support pan, zoom, per-hit details, and judgement bands.
- Explore nine log-analysis views: overview, scatter plot, offset distribution, delta, rolling average, return map, combo heatmap, autocorrelation, and XACC. Filter by player and inspect statistics and individual hits.
- Review the previous run's hit markers in the level editor, inspect them on hover, or import markers from a log.
- Use the floating editor-hit panel (F6 by default) to search and filter by floor, time, player, offset, and judgement.
- Open the last-session report with F10 by default, even when log recording is disabled.
- Search the log list, automatically clean up old logs.

### Configuration

- Configure the mod in game. When loaded through MelonLoader, press F9 by default to open the settings.

## Updates

TimingShow supports in-game update checks for stable releases and CI development builds. Update checks and automatic updates are disabled by default; downloaded updates take effect after restarting the game.

### CI builds

> [!WARNING]
> CI builds provide early access to development versions before they are included in a stable release. They may contain bugs, unfinished features, or compatibility issues, so they are not recommended if you prioritize stability.

On versions with an update-channel selector, choose **CI** in the updater settings, enable update checks, and select **Check for updates now**.

#### Using CI builds on v1.9.4

> [!NOTE]
> Version 1.9.4 does not include an update-channel selector. To use the CI repository, configure a custom update URL as described below.

1. Open the TimingShow settings and expand **Debug**.
2. Enable **Custom Update URL**.
3. Enter this URL in the field that appears:

   ```text
   https://raw.githubusercontent.com/SleepingCui/ts_ci/main/Repository.json
   ```

4. Make sure update checking is enabled, then select **Check for updates now**.

TimingShow will check for CI updates through that repository.

## Screenshots

<details>
<summary>Click to expand</summary>

<img width="682" height="645" alt="TimingShow screenshot showing the in-game interface" src="https://github.com/user-attachments/assets/6abc512e-3f7d-446d-ba2f-d314ffed2934" />

<img width="1721" height="1021" alt="TimingShow screenshot showing analysis graphs" src="https://github.com/user-attachments/assets/62a7a0f1-ba1a-4445-b63a-ccd89d74e90a" />

<img width="1432" height="767" alt="TimingShow screenshot showing a graph window" src="https://github.com/user-attachments/assets/56699d0d-fade-4f03-b5f8-37853f5d38d7" />

<img width="593" height="302" alt="TimingShow screenshot showing a compact panel" src="https://github.com/user-attachments/assets/11a60e56-e268-4049-863e-e0226a894758" />

<img width="1152" height="2583" alt="Offset Analyzer report for TimingShow logs" src="https://github.com/user-attachments/assets/cb8203a3-380a-4b8e-a23b-041c0eb10f7d" />

</details>

## References and code credits

- [Overlayer](https://github.com/c3nb/Overlayer)
- [XPerfect](https://github.com/8100print/XPerfect)
- [YCH](https://github.com/adofaiex/YqlossClientHarmony)

## License

[MIT](LICENSE.txt)
