# TimingShow

面向冰与火之舞的Timing信息提示模组，支持 MelonLoader 和 UnityModManager。

## 功能

- 显示 Timing、UR、Perfect(XPerfect) Ratio 和 XACC曲线图。
- 支持在歌曲标题、星球判定文字、HUD、死亡页和通关页显示。
- 可自定义显示内容、颜色、位置、缩放、格式和精度。
- 支持 XPerfect 判定
- 支持 Timing 日志，可在[Offset Analyzer](https://sleepingcui.github.io/adofai_offset_analyzer)获取详细的分析报告 (包括偏移散点图，正态分布图，XACC曲线等指标)
- 同时支持UMM和MelonLoader,以MelonLoader加载时使用`F9`呼出配置界面

## 版本兼容

同一份 DLL 同时兼容游戏 **3.4 及以后**与 **3.4 以前**的版本，启动时自动识别游戏 `HitMargin` 枚举版本，无需分别下载。

两者的 XPerfect 来源不同：

| 游戏版本 | XPerfect 来源 |
|---|---|
| 3.4 及以后 | **游戏原生** `HitMargin.XPerfect`，不再使用 CalcXP 推导 |
| 3.4 以前 | 优先读取外部 XPerfect 模组；不可用时回退到内置 CalcXP 算法 |

因此「高级功能 → XPerfect Hook 模式」只对 3.4 以前的版本有意义，3.4 下该选项不适用（会显示为不适用，且不会回退到 CalcXP）。

如果游戏枚举既不是已知旧版也不是 3.4，模组会进入安全模式：无法识别的判定不计入统计、不参与着色、不替换星球文字，日志中仍保留原始数值。

## Screenshots
<details>
<summary>（点击展开）</summary>

<img width="682" height="645" alt="屏幕截图 2026-09-06 000354" src="https://github.com/user-attachments/assets/6abc512e-3f7d-446d-ba2f-d314ffed2934" />

<img width="1721" height="1021" alt="屏幕截图 2026-09-06 000500" src="https://github.com/user-attachments/assets/62a7a0f1-ba1a-4445-b63a-ccd89d74e90a" />

<img width="1432" height="767" alt="屏幕截图 2026-09-06 000609" src="https://github.com/user-attachments/assets/56699d0d-fade-4f03-b5f8-37853f5d38d7" />

<img width="593" height="302" alt="屏幕截图 2026-09-06 000804" src="https://github.com/user-attachments/assets/11a60e56-e268-4049-863e-e0226a894758" />

<img width="1152" height="2583" alt="屏幕截图_6-9-2026_0851_sleepingcui github io" src="https://github.com/user-attachments/assets/cb8203a3-380a-4b8e-a23b-041c0eb10f7d" />

</details>

## 引用 & 代码参考

- [Oerlayer](https://github.com/c3nb/Overlayer)
- [ProgressDisplayer](https://github.com/FLOWERs-Modding/ADOFAI_ProgressDisplayer2)
- [XPerfect](https://github.com/8100print/XPerfect)

## LICENSE

 [MIT](LICENSE.txt)
