# TimingShow 日志格式说明（新版）

本文档描述 TimingShow 记录游戏击打数据的日志格式，供 **adofai_offset_analyzer**（及任何第三方工具）适配。

新版的核心变化是 **多玩家支持**：

| 写入器 | 版本常量 | 文件 | 多玩家表示 |
| --- | --- | --- | --- |
| JSON | `FormatVersion = 8` | `*.json` | 每个玩家一个 `offsets_<N>` 数组 |
| Binary | `BinaryFormatVersion = 9` | `*.tlog.gz`（未压缩 `*.tlog`） | 头部记录玩家总数，每条记录前置 `playerId` 字节 |

v8/v9 在每条记录尾部追加了命中上下文（角度、命中时 BPM/speed/pitch、地板 ID），供编辑器击打点标记等新功能使用。读取器仍必须兼容旧的单玩家 JSON（`formatVersion <= 6`，单个 `offsets` 键）、v7 多玩家 JSON 以及旧二进制（`formatVersion <= 8`），详见 [兼容与版本差异](#兼容与版本差异)。

> 相关源码：
> `TimingShow/Logging/TimingLogger.cs`（JSON 写入）、
> `TimingShow/Logging/TimingLoggerBinary.cs`（二进制写入）、
> `TimingShow/Logging/TimingLogReader.cs`（读取，可作为参考实现）、
> `TimingShow/Logging/VarInt.cs`（ZigZag 变长整数）、
> `TimingShow/HitMan.cs`（`HitMan` / `HitMarginVersion` 枚举）。

---

## 1. 如何识别文件类型

按以下顺序探测即可同时兼容新旧格式：

1. **看扩展名**
   - `.json` → JSON。
   - `.tlog.gz` → gzip 压缩的二进制。
   - `.tlog` → 未压缩的二进制。
2. **兜底看魔数**（扩展名不可信时）
   - 前 2 字节为 `1F 8B` → gzip；解压后继续判断。
   - 前 4 字节为 `TSMZ`（`54 53 4D 5A`）→ 二进制。
   - 首个非空白字节为 `{` → JSON。
   - JSON 的旧文件可能没有 `formatVersion`（视为 `<= 6`）。

二进制流的所有多字节数值都是 **小端序（little-endian）**（.NET `BinaryWriter` 默认行为）。

---

## 2. 公共头字段

两种格式承载相同的逻辑信息：

| 字段 | 类型 | 含义 |
| --- | --- | --- |
| `songName` | string | 歌曲标题。**注意：写入的是“净化”后的文件名去扩展名**（非法文件名字符与空格被替换为 `_`），可能不是原始曲名，缺失时为 `"Unknown"`。 |
| `levelPath` | string | 关卡文件路径（可能为空字符串）。 |
| `timestamp` | int64 | 创建时间，Unix 秒（UTC）。文件名为 `<timestamp>_<songName>.<ext>`。 |
| `bpm` | double | BPM。 |
| `speed` | double | 速度倍率。 |
| `pitch` | double | 音高倍率。 |
| `isAngle` | bool | `true` 时每条记录的“数值”是**角度（度）**，`false` 时是**击打偏差 timing（ms）**。整局固定。 |
| `formatVersion` | int | JSON 固定 `8`；二进制固定 `9`。 |
| `hitMarginVersion` | string / byte | 判定表版本，见 [表 A](#表-a-hitmarginversion)。 |
| `judgeCodeVersion` | int / byte | `judgeCode` 枚举版本，当前为 `1`。`judgeCode` 的含义随此值变化。 |
| `playerCount` | int | 玩家总数（仅二进制 v8+ 头部显式存储；JSON 由 `offsets_<N>` 的最大 N 推导）。 |

### 击打记录（sample）的公共字段

每条记录包含：

| 字段 | 类型 | 含义 |
| --- | --- | --- |
| `playerId` | 0-based 序号 | 玩家编号。JSON 由所在的 `offsets_<N>` 键隐含（记录里**不含**该字段）；二进制 v8 每条记录显式带一个字节。 |
| `timeMs` | double | **本局会话时间（session elapsed）**：暂停感知的 Stopwatch 毫秒数，不是严格的歌曲/音频时间。未知时为 `NaN`（JSON 中写作 `null`）。 |
| `value` | double | `isAngle ? 角度(度) : 偏差 timing(ms)`。 |
| `rawMarginCode` | int | 原始 margin 编码（判定带内的原始值）。游戏内读取器会忽略它，但**保留在文件里**，analyzer 可使用。 |
| `judgeCode` | int | 判定种类，见 [表 B](#表-b-hitmanjudgecode)。可为 `-1`（`Unknown`）。 |
| `isXPerfect` | bool | 是否 XPerfect。 |
| `angleDeg` | double / null | 命中时的角度偏差（度）。旧记录缺失时为 `null`。 |
| `hitBpm` | double / null | 命中时地板 BPM（未乘 speed）。缺失时为 `null`。 |
| `hitSpeed` | double / null | 命中时玩家速度倍率。缺失时为 `null`。 |
| `hitPitch` | double / null | 命中时音高倍率。缺失时为 `null`。 |
| `floorId` | int | 命中地板的 `scrFloor.seqID`；未知为 `-1`。 |

### ms / angle 归一化

`value` 的语义仍由头部 `isAngle` 决定：为 `true` 时是角度，`false` 时是毫秒。读取器同时维护两个语义字段：

- `value`（代码中为 `OffsetMs`）：`isAngle = false` 时是毫秒偏差；`isAngle = true` 时**仍是角度**（保持旧日志/旧图表语义）。
- `angleDeg`：恒为角度（度）。`isAngle = true` 时取自 `value` 或显式字段；`isAngle = false` 且带单条 hit 参数时由 `value` 反算。

如需得到绝对值时间偏差（毫秒），换算公式为（`floorBpm = hitBpm * hitSpeed`）：

```text
offsetMs = angleDeg * 60000 / (180 * hitBpm * hitSpeed * hitPitch)
angleDeg = offsetMs * 180 * hitBpm * hitSpeed * hitPitch / 60000
```

仅当该条记录的 `hitBpm/hitSpeed/hitPitch` 都有效（> 0）时才可精确换算。旧记录缺少这些字段时，`angleDeg` 对角度日志仍可用，但无法还原毫秒；消费方不得把角度值当作毫秒使用。

---

## 3. JSON 格式（`formatVersion = 8`）

- 编码：UTF-8，**无 BOM**。
- 顶层是一个对象；先写头字段，再写各玩家数组。

```json
{
  "songName": "MySong",
  "levelPath": "C:\\levels\\MySong.adofai",
  "timestamp": 1700000000,
  "bpm": 180.0,
  "speed": 1.0,
  "pitch": 1.0,
  "isAngle": false,
  "formatVersion": 8,
  "hitMarginVersion": "Legacy",
  "judgeCodeVersion": 1,
  "offsets_1": [
    [100.000, 1.2500, 3, 3, 0, 0.3125, 180.0, 1.0, 1.0, 42],
    [110.000, 2.5000, 3, 6, 0, 0.6250, 180.0, 1.0, 1.0, 43],
    [null, -0.0200, 0, 0, 1, -0.0050, 180.0, 1.0, 1.0, 44]
  ],
  "offsets_2": [
    [100.001, 0.8000, 3, 3, 0, null, null, null, null, -1]
  ]
}
```

> v7 文件的条目只有前 5 项；v8 追加 5 项。解析器必须按“缺失即未知”处理，不能把 0 当作有效的 angle/floor。

### `offsets_<N>` 条目布局

每个玩家一个数组，键名 `offsets_<N>`（`N` 从 **1** 开始，对应 `playerId = N - 1`）。v8 条目为 10 元数组，v7 及更旧文件只有前 5 项：

```
[ timeMs, value, rawMarginCode, judgeCode, isXPerfect,
  angleDeg, hitBpm, hitSpeed, hitPitch, floorId ]
```

| 序号 | 字段 | JSON 表示 |
| --- | --- | --- |
| 0 | `timeMs` | 十进制字符串，保留 3 位小数（`F3`）；非法/负值写作 `null`。 |
| 1 | `value` | 十进制字符串，保留位数由用户精度设置 `PercLog` 决定（`F<PercLog>`，默认 3~4 位）。**是字符串，不是数字**。 |
| 2 | `rawMarginCode` | 整数。 |
| 3 | `judgeCode` | 整数（可为负）。 |
| 4 | `isXPerfect` | `0` / `1`。 |
| 5 | `angleDeg` | 十进制字符串（`F4`，invariant）；未知写作 `null`。**仅 v8+**。 |
| 6 | `hitBpm` | 不变数字符串（`R`）；未知写作 `null`。**仅 v8+**。 |
| 7 | `hitSpeed` | 同上。**仅 v8+**。 |
| 8 | `hitPitch` | 同上。**仅 v8+**。 |
| 9 | `floorId` | 整数；未知为 `-1`。**仅 v8+**。 |

> JSON 写入的是“半结构化”文本：`timeMs` 与 `value` 是格式化后的字符串。解析器需按字符串再转数字。

### 兼容旧 JSON

- 旧版（`formatVersion <= 6`）使用单个 `offsets` 键，等同于 `playerId = 0` 的单玩家数据。
- 解析策略：识别 `offsets`（无后缀）→ 玩家 0；识别 `offsets_<N>` → 玩家 `N-1`。
- 文件可能因崩溃而**截断**（最后一个条目未闭合）。读取器会尽量返回已完成的样本并置 `Truncated = true`，analyzer 建议做同样处理。

---

## 4. 二进制格式（`formatVersion = 9`）

- 外层通常是 **gzip**（`*.tlog.gz`）；`*.tlog` 则是未经压缩的同一字节流。
- 压缩流内部、以及未压缩文件本身，都以 4 字节魔数 `TSMZ` 开头。

### 4.1 基础编码约定

以下类型贯穿全文件（均小端）：

| 概念 | 编码 |
| --- | --- |
| `byte` | 1 字节无符号。 |
| `bool` | 1 字节，`0` = false，非 0 = true。 |
| `int64` | 8 字节有符号小端。 |
| `double` | IEEE-754 binary64，8 字节小端。 |
| `string` | **7-bit 长度前缀**（无符号 LEB128，见下）+ 该长度的 UTF-8 字节。注意前缀是普通无符号变长整数，**不是** ZigZag。 |
| `varint` | ZigZag + LEB128 有符号变长整数，见 4.3。 |
| XOR-double | 与上一条同类字段做差量编码，见 4.2。 |

### 4.2 XOR-double（`timeMs` 与 `value` 各维护一条历史链）

`timeMs` 和 `value` 各自维护一个“上一条记录的 double 位模式” `previousBits`（初始为 0）。写入时：

```text
currentBits = bits(double)          // double 转 int64 位模式
xor         = currentBits XOR previousBits   // 按无符号 64 位

if xor == 0:
    write byte 0x00                 // 与上一条完全相同
else:
    leading    = 高位连续 0 的字节数 (0..7)
    trailing   = 低位连续 0 的字节数 (0..7)
    significant = 8 - leading - trailing     // 1..8
    control    = 0x80 | (leading << 4) | significant
    write byte control
    // 从最低有效字节开始，写 significant 个字节（小端窗口）
    for i in 0 .. significant-1:
        write byte (xor >> (8 * (trailing + i))) & 0xFF
previousBits = currentBits          // 若 xor == 0，previousBits 不变（本就相等）
```

解析时，当前值 = `previousBits XOR 重组出的 xor`，然后再 `Int64BitsToDouble`，并更新 `previousBits`。

> 注意：`timeMs` 用 `timeBits` 链，`value` 用 `valueBits` 链，**两条链互相独立**，不要混用。

**解析伪代码（JavaScript）**

```js
function readXorDouble(view, pos, state) {
  const control = view.getUint8(pos++);
  if (control === 0) {
    // 与上一条相同，previousBits 不变
    return { value: bitsToDouble(state.prev), pos };
  }
  if ((control & 0x80) === 0) throw new Error("bad xor control");

  const leading = (control >> 4) & 0x07;
  const significant = control & 0x0f;
  const trailing = 8 - leading - significant;
  if (significant <= 0 || significant > 8 || trailing < 0) throw new Error("bad xor geometry");

  let xor = 0n;
  for (let i = 0; i < significant; i++) {
    xor |= BigInt(view.getUint8(pos++)) << BigInt(8 * (trailing + i));
  }
  const current = state.prev ^ xor;   // 用 BigInt 做 64 位异或
  state.prev = current;
  return { value: bitsToDouble(current), pos };
}
```

### 4.3 VarInt（ZigZag + LEB128）

`rawMarginCode` 与 `judgeCode` 使用该编码。

**写入（ZigZag 映射后做 LEB128）**

```text
v = ZigZag(value) = (value << 1) XOR (value >> 31)   // 32 位算术右移
while v >= 0x80:
    write byte (v | 0x80)
    v >>= 7
write byte v
```

**读取**

```text
raw = 0; shift = 0
do:
    b = read byte
    raw |= (b & 0x7F) << shift
    shift += 7
while (b & 0x80) != 0
value = UnZigZag(raw) = (raw >> 1) XOR -(raw & 1)
```

> 与 gzip 头部的长度前缀不同：**VarInt 是 ZigZag（有符号）**，字符串长度前缀是**无符号 LEB128**。

### 4.4 文件布局

**头部（按顺序）**

| # | 字段 | 类型 | 备注 |
| --- | --- | --- | --- |
| 1 | magic | 4 byte | ASCII `TSMZ` |
| 2 | `formatVersion` | byte | `9` |
| 3 | `timestamp` | int64 | Unix 秒 |
| 4 | `songName` | string | 7-bit 长度前缀 + UTF-8 |
| 5 | `levelPath` | string | 同上 |
| 6 | `bpm` | double | |
| 7 | `speed` | double | |
| 8 | `pitch` | double | |
| 9 | `isAngle` | byte(bool) | |
| 10 | `hitMarginVersion` | byte | 见 [表 A](#表-a-hitmarginversion) |
| 11 | `judgeCodeVersion` | byte | 当前 `1` |
| 12 | `playerCount` | byte | **仅 v8+**；`> 0` 时有效，否则视为 1 |

**样本列表（重复到下一条 EOF）**

| # | 字段 | 类型 | 备注 |
| --- | --- | --- | --- |
| 1 | `playerId` | byte | **仅 v8+** |
| 2 | `timeMs` | XOR-double | 独立链 |
| 3 | `value` | XOR-double | 独立链 |
| 4 | `rawMarginCode` | varint | ZigZag |
| 5 | `judgeCode` | varint | ZigZag |
| 6 | `isXPerfect` | byte(bool) | |
| 7 | `angleDeg` | float32 | **仅 v9+**；未知写 `NaN` |
| 8 | `hitBpm` | float32 | **仅 v9+**；未知写 `NaN` |
| 9 | `hitSpeed` | float32 | **仅 v9+**；未知写 `NaN` |
| 10 | `hitPitch` | float32 | **仅 v9+**；未知写 `NaN` |
| 11 | `floorId` | varint | **仅 v9+**；未知写 `-1` |

**版本差异**：

- **v7**：头部没有 `playerCount`，每条记录没有 `playerId`；判定玩家 0。
- **v8**：加入 `playerCount` 与 `playerId`。
- **v9**：每条记录尾部加入 4 个 float32（angle/bpm/speed/pitch）与一个 `floorId` varint。新增字段在写入端做有效性判断：命中参数无效时写 `NaN`。

> 结束位置：读到流末尾（EOF）即结束。若中途字节不足（`EndOfStream`），视为截断，返回已解析出的样本。

### 4.5 完整解析顺序（二进制）

```text
magic = readBytes(4)              // 必须 == "TSMZ"
version = readByte()
timestamp = readInt64()
songName  = readString()          // 7-bit 长度前缀 + UTF-8
levelPath = readString()
bpm = readDouble(); speed = readDouble(); pitch = readDouble()
isAngle = readByte() != 0
hitMarginVersion = readByte()
judgeCodeVersion = readByte()
playerCount = (version >= 8) ? max(1, readByte()) : 1

prevTime = 0; prevValue = 0
samples = []
while not EOF:
    playerId = (version >= 8) ? readByte() : 0
    timeMs  = readXorDouble(timeBits)
    value   = readXorDouble(valueBits)
    rawMarginCode = readVarInt()
    judgeCode     = readVarInt()
    isXPerfect    = readByte() != 0
    if version >= 9:
        angleDeg   = readFloat32()
        hitBpm     = readFloat32()
        hitSpeed   = readFloat32()
        hitPitch   = readFloat32()
        floorId    = readVarInt()
    else:
        angleDeg = hitBpm = hitSpeed = hitPitch = NaN
        floorId = -1
    samples.push({ playerId, timeMs, value, rawMarginCode, judgeCode, isXPerfect,
                   angleDeg, hitBpm, hitSpeed, hitPitch, floorId })
```

### 4.6 截断检测

- 若输入为 gzip：文件**最后 4 字节**是小端的 gzip `ISIZE`（解压后字节数 mod 2^32）。可与实际解压出的字节数比对，不一致说明写入中途被截断。
- 读取过程中任何 `EndOfStream` 都表示截断；返回已解析样本即可，不要整体报错。

---

## 5. 枚举表

### 表 A：`HitMarginVersion`

| 值 | JSON 文本 | 二进制 byte | 说明 |
| --- | --- | --- | --- |
| 0 | `"Unknown"` | `0` | 无法识别 |
| 1 | `"Legacy"` | `1` | 旧版判定表（无 XPerfect / PerfectMinus 等） |
| 2 | `"Game34"` | `2` | 3.4 判定表 |

判定表影响哪些 `judgeCode` 会出现，以及 `rawMarginCode` 的取值范围。

### 表 B：`HitMan` / `judgeCode`

`judgeCode` 即 `(int)HitMan`，含义随 `judgeCodeVersion`（当前 `1`）变化：

| 名称 | 值 | 说明 |
| --- | --- | --- |
| `Unknown` | `-1` | 未知/无效 |
| `TooEarly` | 0 | |
| `VeryEarly` | 1 | |
| `EarlyPerfect` | 2 | |
| `PerfectMinus` / `LegacyPerfect` | 3 | Legacy 下是 `Perfect` |
| `XPerfect` | 4 | |
| `PerfectPlus` | 5 | |
| `LatePerfect` | 6 | |
| `VeryLate` | 7 | |
| `TooLate` | 8 | |
| `Multipress` | 9 | |
| `FailMiss` | 10 | 失败类 |
| `FailOverload` | 11 | 失败类 |
| `Auto` | 12 | |
| `OverPress` | 13 | 失败类 |
| `Midspin` | 14 | |
| `FailedFloor` | 15 | 失败类 |

参考分组（可用于统计）：
- **Perfect 家族**：`EarlyPerfect, PerfectMinus, XPerfect, PerfectPlus, LatePerfect`
- **普通 Perfect**：Legacy 下的 `Perfect(LegacyPerfect)`；Game34 下的 `PerfectPlus`
- **失败家族**：`FailMiss, FailOverload, OverPress, FailedFloor`

---

## 6. 兼容与版本差异

| 场景 | `formatVersion` | 多玩家键 / 字节 | 解析要点 |
| --- | --- | --- | --- |
| 新版 JSON | `8` | `offsets_1..N`，条目 10 项 | `N` 从 1 开始；`playerId = N-1` |
| 旧版 JSON v7 | `7` | `offsets_1..N`，条目 5 项 | 追加字段缺失 → angle/floor 未知 |
| 旧版 JSON | `<= 6`（可能缺字段） | `offsets` | 单玩家，视作 `playerId = 0` |
| 新版二进制 | `9` | 头部 `playerCount` + 每条 `playerId` + 命中上下文 | |
| 旧版二进制 | `8` | 头部 `playerCount` + 每条 `playerId` | 记录尾部无 v9 字段 |
| 更旧二进制 | `<= 7` | 无 | 头部少一个字节，记录少一个字节 |

判定分支只依赖二进制头部的 `formatVersion`（第 5 字节，魔数之后），或 JSON 的 `formatVersion`。

### 其他兼容性注意

- `songName` 是净化后的文件名，可能含 `_`，不代表真实曲名。
- JSON 的 `value` / `timeMs` 是**格式化字符串**（精度受用户设置影响），不要期望固定小数位；`timeMs` 可能为 `null`。
- JSON 的 `rawMarginCode`（数组第 3 项）存在但游戏内读取器不消费；analyzer 可用。
- 空会话（没有任何击打）**不会产生文件**——若文件存在，至少有一条记录（或为截断）。
- 二进制可能以 gzip 分块流式写入，正在写入的文件也可能被读取（游戏/桥接层以共享读方式打开）。

---

## 7. 获取文件（桥接层）

游戏内可通过本地 HTTP 桥接把日志交给 Web analyzer（`TimingShow/Bridge/LogAnalyzerBridge.cs`）：

- 桥接在本机监听一个随机端口，返回单个文件。
- 二进制 `.tlog.gz` 以 `Content-Type: application/gzip` 返回，`.json` 以 `application/json` 返回。
- 浏览器端需根据 `Content-Type` / 扩展名 / 魔数选择 JSON 解析或 gzip 解压后再按第 4 节解析。
- 允许的来源为 `https://sleepingcui.github.io`（CORS 已配置）。

---

## 8. 最小可运行示例（JavaScript，二进制）

```js
async function parseTimingShowLog(bytes) {
  // 1. 需要时 gunzip（浏览器原生 DecompressionStream）
  if (bytes[0] === 0x1f && bytes[1] === 0x8b) {
    bytes = new Uint8Array(await new Response(
      new Blob([bytes]).stream().pipeThrough(new DecompressionStream("gzip"))
    ).arrayBuffer());
  }

  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  let pos = 0;
  const magic = String.fromCharCode(...bytes.slice(0, 4));
  if (magic !== "TSMZ") throw new Error("not a TimingShow binary log");
  pos = 4;

  const version = view.getUint8(pos++);
  const timestamp = Number(view.getBigInt64(pos, true)); pos += 8;

  const readString = () => {              // 无符号 LEB128 长度前缀 + UTF-8
    let len = 0, shift = 0, b;
    do { b = view.getUint8(pos++); len |= (b & 0x7f) << shift; shift += 7; } while (b & 0x80);
    const s = new TextDecoder().decode(bytes.subarray(pos, pos + len));
    pos += len;
    return s;
  };
  const readDouble = () => { const d = view.getFloat64(pos, true); pos += 8; return d; };
  const readFloat = () => { const f = view.getFloat32(pos, true); pos += 4; return f; };
  const readVarInt = () => {
    let raw = 0, shift = 0, b;
    do { b = view.getUint8(pos++); raw |= (b & 0x7f) << shift; shift += 7; } while (b & 0x80);
    return (raw >>> 1) ^ -(raw & 1);      // UnZigZag
  };

  const songName = readString();
  const levelPath = readString();
  const bpm = readDouble(), speed = readDouble(), pitch = readDouble();
  const isAngle = view.getUint8(pos++) !== 0;
  const hitMarginVersion = view.getUint8(pos++);
  const judgeCodeVersion = view.getUint8(pos++);
  const playerCount = version >= 8 ? Math.max(1, view.getUint8(pos++)) : 1;

  const timeState = { prev: 0n }, valueState = { prev: 0n };
  const readXor = (st) => {
    const control = view.getUint8(pos++);
    if (control === 0) return bitsToDouble(st.prev);
    const leading = (control >> 4) & 7, sig = control & 0x0f, trailing = 8 - leading - sig;
    let xor = 0n;
    for (let i = 0; i < sig; i++) xor |= BigInt(view.getUint8(pos++)) << BigInt(8 * (trailing + i));
    st.prev ^= xor;
    return bitsToDouble(st.prev);
  };

  const samples = [];
  while (pos < bytes.length) {
    const playerId = version >= 8 ? view.getUint8(pos++) : 0;
    const timeMs = readXor(timeState);
    const value = readXor(valueState);
    const rawMarginCode = readVarInt();
    const judgeCode = readVarInt();
    const isXPerfect = view.getUint8(pos++) !== 0;
    const angleDeg = version >= 9 ? readFloat() : NaN;
    const hitBpm = version >= 9 ? readFloat() : NaN;
    const hitSpeed = version >= 9 ? readFloat() : NaN;
    const hitPitch = version >= 9 ? readFloat() : NaN;
    const floorId = version >= 9 ? readVarInt() : -1;
    samples.push({ playerId, timeMs, value, rawMarginCode, judgeCode, isXPerfect,
                   angleDeg, hitBpm, hitSpeed, hitPitch, floorId });
  }

  return { songName, levelPath, timestamp, bpm, speed, pitch, isAngle,
           formatVersion: version, hitMarginVersion, judgeCodeVersion, playerCount, samples };
}

function bitsToDouble(bits) {
  const buf = new ArrayBuffer(8);
  const dv = new DataView(buf);
  dv.setBigInt64(0, BigInt.asIntN(64, bits), true);
  return dv.getFloat64(0, true);
}
```

> 上方 `readXor` / `bitsToDouble` 用 `BigInt` 做 64 位整数运算，避免 JS Number 精度问题。读取单个字节后 `pos++` 的顺序必须与第 4.4 节严格一致。

---

## 附录：字段速查

```
JSON v8 顶层键：songName levelPath timestamp bpm speed pitch isAngle
                formatVersion hitMarginVersion judgeCodeVersion
                offsets_1 .. offsets_N
JSON v8 条目：   [ timeMs, value, rawMarginCode, judgeCode, isXPerfect,
                  angleDeg, hitBpm, hitSpeed, hitPitch, floorId ]
JSON v7 条目：   [ timeMs, value, rawMarginCode, judgeCode, isXPerfect ]

Binary v9 头部：TSMZ | version(1) | timestamp(8) | songName(str) | levelPath(str)
                bpm(8) speed(8) pitch(8) isAngle(1) hitMarginVersion(1)
                judgeCodeVersion(1) playerCount(1)
Binary v9 记录： playerId(1) timeMs(xor) value(xor) rawMarginCode(varint)
                judgeCode(varint) isXPerfect(1) angleDeg(f32) hitBpm(f32)
                hitSpeed(f32) hitPitch(f32) floorId(varint)
```
