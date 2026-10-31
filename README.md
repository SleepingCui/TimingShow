# TimingShow

面向冰与火之舞的击打分析模组，支持 MelonLoader 和 UnityModManager

A hit-timing analysis mod for A Dance of Fire and Ice, supporting MelonLoader and UnityModManager.

## 功能 / Features

- 显示 Timing、UR、Perfect(XPerfect) Ratio 和 XACC曲线图

  Displays Timing, UR, Perfect (XPerfect) Ratio, and XACC curve graphs

- 支持在歌曲标题、星球判定文字、HUD、死亡页和通关页显示

  Supports display on song titles, planet judgement text, HUD, death screen, and level complete screen
- 可自定义显示内容、颜色、位置、缩放、格式和精度

  Customizable display content, colors, position, scale, format, and precision
- 支持 XPerfect 判定

  Supports XPerfect
- 支持 Timing 日志，可在[Offset Analyzer](https://sleepingcui.github.io/adofai_offset_analyzer)获取详细的分析报告 (包括偏移散点图，正态分布图，XACC曲线等指标)

  Supports Timing logs; detailed analysis reports (including offset scatter plots, normal distribution graphs, XACC curves, and other metrics) are available at [Offset Analyzer](https://sleepingcui.github.io/adofai_offset_analyzer).
- 提供游戏内实时 Timing 散点图与日志散点图窗口，支持平移缩放、单次击打详情与判定/XPerfect 色带

  Provides an in-game real-time Timing scatter graph and a log scatter window (log list → View Graph), with pan/zoom, per-hit details, and judge/XPerfect bands
- 日志分析窗口新增概览，提供九种图表视图，包括散点、偏移分布、Delta、滑动均值、Return Map、连击热力图、自相关和 XACC；支持玩家筛选、统计信息与逐击详情

  The log analysis window provides nine views, including an overview, scatter plot, offset distribution, delta, rolling average, return map, combo heatmap, autocorrelation, and XACC, with player filters, statistics, and per-hit details
- 返回关卡编辑器后可显示上一局的击打点，并支持悬停查看详情或从日志导入击打点

  View the previous run's hit markers in the level editor, inspect them on hover, or import hit markers from a log
- 可通过 F10（默认快捷键）打开上次击打报告；即使未启用日志记录，也能查看本局图表

  Press F10 (the default hotkey) to view the last-session report and its charts, even when logging is disabled
- 日志列表支持搜索和自动清理；界面支持韩语

  Search the log list, automatically clean up old logs, and use the Korean interface
- 新增平均偏移 (Avg) HUD 与命中跳动效果

  Adds an Avg offset HUD and a hit bump animation
- 支持本地多人 (co-op)

  Supports local co-op: per-player statistics
- 同时支持UMM和MelonLoader,以MelonLoader加载时使用`F9`呼出配置界面

  Supports UMM, MelonLoader and BepInEx; when loaded via MelonLoader or BepInEx, press `F9` to open the configuration interface.

## Screenshots
<details>
<summary>（点击展开 / Click to expand）</summary>
<img width="682" height="645" alt="屏幕截图 2026-09-06 000354" src="https://github.com/user-attachments/assets/6abc512e-3f7d-446d-ba2f-d314ffed2934" />

<img width="1721" height="1021" alt="屏幕截图 2026-09-06 000500" src="https://github.com/user-attachments/assets/62a7a0f1-ba1a-4445-b63a-ccd89d74e90a" />

<img width="1432" height="767" alt="屏幕截图 2026-09-06 000609" src="https://github.com/user-attachments/assets/56699d0d-fade-4f03-b5f8-37853f5d38d7" />

<img width="593" height="302" alt="屏幕截图 2026-09-06 000804" src="https://github.com/user-attachments/assets/11a60e56-e268-4049-863e-e0226a894758" />

<img width="1152" height="2583" alt="屏幕截图_6-9-2026_0851_sleepingcui github io" src="https://github.com/user-attachments/assets/cb8203a3-380a-4b8e-a23b-041c0eb10f7d" />

</details>

## 引用 & 代码参考 / References & Code Credits

- [Oerlayer](https://github.com/c3nb/Overlayer)
- [ProgressDisplayer](https://github.com/FLOWERs-Modding/ADOFAI_ProgressDisplayer2)
- [XPerfect](https://github.com/8100print/XPerfect)

## LICENSE

[MIT](LICENSE.txt)
