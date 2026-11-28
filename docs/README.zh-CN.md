# TimingShow

[English](../README.md)

《冰与火之舞》击打时机分析模组，支持 MelonLoader 和 UnityModManager（UMM）。

## 功能

### 游戏内显示

- 显示 Timing、UR、Perfect（XPerfect）Ratio 和 XACC 曲线图。
- 在歌曲标题、星球判定文字、HUD、死亡页和通关页显示数据。
- 自定义显示内容、颜色、位置、缩放、格式和精度。
- 记录 XPerfect、平均偏移（Avg）和本地多人各玩家的数据。
- HUD 支持命中跳动效果。

### 日志与分析

- 记录 Timing 日志，并在 [Offset Analyzer](https://sleepingcui.github.io/adofai_offset_analyzer) 查看详细报告，包括偏移散点图、分布图、XACC 等指标。
- 游戏中查看实时 Timing 散点图，也可从日志列表打开日志散点图。两种视图均支持平移缩放、单次击打详情和判定色带。
- 日志分析窗口提供九种视图：概览、散点图、偏移分布、Delta、滑动均值、Return Map、连击热力图、自相关和 XACC。支持按玩家筛选，并查看统计信息和逐击详情。
- 返回关卡编辑器后查看上一局的击打点，可悬停查看详情，也可从日志导入击打点。
- 启用编辑器击打点后，可用悬浮面板（默认按 F6）按地板、时间、玩家、偏差和判定搜索或筛选。
- 默认按 F10 打开上次击打报告；即使未启用日志记录，也能查看报告图表。
- 搜索日志列表、自动清理旧日志。

### 配置

- 可在游戏内配置模组。使用 MelonLoader 加载时，默认按 F9 打开设置界面。

## 更新

TimingShow 支持在游戏内检查正式版和 CI 开发构建。更新检查和自动更新默认关闭；下载的更新会在重启游戏后生效。

### CI 构建

> [!WARNING]
> CI 构建可提前体验尚未进入正式版的开发版本，可能包含 Bug、未完成的功能或兼容性问题；注重稳定性的用户不建议使用。

带有更新通道选择器的版本，可在更新设置中选择 **CI**，启用更新检查，然后点击 **立即检查更新**。

#### 在 v1.9.4 使用 CI 构建

> [!NOTE]
> v1.9.4 没有内置更新通道选择器，需按以下步骤配置自定义更新 URL 来使用 CI 更新源。

1. 打开 TimingShow 设置并展开 **Debug**。
2. 启用 **Custom Update URL**（自定义更新源 URL）。
3. 在出现的输入框中填入：

   ```text
   https://raw.githubusercontent.com/SleepingCui/ts_ci/main/Repository.json
   ```

4. 确认已启用更新检查，然后点击 **立即检查更新**。

之后，TimingShow 会通过该仓库检查 CI 更新。

## 截图

<details>
<summary>点击展开</summary>

<img width="682" height="645" alt="TimingShow 游戏内界面截图" src="https://github.com/user-attachments/assets/6abc512e-3f7d-446d-ba2f-d314ffed2934" />

<img width="1721" height="1021" alt="TimingShow 分析图表截图" src="https://github.com/user-attachments/assets/62a7a0f1-ba1a-4445-b63a-ccd89d74e90a" />

<img width="1432" height="767" alt="TimingShow 图表窗口截图" src="https://github.com/user-attachments/assets/56699d0d-fade-4f03-b5f8-37853f5d38d7" />

<img width="593" height="302" alt="TimingShow 紧凑面板截图" src="https://github.com/user-attachments/assets/11a60e56-e268-4049-863e-e0226a894758" />

<img width="1152" height="2583" alt="TimingShow 日志的 Offset Analyzer 报告" src="https://github.com/user-attachments/assets/cb8203a3-380a-4b8e-a23b-041c0eb10f7d" />

</details>

## 引用与代码致谢

- [Overlayer](https://github.com/c3nb/Overlayer)
- [XPerfect](https://github.com/8100print/XPerfect)
- [YCH](https://github.com/adofaiex/YqlossClientHarmony)

## 许可证

[MIT](../LICENSE.txt)
