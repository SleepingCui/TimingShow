# TimingShow 发布文案

本文件包含三部分：简介、README、详细介绍。README 与详细介绍都是完整 markdown 全文，直接复制对应代码块里的内容即可。

## 一、简介

**标准版（推荐，约 60 字）**

> TimingShow 是《冰与火之舞》的击打分析模组：实时显示 Timing、平均偏移、UR、Perfect(XPerfect) Ratio 与 XACC 曲线，记录逐击日志，并能在游戏内查看九种图表、在编辑器回看上一局击打点。

**短版（约 25 字）**

> 把每一击的 Timing 变成看得见的数据。《冰与火之舞》击打分析模组，支持 UMM / MelonLoader。

**长版（约 110 字）**

> 把每一击的 Timing 变成看得见的数据。TimingShow 实时显示 Timing、平均偏移、UR、Perfect(XPerfect) Ratio 与 XACC 曲线，逐击记录日志，提供游戏内散点图与九视图日志分析、编辑器击打点回看和网页端 Offset Analyzer 报告；同时支持 UnityModManager 与 MelonLoader，界面含简体中文、English 与 한국어。

## 二、README（全文）

````markdown
# TimingShow

把每一击的 Timing 变成看得见的数据。

*Every tap's timing, made visible.*

面向《冰与火之舞》(A Dance of Fire and Ice) 的击打分析模组：实时显示 Timing、平均偏移、UR、Perfect(XPerfect) Ratio 与 XACC 曲线，记录逐击日志，并提供游戏内图表窗口、编辑器击打点和网页分析报告。

A hit-timing analysis mod for *A Dance of Fire and Ice*: live readouts, XACC curves, per-hit logs, an in-game chart window, editor hit markers, and web reports.

[![Release](https://img.shields.io/github/v/release/SleepingCui/TimingShow?display_name=tag&sort=semver)](https://github.com/SleepingCui/TimingShow/releases)
[![Downloads](https://img.shields.io/github/downloads/SleepingCui/TimingShow/total)](https://github.com/SleepingCui/TimingShow/releases)
[![License](https://img.shields.io/github/license/SleepingCui/TimingShow)](https://github.com/SleepingCui/TimingShow/blob/master/LICENSE.txt)

支持 UnityModManager 与 MelonLoader · 简体中文 / English / 한국어

**简体中文** | [English](#english)

> 下载：[Releases](https://github.com/SleepingCui/TimingShow/releases) · 更新记录：[CHANGELOG](https://github.com/SleepingCui/TimingShow/blob/master/CHANGELOG.md)

## 功能

### 实时读数

- Timing、平均偏移 (Avg)、UR、Perfect(XPerfect) Ratio 四个 HUD 可独立开关，位置、缩放、字体、加粗、对齐、显示格式与小数位均可自定义
- XACC 曲线图，可调采样点数、渲染点数、点大小与配色
- 命中跳动与按判定着色，可选以角度显示

### 显示位置

- 歌曲标题、星球判定文字、游戏 HUD、死亡页与结算页
- 本地多人 (co-op)：按玩家分别统计，结算页可每页显示全员

### 判定

- XPerfect：游戏 3.4 起直接使用原生判定；旧版本使用内置算法，也可开启 Hook 读取 [XPerfect](https://github.com/8100print/XPerfect) 模组的数据
- 判定文字替换：把 Early、Late、Perfect 等替换成自定义文案，可按判定单独开关
- 图表中的判定窗口与 XPerfect 色带，可按难度与 BPM 自动计算，也可手动指定

### 图表

- 游戏内实时 Timing 散点图：暂停或结算后拖动平移、滚轮缩放，点选查看单次击打详情
- 日志图表窗口提供九种视图：概览、散点图、分布图、XACC 曲线图、Δ 分布、滑动均值、偏移关系、连击热力条、自相关
- 玩家筛选与范围筛选、0 ms 参考线与平均值线、判定色带与 XPerfect 色带、逐击详情
- 图表窗口可调配色、UI 缩放、圆角与字体

### 日志

- 逐击记录 Timing、时间、判定、地板编号、BPM、速度、音高与玩家
- JSON (v8) 或二进制 (v9) 两种格式，兼容读取旧日志
- 日志列表支持搜索、按时间/大小/歌名排序、打开文件夹、删除与自动清理
- 一键 Web 分析：通过本地服务把日志送入 [Offset Analyzer](https://sleepingcui.github.io/adofai_offset_analyzer)，生成偏移散点图、正态分布与 XACC 报告

### 回看

- 上次击打报告（默认 `F10`）：通关、失败或回到编辑器后查看本局图表，未开启日志记录同样可用
- 关卡编辑器击打点：显示上一局的击打位置，悬停查看地板、角度、BPM、旋转方向与 Timing，也可从日志导入或清除

## 安装

一个压缩包同时包含两种加载器，按已安装的加载器解压即可，不要同时安装。

### UnityModManager

1. 安装 UMM 并配置好游戏目录
2. 解压 `TimingShow.zip` 到 `Mods/TimingShow/`，确认 `Info.json` 与 dll 同级
3. 启动游戏，在 UMM 的 Mods 列表里启用 TimingShow，点开即配置界面

### MelonLoader

1. 安装 MelonLoader（构建基于 0.7.3）
2. 解压 `TimingShow.zip` 到 `Mods/`；若日志出现 UMM 加载器相关报错，删除 `TimingShow.Loader.UMM.dll` 与 `Info.json` 即可
3. 启动游戏，按 `F9` 打开配置界面

安装后大部分功能默认关闭，请先在配置界面打开需要的 HUD、曲线或散点图。

## 快捷键

| 按键 | 作用 | 说明 |
| --- | --- | --- |
| `F9` | 打开 / 关闭配置界面 | MelonLoader 版本；可在配置里改键，UMM 版本使用 UMM 自带的 Mods 面板 |
| `F10` | 打开 / 关闭上次击打报告 | 通关、失败或回到编辑器等非游玩状态可用；可在配置里改键 |

## 日志与报告

- 日志默认保存在 `Mods/TimingShow/Logs`，可在配置里改路径
- 开启「启用 Timing 记录」后逐击写盘；「使用 JSON 日志格式」切换为可读文本，体积更大
- 日志列表的「查看分析」打开游戏内图表窗口，「Web 分析」用浏览器打开 Offset Analyzer
- Web 分析依赖本地服务（日志设置中的「启用 Analyzer 本地服务」），端口与超时可调；关闭后不会启动服务

## 常见问题

**装好了但界面没有任何数字？**
实时 HUD、XACC 曲线、散点图默认关闭，请在配置界面里打开，并确认只装了一种加载器。

**判定文字没有被替换？**
需要开启「替换判定显示」；改动后重新进入关卡编辑器生效（自动重载编辑器默认关闭）。

**XPerfect 不显示？**
游戏 3.4 之前需要开启「启用 XPerfect」；Hook 模式需要已安装 XPerfect 模组，未安装时自动回退到内置算法。

**F10 没有反应？**
击打报告只能在通关、失败或回到编辑器等非游玩状态打开；若与游戏或其他模组按键冲突，请在配置里改键。

**Web 分析页面打不开？**
确认已启用 Analyzer 本地服务且端口未被占用；页面通过 `127.0.0.1` 读取日志，代理或防火墙可能拦截。

## 截图

<details>
<summary>（点击展开）</summary>
<img width="682" height="645" alt="屏幕截图 2026-09-06 000354" src="https://github.com/user-attachments/assets/6abc512e-3f7d-446d-ba2f-d314ffed2934" />

<img width="1721" height="1021" alt="屏幕截图 2026-09-06 000500" src="https://github.com/user-attachments/assets/62a7a0f1-ba1a-4445-b63a-ccd89d74e90a" />

<img width="1432" height="767" alt="屏幕截图 2026-09-06 000609" src="https://github.com/user-attachments/assets/56699d0d-fade-4f03-b5f8-37853f5d38d7" />

<img width="593" height="302" alt="屏幕截图 2026-09-06 000804" src="https://github.com/user-attachments/assets/11a60e56-e268-4049-863e-e0226a894758" />

<img width="1152" height="2583" alt="屏幕截图_6-9-2026_0851_sleepingcui github io" src="https://github.com/user-attachments/assets/cb8203a3-380a-4b8e-a23b-041c0eb10f7d" />

</details>

## 构建

需要 Visual Studio Build Tools（MSBuild）与单独提供的 `libs/` 依赖：

```bat
build.bat
```

产物为 `TimingShow.zip`。几何检查：

```bat
dotnet run --project tools/GeometryTests/GeometryTests.csproj
```

## 引用 & 代码参考

- [Overlayer](https://github.com/c3nb/Overlayer)
- [ProgressDisplayer](https://github.com/FLOWERs-Modding/ADOFAI_ProgressDisplayer2)
- [XPerfect](https://github.com/8100print/XPerfect)

## LICENSE

[MIT](https://github.com/SleepingCui/TimingShow/blob/master/LICENSE.txt)

## English

**TimingShow** is a hit-timing analysis mod for *A Dance of Fire and Ice*. It shows live Timing, average offset, UR, Perfect (XPerfect) ratio and XACC curves, logs every hit, and provides an in-game chart window, editor hit markers, and web reports.

- Four toggleable HUDs (Timing, Avg, UR, Ratio) with customizable position, scale, font, format and precision
- XACC curve and a real-time Timing scatter plot with pan, zoom and per-hit details
- Judgement text replacement; XPerfect support (native from game 3.4, built-in algorithm on older versions, optional hook into the XPerfect mod)
- Log chart window with nine views: overview, scatter, distribution, XACC curve, delta, rolling average, return map, combo strip, autocorrelation
- Per-player and range filters, judgement and XPerfect bands, per-hit details
- Per-hit logs in JSON or binary format, searchable and sortable, with auto-cleanup and one-click web analysis via Offset Analyzer
- Last-session report on `F10`, hit markers from the previous run in the level editor, local co-op support
- Simplified Chinese, English and Korean interface with bundled fonts; UnityModManager and MelonLoader support

**Install** — extract the release zip into `Mods/TimingShow/` for UMM, or into `Mods/` for MelonLoader (built against 0.7.3). Install only one loader. Most features are disabled by default: open the configuration panel and enable what you need.

**Hotkeys** — `F9` toggles the settings panel (MelonLoader build), `F10` toggles the last-session report. Both are rebindable. The report is available outside of active gameplay (after clearing, failing, or back in the editor).
````

## 三、详细介绍（全文）

````markdown
# TimingShow 详细介绍

> 一句话：把每一击的 Timing 变成看得见的数据。

TimingShow 是《冰与火之舞》(A Dance of Fire and Ice) 的击打分析模组。它只读取游戏里的击打数据，不改判定、不改判定窗口，做的事只有一件：把每一次击打记录下来，再用数字、曲线和图表回答——我到底打得准不准、稳不稳、偏在哪一段。

支持 UnityModManager 与 MelonLoader，界面提供简体中文、English 与 한국어。

## 适合谁

- 想知道自己的准确度和稳定性到底如何，而不只是看评级
- 在练 XPerfect、冲 UR，想要看得见的进步曲线
- 换了设备或调了延迟，想确认平均偏移有没有变化
- 做谱、测手感，想回看一局里从哪一段开始崩
- 想复盘多人 (co-op) 里每位玩家的表现

## 一、实时读数：四个 HUD 和一条曲线

四个读数可以独立开关，也能同时显示在歌曲标题、星球判定文字、HUD、死亡页和结算页：

- **Timing**：最近一击的偏移，单位毫秒，可设小数位、显示格式，并按判定着色
- **平均偏移 (Avg)**：整局的平均偏移，正负号直接告诉你整体偏晚还是偏早；长期稳定的偏移通常来自设备或音频延迟，可以据此调整延迟设置
- **UR**：偏移标准差乘 10，越小越稳；比平均值更能反映稳定性
- **Ratio**：Perfect 占比，提供 Perfect、±Perfect、XPerfect 三种统计口径，显示为比例
- **XACC 曲线**：逐击的 XACC 百分比连成曲线，一局里的走势和崩点一眼可见

位置、缩放、字体、加粗、对齐、颜色、精度都可以调；还可以选择以角度显示，或打开命中跳动，让每一次击打都有反馈。

## 二、判定与 XPerfect

- 游戏 3.4 起自带 XPerfect 判定，模组直接读取，不需要额外设置
- 旧版本使用内置的 XPerfect 算法，与游戏判定并行计算
- 如果已经安装了 XPerfect 模组，可以开启 Hook 模式，直接读取它的判定数据（模组声明了 `LoadAfter XPerfect`），避免两套算法的细微差异；未安装时自动回退到内置算法
- 判定文字替换：把 Early、Late、Perfect、多次按键、失误、过多按键、自动播放等逐项替换成自定义文案，方便做自己的显示风格
- 判定色带与 XPerfect 色带：图表背景里的分区，默认按难度与 BPM 自动计算，也可以手动指定阈值 BPM
- 判定集与游戏一致，从 TooEarly 到 TooLate，另外区分 Perfect−、Perfect+、XPerfect 与各种失误类型

## 三、图表：一局到底发生了什么

游戏内的实时散点图不用等日志：打出每一击就落一个点，暂停或结算后可以拖动平移、滚轮缩放，点选查看单次击打详情。

日志图表窗口提供九种视图，从不同角度看同一局：

| 视图 | 看什么 | 怎么用 |
| --- | --- | --- |
| 概览 | 总击打数、最大连击、UR、Ratio、XPerfect Ratio、谱面与日志元信息 | 先看整体；打开「忽略离群点」避免个别爆点带偏统计 |
| 散点图 | 每一击一个点，纵轴是偏移，横轴是击打序号 | 看落点是否成带、有没有系统性偏移；配合判定色带看落在哪一档 |
| 分布图 | 偏移直方图 + 均值、标准差、偏度、峰度 | 均值偏正/负是整体偏晚/偏早，标准差大是不稳，偏度看拖尾方向 |
| XACC 曲线图 | XACC 随击打序号的变化 | 看稳定性走势：一路平稳，还是中段开始掉 |
| Δ 分布 | 相邻两击的偏移差 | 衡量随机抖动，和分布图互补 |
| 滑动均值 | 窗口均值 ±1σ，并对比前 25% 与后 25% 的均值 | 看漂移：前后段差得多，说明越打越偏或越打越稳 |
| 偏移关系 | 上一击偏移 vs 当前偏移，以及相关系数 r | r 明显为负是过度修正，为正说明偏差在延续，接近 0 则基本独立 |
| 连击热力条 | 判定序列 | 断连位置一眼可见，快速定位崩点 |
| 自相关 | 不同 lag 的相关系数与 95% 置信带 | 找周期性误差，比如每隔几击就重复同样的偏差 |

操作上：顶部切换视图，右上角固定显示总击打数、UR、Ratio 与 XACC；多人时可以按玩家筛选；范围筛选框可以只统计某个偏移区间或某段击打；0 ms 参考线、平均线、网格、边框、判定色带、XPerfect 色带都能单独开关；图表设置里可以调配色、UI 缩放、圆角与字体。

几个实用的看法：

- 平均偏移长期是 -20 ms 左右，先怀疑设备和延迟，而不是手感
- 分布图很窄但整体偏一侧，是系统性偏移；分布图很宽，是稳定性问题
- 偏移关系的 r 明显为负，说明你在过度修正，越想打准反而左右摇摆
- 滑动均值前段和后段差很多，通常是体力或注意力在中后段掉了
- 自相关在某个 lag 上有明显尖峰，往往是跟着某段节奏形成了固定的误差周期

## 四、日志：把这一局留下来

开启「启用 Timing 记录」后，模组会把每一击写进日志：

- 记录玩家、击打序号、Timing、时间、判定、地板编号、BPM、速度、音高，角度模式下还会记录角度
- 默认二进制格式，体积小；切换成 JSON 后是可读文本，方便自己写脚本处理
- JSON 格式版本 v8、二进制版本 v9，旧日志仍然可以读取；缺地板编号的旧日志无法在编辑器里定位击打点，但图表分析不受影响
- 日志列表支持搜索、按时间/大小/歌名排序、打开文件夹、删除，以及自动清理（只保留前 N 个文件）
- 超出记录上限的日志会被截断，元信息里会标明

Web 分析把日志变成网页报告：模组在本地起一个临时服务，把日志交给 [Offset Analyzer](https://sleepingcui.github.io/adofai_offset_analyzer/)，在浏览器里生成偏移散点图、正态分布、XACC 曲线等报告。端口可以指定（0 表示随机），超时可调；不开启服务时不会启动任何进程。也可以直接打开日志文件，在网页里手动上传。

## 五、回到编辑器：击打点

从关卡编辑器返回后，模组会把上一局的击打点画在轨道上：

- 每个点是一圈虚线圆环，颜色按判定，大小与角度倍率可调
- 鼠标悬停显示地板、角度、BPM、旋转方向、Timing、判定与时间
- 可以从任意一份日志导入击打点（会校验日志与当前编辑器的谱面是否一致），也可以一键清除
- 适合定位「总是偏早/偏晚」的具体位置，配合修改或针对性练习

## 六、F10：随时翻出上一局

通关、失败或回到编辑器后，按 `F10`（默认键，可改）打开本局的击打报告，内容和日志图表窗口一致。它不依赖日志文件：即使关闭了记录，本局的样本仍在内存里，一样能看图表。进入下一关时会自动关闭，避免挡视线。设置里也有「查看上次击打报告」按钮。

## 七、多人、语言与外观

- 本地多人 (co-op)：每位玩家单独统计，HUD 可以按玩家分列，结算页可以选择每页显示全员
- 界面支持简体中文、English、한국어；内置思源黑体与 Gmarket Sans，缺字时回退系统字体
- 图表窗口使用圆角、羽化抗锯齿、渐变与阴影，可按屏幕调整 UI 缩放
- HUD 与图表都可以使用自定义字体

## 八、安装与第一次使用

1. 选择加载器：UMM 或 MelonLoader，只装一种
2. UMM：解压到 `Mods/TimingShow/`，在 UMM 的 Mods 列表启用；MelonLoader：解压到 `Mods/`，进游戏按 `F9`
3. 第一次打开配置，先启用 Timing HUD、XACC 曲线和实时散点图，打一首歌看看效果
4. 想认真分析时再开启日志记录；打完按 `F10` 看本局报告，或到日志列表点「查看分析」

## 九、常见问题

**装好了但没有任何数字？**
实时 HUD、曲线、散点图默认都是关闭的，请在配置界面里打开；同时确认没有同时安装两种加载器。

**判定文字没有被替换？**
需要开启「替换判定显示」，改动后重新进入关卡编辑器生效；「自动重载编辑器」默认关闭，打开后切换开关会自动重载。

**XPerfect 不显示？**
游戏 3.4 之前需要开启「启用 XPerfect」；Hook 模式需要已安装 XPerfect 模组，未安装会自动回退到内置算法。

**F10 没有反应？**
击打报告只能在通关、失败或回到编辑器等非游玩状态打开；如果和游戏或其他模组按键冲突，请在配置里改键。

**Web 分析页面打不开？**
确认已启用 Analyzer 本地服务，端口没有被占用；页面通过 `127.0.0.1` 读取日志，代理或防火墙可能拦截。

**日志太大？**
改用二进制格式、开启自动清理并设置保留份数，或者把日志目录换到其他磁盘。

**会不会影响性能？**
图表窗口有采样点与渲染点上限，日志列表按帧分批绘制；觉得卡可以关闭「始终滚动」、降低渲染点数，或只在需要时打开图表窗口。

## 致谢

- [Overlayer](https://github.com/c3nb/Overlayer)
- [ProgressDisplayer](https://github.com/FLOWERs-Modding/ADOFAI_ProgressDisplayer2)
- [XPerfect](https://github.com/8100print/XPerfect)

分析与网页报告：[Offset Analyzer](https://sleepingcui.github.io/adofai_offset_analyzer/)

更新记录见 [CHANGELOG](https://github.com/SleepingCui/TimingShow/blob/master/CHANGELOG.md)，许可证为 [MIT](https://github.com/SleepingCui/TimingShow/blob/master/LICENSE.txt)。
````
