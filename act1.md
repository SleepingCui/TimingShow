# MelonLoader 自动更新实现方案（方案 1：MelonPlugin 引导）

## 1. 目标

让 MelonLoader 版本获得与 UMM 版本一致的体验：

- 游戏内检查更新（Repository.json）
- 提示新版本、下载、SHA-256 校验、解压校验
- 重启游戏后自动应用，无需手动替换文件
- 失败可回滚，不破坏现有安装

不在本方案范围内：更新 Bootstrap 插件自身、退出游戏后由外部进程替换、Melon 以外的其他加载器。

## 2. 前置验证（开工门禁）

以下三项决定方案是否成立，开工前必须实测确认；

1. **插件生命周期**
   - 确认当前 MelonLoader 版本的 `MelonPlugin` 在 `MelonMod` 程序集加载前完成初始化。
   - 预期入口：`MelonPlugin.OnPreInitialization()`；若版本不提供，退到 `OnInitializeMelon()`。
   - 判定方法：Bootstrap 日志与现有 `coop players = ...` / mod 加载日志的先后顺序。
   - 若插件初始化发生在 mod 程序集加载之后，且文件被锁定，则方案 1 失败，改用外部辅助进程方案。

2. **文件锁**
   - 游戏运行时尝试重命名/替换 `Mods/TimingShow.dll`、`Mods/TimingShow.Loader.Melon.dll`。
   - 可替换：核心可尝试立即应用，Bootstrap 只是兜底。
   - 被锁定：所有 DLL 更新都必须留到重启后，由 Bootstrap 在 mod 加载前处理。

3. **目录扫描**
   - 确认 MelonLoader 是否递归加载 `Mods/**/*.dll`。
   - 若是，`Update/staging`、`Update/backup-*` 里的 DLL 可能被当成第二个 mod 加载，更新工作目录必须放在 Mods 之外。

### 结论（2026-11-07，依据 MelonLoader 官方仓库源码，未接触游戏资产）

1. **插件生命周期：成立。** `MelonLoader/Core.cs` 的启动顺序是：插件目录扫描与加载 → `MelonEvents.OnPreInitialization.Invoke()`（Core.cs:182）→ `LoadMelons(ScanType.Mods)`（Core.cs:212）→ `OnApplicationStart`。`MelonPlugin.OnPreInitialization()` 因此早于任何 mod 程序集被加载，是替换文件的唯一安全窗口。
2. **文件锁：按"被锁定"分支实现。** Windows 上已加载的 mod 程序集不可替换，运行中立即应用会失败；实现上先尝试应用，失败则保留 `pending.txt` 与 `staging/`，由下次启动的 Bootstrap 在插件初始化阶段完成替换。
3. **目录扫描：Mods 必须平铺安装，更新目录仍放在 Mods 之外。** 实测（2026-11-07 用户日志）：把 mod DLL 放进 `Mods/TimingShow/` 后 MelonLoader 输出 `Loading Mods...` → `0 Mods loaded.`，说明当前版本只加载 `Mods/` 根目录下的程序集，不会下钻子目录。因此 Melon 安装形态固定为 `Mods/TimingShow.dll`、`Mods/TimingShow.Loader.Melon.dll`、`Mods/lang.json`。更新目录仍固定为 `UserData/TimingShow/Update`：Bootstrap 与游戏运行时的 mod 程序集不共享扫描根，staging/backup 不会被当成 mod 加载。
结论写入本节后再进入实现。

## 3. 总体设计

### 3.1 组件

```
TimingShow.dll（核心，加载器无关）
├─ Updater            检查/下载/状态机/UI 通知
├─ UpdateClient       HTTP、版本比较、SHA-256
└─ UpdatePackage      解压、校验、manifest、pending、立即应用

TimingShow.Loader.Melon.dll
└─ 初始化 Updater，传入 Melon 版本、仓库地址、更新目录、目标类型

TimingShow.UpdateBootstrap.dll（新，MelonPlugin，放 Plugins/）
└─ 启动时在 mod 加载前读取 pending 并替换文件，不引用核心

PendingUpdateApplier（共享源码，链接编译进核心、UMM loader、Bootstrap）
└─ 纯文件逻辑：manifest 校验、backup、替换、回滚、清理
```

### 3.2 为什么 Bootstrap 必须是独立插件

- `PendingUpdate` 必须替换 `TimingShow.dll`，因此不能先加载核心，否则文件被锁。
- 也不能复用 `TimingShow.Loader.Melon.dll` 中的逻辑：它自身也在更新范围内。
- MelonPlugin 位于 `Plugins/`，不在更新文件集合内，可以在 mod 加载前安全执行文件替换。
- Bootstrap 只做文件操作，不加载任何待替换程序集，也不访问游戏资产。

### 3.3 更新目录

Melon 使用游戏用户数据目录，避免 Melon 扫描到 staging/backup 中的 DLL：

```
<Game>/UserData/TimingShow/Update/
├─ download/TimingShow-<version>.zip
├─ staging/
├─ backup-<fromVersion>/
└─ pending.txt
```

UMM 保持现状：`<modPath>/Update/`。

`Updater.Initialize` 接收 `updateRoot` 参数，核心内部所有 `Path.Combine(modPath, "Update")` 改为使用该值。

Melon 侧路径：

```csharp
Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow", "Update")
```

Bootstrap 必须计算出同一路径。若 `MelonEnvironment` 在目标版本不可用，退回 `Path.Combine(gameRoot, "UserData", "TimingShow", "Update")`，并在启动日志中输出实际路径。

## 4. 详细改动

### 4.1 核心：`Updater.cs`

新增目标类型与初始化参数：

```csharp
public enum UpdateTarget
{
    Umm,
    Melon
}

public static void Initialize(
    string modPath,
    string updateRoot,
    string repositoryUrl,
    string currentVersion,
    UpdateTarget target,
    bool autoApplyAvailable,
    IModLogger logger)
```

- `autoApplyAvailable=false` 时（Melon 未安装 Bootstrap）设置 `Supported=false`，并记录不可用原因 key，避免用户下载了却无法应用。
- 新增 `_updateRoot`、`_target` 字段，所有 `DownloadWorker` 路径改用 `_updateRoot`。
- `Initialize` 末段行为不变：`UpdateHost.EnsureHost()`，按 `CheckEnabled` 开始检查。
- 新增 `public static string UnavailableReasonKey`，默认 `"Updater_Unsupported"`，Bootstrap 缺失时为 `"Updater_BootstrapMissing"`。

### 4.2 核心：`UpdatePackage.cs`

按目标区分必需文件与应用文件：

```csharp
public static string[] GetRequiredFiles(UpdateTarget target)
public static bool IsApplicableFile(string relativePath, UpdateTarget target)
```

| 目标 | 必需文件 | 应用文件 |
|---|---|---|
| Umm | `TimingShow.dll`、`TimingShow.Loader.UMM.dll`、`Info.json`、`lang.json` | 同左 |
| Melon | `TimingShow.dll`、`TimingShow.Loader.Melon.dll`、`lang.json` | 同左 |

- `Verify(staging, modPath, release, target)`：
  - 公共：必需文件存在且非空；`lang.json` 可解析；核心程序集名称正确；包大小/SHA 已由下载阶段校验。
  - UMM：保留现有 `Info.json` 的 Id / Version / EntryMethod / AssemblyName 比较。
  - Melon：校验打包的 `TimingShow.Loader.Melon.dll` 存在、非空，`AssemblyName.GetAssemblyName` 名称正确；不要求 `Info.json`，不比较 EntryMethod。
  - Melon 不在本阶段读取 `MelonInfo` 版本；发布版本一致性由 Repository 的 `Version` + 整包 SHA-256 保证。
- `WriteManifest(staging, target)`：只写入目标可应用文件，避免把 UMM 文件写进 Melon 安装目录，也避免把 Bootstrap 写进 mod 目录。
- `WritePending(modPath, version, fromVersion, target)`：文件格式升级为：

```
version=<目标版本>
from=<当前版本>
target=Melon
path=<modPath 绝对路径>
```

  - UMM 旧读取逻辑忽略新增行，保持兼容。
  - `path=` 让 Bootstrap 不依赖安装目录猜测；应用前必须校验该目录存在且包含对应 loader DLL。
- `ApplyStaged(modPath, updateRoot, stagingDirectory, fromVersion)`：改为调用共享的 `PendingUpdateApplier`，不再维护第二套替换/回滚代码。

### 4.3 新增共享源码：`TimingShow.Loader.Shared/PendingUpdateApplier.cs`

纯 `System.IO` 实现，无核心、无 UMM、无 Melon 依赖。以 `<Compile Include="..\TimingShow.Loader.Shared\PendingUpdateApplier.cs" Link="PendingUpdateApplier.cs" />` 链接进：

- `TimingShow.csproj`
- `TimingShow.Loader.UMM.csproj`
- `TimingShow.UpdateBootstrap.csproj`

API：

```csharp
internal static class PendingUpdateApplier
{
    public static bool Apply(
        string updateRoot,
        string fallbackModPath,
        Action<string> log,
        Action<string> error);
}
```

流程：

1. 无 `pending.txt` → 返回 `false`。
2. 读取 `version` / `from` / `target` / `path`；`path` 缺失时使用 `fallbackModPath`。
3. 校验 staging、manifest 存在。
4. 逐行解析 `sha256<TAB>relative`：
   - 源路径必须位于 staging 前缀下。
   - 目标路径必须位于 modPath 前缀下（新增校验，现有 UMM 逻辑缺少该检查）。
   - 源文件存在且 SHA-256 匹配。
5. 备份目标文件到 `updateRoot/backup-<from>`，替换为 staging 文件。
6. 任一替换失败：回滚已替换文件，保留 pending/staging，返回 `false` 并抛出/记录原因，等待下次启动。
7. 全部成功：删除 pending、staging、download，清理旧 backup。
8. 哈希不符或 manifest 损坏：恢复 backup，删除 pending/staging，记录错误。

`UpdatePackage` 的立即应用路径复用同一实现，失败时按现有逻辑进入 `Ready` 状态（重启后由 Bootstrap 完成）。

### 4.4 UMM loader 适配

- `CoreStart.cs`：

```csharp
Updater.Initialize(
    modEntry.Path,
    Path.Combine(modEntry.Path, "Update"),
    modEntry.Info.Repository,
    modEntry.Info.Version.ToString(),
    UpdateTarget.Umm,
    autoApplyAvailable: true,
    logger);
```

- `PendingUpdate.cs` 改为薄封装，调用 `PendingUpdateApplier.Apply(modPath/Update, modEntry.Path, log, error)`；不再保留重复实现。

### 4.5 Melon loader 适配

`MelonEntry.OnInitializeMelon`：

```csharp
string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
string updateRoot = Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow", "Update");
bool bootstrapInstalled = File.Exists(Path.Combine(
    MelonEnvironment.GameRootDirectory, "Plugins", "TimingShow.UpdateBootstrap.dll"));

Updater.Initialize(
    modPath,
    updateRoot,
    Updater.DefaultRepositoryUrl,
    Info.Version,
    UpdateTarget.Melon,
    bootstrapInstalled,
    logger);
```

- 调用位置在 `ModContext.Enable()` 之后，保证 Unity 对象与 Harmony 已就绪。
- Bootstrap 缺失时仍加载 mod，但更新开关禁用，UI 显示 `Updater_BootstrapMissing` 文案，日志给出明确安装提示。
- `MelonEnvironment` 的具体命名空间/属性按前置验证结果确认。

### 4.6 新增 Bootstrap 项目

`TimingShow.UpdateBootstrap/TimingShow.UpdateBootstrap.csproj`

- 旧式 csproj，`TargetFrameworkVersion v4.8.1`，`AssemblyName TimingShow.UpdateBootstrap`
- 引用 `..\libs\MelonLoader.dll`（`Private=False`）
- 链接编译 `TimingShow.Loader.Shared/PendingUpdateApplier.cs`
- 不引用 `TimingShow.csproj`

`BootstrapEntry.cs`：

```csharp
public sealed class BootstrapEntry : MelonPlugin
{
    public override void OnPreInitialization()
    {
        string updateRoot = Path.Combine(MelonEnvironment.UserDataDirectory, "TimingShow", "Update");
        try
        {
            PendingUpdateApplier.Apply(updateRoot, null, MelonLogger.Msg, MelonLogger.Error);
        }
        catch (Exception e)
        {
            MelonLogger.Error("[UpdateBootstrap] apply failed: " + e);
        }
    }
}
```

`Properties/AssemblyInfo.cs`：

```csharp
[assembly: MelonInfo(typeof(TimingShow.UpdateBootstrap.BootstrapEntry),
    "TimingShow Update Bootstrap", "1.0.0", "SleepingCui")]
[assembly: MelonGame("7th Beat Games", "A Dance of Fire and Ice")]
```

要点：

- 所有异常必须吞掉并记录，Bootstrap 不能阻止游戏启动。
- 不加载任何待替换 DLL；只用 `File` / `SHA256`。
- 日志统一 `[UpdateBootstrap]` 前缀，输出实际 updateRoot、pending 版本、应用结果。
- Bootstrap 版本固定，不参与自动更新；仅当应用逻辑变更时手动发布新 DLL，并在更新说明中要求手动替换。

## 5. 完整流程

1. 游戏启动，Melon 加载 Plugins → Bootstrap 先执行，检查并应用遗留 pending。
2. Melon 加载 `Mods/TimingShow.Loader.Melon.dll`，`OnInitializeMelon` 初始化 Updater。
3. 若启用更新：后台请求 Repository.json，比较 `Info.Version`，状态变为 Available。
4. 用户点「更新」或自动更新：下载 zip 到 `UserData/TimingShow/Update/download`。
5. SHA-256、大小校验通过后解压到 `staging`，按 Melon 目标校验文件并生成过滤后的 manifest。
6. 写入 `pending.txt`（含目标、版本、modPath）。
7. 尝试立即应用：
   - 成功：清理 pending/staging，提示重启。
   - 文件被锁：回滚已替换文件，保留 pending，提示重启。
8. 重启后 Bootstrap 在 mod 加载前完成替换，新版本生效。
9. UI 文案沿用 `Updater_Pending`；若 Bootstrap 缺失，更新入口置灰并提示安装。

## 6. 校验与安全

- 仍只信任 `github.com` / `githubusercontent.com`，仅 HTTPS。
- 整包 SHA-256 与 `Size` 校验不变。
- 解压路径穿越校验不变。
- 新增 manifest 目标路径穿越校验。
- manifest 每个文件再次校验 SHA-256，防止 staging 被篡改。
- Bootstrap 校验 `pending.txt` 中的 `path`：目录存在且包含 `TimingShow.Loader.Melon.dll` 才继续。
- 不执行、不反射加载 staging 中的任何程序集。
- 更新目录在 UserData 下，避免 Melon 扫描到临时 DLL。

## 7. 兼容性与边界

- **Melon 安装**：`TimingShow.Melon.zip` 已包含 `Plugins/TimingShow.UpdateBootstrap.dll`，解压到游戏根目录即可；用户手动删除该文件时插件仍可正常加载，只是自动更新不可用。
- **安装形态**：MelonLoader 只扫描 `Mods/` 根目录，因此 Melon 必须平铺安装（`Mods/TimingShow.dll`、`Mods/TimingShow.Loader.Melon.dll`、`Mods/lang.json`）。modPath 即 `Mods/`，`Settings.json` 也会落在 `Mods/` 根；更新目录仍在 UserData，不会污染扫描根。
- **UMM 行为**：除更新目录参数化与共享回滚逻辑外不变。
- **切换加载器**：更新包按目标过滤，UMM 安装不再顺带更新 Melon loader DLL；如需切换加载器，重新安装完整包。
- **多个安装共用 UserData**：`pending.txt` 带 `path`，最新一次更新覆盖旧 pending；属于可接受限制。
- **手动作业**：替换前用户关闭游戏即可避免大部分文件锁问题。

## 8. 构建、CI 与发布

- `TimingShow.slnx` 增加 `TimingShow.UpdateBootstrap/TimingShow.UpdateBootstrap.csproj`。
- `build.bat`：分别产出 `TimingShow.UMM.zip` 与 `TimingShow.Melon.zip`，Bootstrap 只进 Melon 包。
- `.github/workflows/build-dev.yml`：产出同样两个 zip 作为 artifact。
- `.github/workflows/build-release.yml`：校验并上传两个 zip；`Repository.json` 的 `TimingShow`（UMM）与 `TimingShow.Melon` 两条各自回写版本、下载地址、哈希与大小。
- `build2.ps1`：只构建并部署 UMM 版本到 `Mods/TimingShow`，不碰游戏 `Plugins/`；不运行该脚本，除非用户明确要求。
- 发布包结构：

```
TimingShow.UMM.zip
├─ TimingShow.dll
├─ TimingShow.Loader.UMM.dll
├─ Info.json
└─ lang.json

TimingShow.Melon.zip                 ← 用户直接解压到游戏根目录
├─ Mods/
│  ├─ TimingShow.dll
│  ├─ TimingShow.Loader.Melon.dll
│  └─ lang.json
└─ Plugins/
   └─ TimingShow.UpdateBootstrap.dll
```

Melon 包解压时按 `Mods/` 前缀归一化到 staging，`Plugins/` 下的内容不参与自动更新（运行中的 Bootstrap 无法自我替换）。

Melon 更新应用时按目标过滤，不会把 Bootstrap/UMM 文件写进 `Mods/`。

## 9. 文档与文案

- `lang.json` 新增：
  - `Updater_BootstrapMissing`：简体中文「自动更新组件缺失，请将 TimingShow.UpdateBootstrap.dll 放入 Plugins 目录」；英文、韩文同步。
- `UpdateSettingsUI`：不可用提示改为读取 `Updater.UnavailableReasonKey`，不再硬编码 `Updater_Unsupported`。
- README / 发布说明补充 Melon 安装与升级步骤。

## 10. 测试计划（注：<u>!!!重要!!!</u>请不要在这里过度浪费时间和token）

1. **前置验证**：插件初始化顺序、DLL 锁、Mods 递归扫描。
2. **Melon 全新安装**：无 pending、无更新 → 不产生额外文件。
3. **Melon 更新全流程**：用测试仓库发布旧版本 + 新版本，验证检查、下载、校验、pending、重启应用、版本号变化。
4. **立即应用失败路径**：保持游戏运行锁住 DLL，确认回滚、pending 保留、重启后成功。
5. **Bootstrap 缺失**：更新开关禁用并显示正确文案。
6. **Bootstrap 应用失败**：manifest 损坏 / 哈希不符 / 目标目录不存在 → 回滚、日志清晰、不阻止启动。
7. **UMM 回归**：检查、下载、立即应用、pending 应用均与当前一致。
8. **异常场景**：下载中断、取消、磁盘不可写、只读 Mods、权限不足、多实例运行。
9. **扫描验证**：每次更新后确认 `Mods` 下没有多出 staging/backup DLL，日志中无重复 mod 加载。
10. **默认关闭**：更新开关关闭时不发起任何网络请求。

## 11. 风险与回退

| 风险 | 影响 | 应对 |
|---|---|---|
| 插件初始化晚于 mod 程序集加载 | Bootstrap 无法替换已锁文件 | 前置验证；失败改用外部辅助进程方案 |
| Melon 递归扫描 UserData 之外目录 | 待更新 DLL 被当作 mod | 工作目录放 UserData；测试确认 |
| 现有用户没有 Bootstrap | 更新无法应用 | 禁用更新入口 + 明确提示 + 发布说明 |
| Bootstrap 自身缺陷 | 无法自动修复 | 手动替换；逻辑保持极简、无依赖 |
| `MelonEnvironment` API 差异 | 路径不一致 | 前置验证；日志输出实际路径；必要时固定 UserData 路径 |
| 替换中途失败 | 文件不一致 | manifest 哈希 + backup 回滚 + 下次启动重试 |
| 多安装共用 UserData | pending 覆盖 | pending 携带 path，仅保留最新任务 |

## 12. 验收



验收标准：

- Melon 与 UMM 都能从旧版本升级到新版本，重启后版本号正确。
- 更新失败不会破坏现有安装，可再次触发更新。
- 更新关闭时不产生网络请求。
- UMM 现有更新流程无回归。
- 构建产出 `TimingShow.UMM.zip` 与 `TimingShow.Melon.zip`，Melon 包含 Bootstrap，README 安装说明完整。
