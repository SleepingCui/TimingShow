# 分支规范

一句话：`master` 是唯一长期分支，工作分支活不过一个版本，版本号只由 tag 表达，分支名里不出现版本号。

## 分支类型

| 分支 | 命名 | 从哪切 | 生命周期 | 怎么合并 |
| --- | --- | --- | --- | --- |
| `master` | `master` | — | 永久 | 小范围修复直接提交；大范围改动、重构、新功能通过 PR 进入，始终可构建、可发布 |
| 功能 | `feat/<topic>` | `master` | 一个版本内 | Squash merge，合并后删除 |
| 修复 | `fix/<topic>` | `master`（或 `release/*`） | 同上 | Squash merge，合并后删除 |
| 重构/性能/文档/杂务 | `refactor/`、`perf/`、`docs/`、`chore/<topic>` | `master` | 同上 | Squash merge，合并后删除 |
| 发布冻结 | `release/<x.y.z>` | `master` | 发布前到发布后 | 发布完成后 merge 回 `master` 并删除 |
| 旧版本补丁 | `fix/<x.y.z>-<topic>` | 对应 tag | 一到两个提交 | 打补丁版本 tag，再择机合回 `master` |

命名细则：

- 全小写 ASCII，kebab-case，用 `-` 分词：`feat/log-scatter-filter`、`fix/hud-scale-on-dpi-change`。
- 不要用纯数字或 `x.y.z` 当分支名。`1.9.0`、`1.9.1` 这种名字与 tag 重名，`git checkout 1.9.1` 会报 `refname '1.9.1' is ambiguous`。
- 不要中文、空格、下划线以外还可能出问题的字符：开发构建会用分支名拼版本号 `CI-<branch>-<hash>`，非法字符会被替换成 `-`。
- 一个分支只做一件事。改了两块互不相关的东西就拆两个分支。

## 日常开发

小范围修复（普通的 bug 修复、文案/样式调整）不开分支、不开 PR，直接在 `master` 上提交并推送：

```bash
git switch master
git pull --ff-only
# ... 修改并提交 ...
git push
```

只有大范围改动、某一部分的重构或新增功能才走分支和 PR：

```bash
git switch master
git pull --ff-only
git switch -c feat/log-scatter-filter

# ... 提交 ...

git push -u origin feat/log-scatter-filter
# GitHub 上开 PR → Squash and merge → Delete branch
```

本地保留已合并分支没有收益，合并即删。

## 发布

版本号只在发布时动，日常开发不要改 `Info.json` 和 `MelonInfo` 里的版本。

1. 在 `master` 上改版本与变更说明：
   - `TimingShow.Loader.UMM/Info.json` 的 `Version`
   - `TimingShow.Loader.Melon/Properties/AssemblyInfo.cs` 里 `MelonInfo(..., "1.9.4", ...)`
   - `CHANGELOG.md`（整份会被当成 release notes，只写这一版的变更）
2. 提交：`chore(release): 1.9.4`。
3. 跑 `Build Release` 工作流，`tag` 填 `1.9.4`，**分支选 `master`**。它会自己创建并推送 tag、用 `CHANGELOG.md` 建 draft release、再把 `Repository.json` 回写到它运行的那个分支。
4. 核对 draft release 的 zip 和说明后手动发布。
5. 把 CI 回写的 `Repository.json` 提交拉回本地 `master`（`git pull`）。

只有需要冻结回归（比如游戏更新后一边修旧版一边开发新版）才开 `release/<x.y.z>`：

```bash
git switch -c release/1.9.4 master
# 只让 fix/* 进来，功能改动一律回 master
# 发布后必须合回 master：
git switch master && git merge --no-ff release/1.9.4 && git branch -d release/1.9.4
```

发布分支不回合并，`Repository.json` 的回写提交就会留在孤儿分支上，下次发布从 `master` 出发时这版记录就丢了；`master` 与 `1.9.4` 现在分叉 14/9 就是这么来的，1.9.3 的功能在两条线上各实现了一遍。

## Tag

- tag 只由 `Build Release` 创建，不手打，保证 tag 对应的提交就是实际构建发布的那一份。
- 格式沿用 `1.9.4`，不加 `v` 前缀（与已有 tag 和 `Repository.json` 的 `DownloadUrl` 保持一致）。
- tag 推送后不改。发错了就发下一个补丁版本。
- 需要留档的死分支，打 `archive/<branch>` tag 再删分支，而不是让分支永久挂着。

## 提交信息

`type(scope): 摘要`，摘要用中文，一句话说清改了什么。

- type：`feat`、`fix`、`perf`、`refactor`、`docs`、`chore`、`build`、`test`
- scope 取目录：`hud`、`log`、`opt`、`patch`、`bridge`、`diag`、`ui`、`loader`、`build`

```
fix(log): 散点图筛选按钮缺字
feat(hud): 支持按玩家分列显示
chore(release): 1.9.4
```

不要 `1`、`docs2`、`readme`、`update` 这种。正文写"为什么"，UI 改动写受影响区域和加载器。

## PR

仅用于大范围改动、某一部分的重构和新增功能；普通修复不要开 PR。

- 标题沿用提交信息格式。
- 描述三段：行为变化 / 受影响的加载器（UMM、Melon）/ 实际执行过的验证（`build.bat`、`dotnet run --project tools/GeometryTests/GeometryTests.csproj`，没跑就说没跑）。
- 功能分支用 Squash merge；`release/*` 回合并用 merge commit，保留发布边界。
- 合并即删分支，本地和远端都删。

## 一次性清理

```bash
git fetch --prune
git switch master && git merge --ff-only origin/master

# 已完全落后于 master、只靠 tag 记录的分支
git tag archive/1.9.0 1.9.0 && git branch -D 1.9.0
git tag archive/1.9.1 1.9.1 && git branch -D 1.9.1
git tag archive/1.9.2-ref 1.9.2-ref && git branch -D 1.9.2-ref
git tag archive/ui-ref uiref && git branch -D uiref
git tag archive/dev-1.8.0 dev-1.8.0 && git branch -D dev-1.8.0

git push origin --delete 1.9.1-beplnex dev-1.8.0 uiref
```

`1.9.4` 不归档：先把 `master` 合进 `1.9.4` 解冲突，再把 `1.9.4` 合回 `master`，之后按上面的发布流程从 `master` 发 1.9.4，最后删除该分支。
