using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace TimingShow.Logging
{

    public sealed class TimingLogData
    {
        public string FilePath;
        public string SongName;
        public string LevelPath;
        public long Timestamp;
        public double Bpm;
        public double Speed;
        public double Pitch;
        public bool IsAngle;
        public int FormatVersion;
        public string HitMarginVersionText;
        public int JudgeCodeVersion;
        public bool Truncated;
        public int PlayerCount = 1;
        public List<TimingScatterSample> Samples = new List<TimingScatterSample>(1024);
    }

    public static class TimingLogReader
    {

        private const int MaxSamples = 2000000;

        private const int StreamBufferSize = 1 << 16;

        private static readonly byte[] BinaryMagic = Encoding.UTF8.GetBytes("TSMZ");

        public static bool IsSupportedLogFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            return EndsWith(filePath, ".json") || EndsWith(filePath, ".tlog") || EndsWith(filePath, ".tlog.gz");
        }

        public static TimingLogData Read(string filePath)
        {
            TimingLogData data;
            string error;
            if (TryRead(filePath, out data, out error)) return data;

            ModContext.Logger?.Error($"[TimingLogReader] Failed to read log: {filePath} ({error})");
            return null;
        }

        public static bool TryRead(string filePath, out TimingLogData data, out string error)
        {
            data = null;
            error = null;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                error = "file path is null or empty";
                return false;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    error = "file not found";
                    return false;
                }

                if (EndsWith(filePath, ".gz") || EndsWith(filePath, ".tlog"))
                    return TryReadBinary(filePath, out data, out error);

                if (EndsWith(filePath, ".json"))
                    return TryReadJson(filePath, out data, out error);

                error = "unsupported log file extension";
                return false;
            }
            catch (Exception ex)
            {
                data = null;
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static bool TryReadJson(string filePath, out TimingLogData data, out string error)
        {
            data = null;
            error = null;

            FileStream fileStream = null;
            StreamReader streamReader = null;
            JsonTextReader reader = null;
            TimingLogData result = new TimingLogData();
            result.FilePath = filePath;

            try
            {
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fileStream.Length == 0)
                {
                    error = "empty file";
                    return false;
                }

                streamReader = new StreamReader(fileStream, new UTF8Encoding(false), true, StreamBufferSize);
                reader = new JsonTextReader(streamReader);

                reader.DateParseHandling = DateParseHandling.None;
                reader.FloatParseHandling = FloatParseHandling.Double;
                reader.CloseInput = true;

                bool sawRoot = false;
                bool sawHeaderField = false;
                bool hasOffsets = false;
                bool completed = false;
                bool aborted = false;

                try
                {

                    while (true)
                    {
                        if (!reader.Read()) break;
                        JsonToken token = reader.TokenType;

                        if (token == JsonToken.StartObject)
                        {
                            if (!sawRoot)
                            {
                                sawRoot = true;
                                continue;
                            }
                            SkipContainer(reader);
                            continue;
                        }

                        if (token == JsonToken.EndObject)
                        {
                            completed = true;
                            break;
                        }

                        if (token != JsonToken.PropertyName) continue;

                        string name = reader.Value as string;
                        if (!reader.Read())
                        {
                            result.Truncated = true;
                            break;
                        }

                        if (IsOffsetsField(name))
                        {
                            int offsetsPlayerId = OffsetsFieldPlayerId(name);
                            if (reader.TokenType == JsonToken.StartArray)
                            {
                                hasOffsets = true;
                                if (offsetsPlayerId + 1 > result.PlayerCount) result.PlayerCount = offsetsPlayerId + 1;
                                if (!AppendOffsetsArray(reader, result, offsetsPlayerId))
                                {
                                    aborted = true;
                                    break;
                                }
                            }
                            else
                            {

                                if (reader.TokenType == JsonToken.Null) {  }
                                else SkipContainer(reader);
                                result.Truncated = true;
                            }
                            continue;
                        }

                        if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray)
                        {
                            SkipContainer(reader);
                            continue;
                        }

                        if (ApplyHeaderField(result, name, reader)) sawHeaderField = true;
                    }

                    if (hasOffsets && !completed && !aborted)
                    {
                        int depth = 1;
                        while (reader.Read())
                        {
                            JsonToken token = reader.TokenType;
                            if (token == JsonToken.StartObject || token == JsonToken.StartArray)
                            {
                                depth++;
                            }
                            else if (token == JsonToken.EndObject || token == JsonToken.EndArray)
                            {
                                depth--;
                                if (depth <= 0)
                                {
                                    completed = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {

                    result.Truncated = true;
                    aborted = true;
                }

                if (!sawRoot)
                {
                    error = "not a TimingShow JSON log (no root object)";
                    return false;
                }

                if (!hasOffsets && !sawHeaderField)
                {
                    error = "not a TimingShow JSON log (no recognized fields)";
                    return false;
                }

                if (!completed) result.Truncated = true;
                if (string.IsNullOrEmpty(result.HitMarginVersionText)) result.HitMarginVersionText = "Unknown";

                NormalizeSamples(result);

                data = result;
                return true;
            }
            catch (Exception ex)
            {
                data = null;
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
            finally
            {
                if (reader != null) { try { reader.Close(); } catch { } }
                if (streamReader != null) { try { streamReader.Dispose(); } catch { } }
                if (fileStream != null) { try { fileStream.Dispose(); } catch { } }
            }
        }

        private static bool ApplyHeaderField(TimingLogData data, string name, JsonTextReader reader)
        {
            switch (name)
            {
                case "songName":
                    data.SongName = TokenToString(reader.Value);
                    return true;
                case "levelPath":
                    data.LevelPath = TokenToString(reader.Value);
                    return true;
                case "timestamp":
                    data.Timestamp = TokenToInt64(reader.Value);
                    return true;
                case "bpm":
                    data.Bpm = TokenToDouble(reader.Value);
                    return true;
                case "speed":
                    data.Speed = TokenToDouble(reader.Value);
                    return true;
                case "pitch":
                    data.Pitch = TokenToDouble(reader.Value);
                    return true;
                case "isAngle":
                    data.IsAngle = TokenToBoolean(reader.Value);
                    return true;
                case "formatVersion":
                    data.FormatVersion = TokenToInt32(reader.Value);
                    return true;
                case "hitMarginVersion":
                    data.HitMarginVersionText = TokenToString(reader.Value);
                    return true;
                case "judgeCodeVersion":
                    data.JudgeCodeVersion = TokenToInt32(reader.Value);
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsOffsetsField(string name)
        {
            if (name == null) return false;
            if (string.Equals(name, "offsets", StringComparison.Ordinal)) return true;
            if (name.Length <= OffsetsPrefix.Length) return false;
            if (!name.StartsWith(OffsetsPrefix, StringComparison.Ordinal)) return false;

            for (int i = OffsetsPrefix.Length; i < name.Length; i++)
            {
                char c = name[i];
                if (c < '0' || c > '9') return false;
            }
            return true;
        }

        private static int OffsetsFieldPlayerId(string name)
        {
            if (string.Equals(name, "offsets", StringComparison.Ordinal)) return 0;
            if (name == null || name.Length <= OffsetsPrefix.Length) return 0;

            int value = 0;
            for (int i = OffsetsPrefix.Length; i < name.Length; i++)
            {
                char c = name[i];
                if (c < '0' || c > '9') return 0;
                value = value * 10 + (c - '0');
                if (value > 1000) return 0;
            }

            int playerId = value - 1;
            return playerId < 0 ? 0 : playerId;
        }

        private const string OffsetsPrefix = "offsets_";

        private static bool AppendOffsetsArray(JsonTextReader reader, TimingLogData result, int playerId)
        {
            List<TimingScatterSample> samples = result.Samples;

            while (true)
            {
                if (samples.Count >= MaxSamples)
                {
                    result.Truncated = true;
                    return false;
                }

                if (!reader.Read())
                {
                    result.Truncated = true;
                    return false;
                }

                JsonToken token = reader.TokenType;
                if (token == JsonToken.EndArray) return true;

                if (token != JsonToken.StartArray)
                {
                    SkipContainer(reader);
                    continue;
                }

                TimingScatterSample sample = new TimingScatterSample();
                sample.HasJudge = true;
                sample.TimeMs = float.NaN;
                sample.Judge = HitMan.Unknown;
                sample.FloorId = -1;

                double timeMs = double.NaN;
                double value = 0.0;
                double angleDeg = double.NaN;
                double hitBpm = double.NaN;
                double hitSpeed = double.NaN;
                double hitPitch = double.NaN;
                int floorId = -1;
                bool hasFloorField = false;
                int judgeCode = -1;
                bool isXp = false;
                bool innerDone = false;
                int index = 0;

                while (true)
                {
                    if (!reader.Read())
                    {
                        result.Truncated = true;
                        return false;
                    }

                    JsonToken inner = reader.TokenType;
                    if (inner == JsonToken.EndArray)
                    {
                        innerDone = true;
                        break;
                    }

                    if (inner == JsonToken.StartObject || inner == JsonToken.StartArray)
                    {
                        SkipContainer(reader);
                        index++;
                        continue;
                    }

                    switch (index)
                    {
                        case 0:
                            if (inner != JsonToken.Null) timeMs = TokenToDouble(reader.Value);
                            break;
                        case 1:
                            value = TokenToDouble(reader.Value);
                            break;
                        case 2:

                            break;
                        case 3:
                            judgeCode = TokenToInt32(reader.Value);
                            break;
                        case 4:
                            isXp = TokenToInt32(reader.Value) != 0;
                            break;
                        case 5:
                            if (inner != JsonToken.Null) angleDeg = TokenToDouble(reader.Value);
                            break;
                        case 6:
                            if (inner != JsonToken.Null) hitBpm = TokenToDouble(reader.Value);
                            break;
                        case 7:
                            if (inner != JsonToken.Null) hitSpeed = TokenToDouble(reader.Value);
                            break;
                        case 8:
                            if (inner != JsonToken.Null) hitPitch = TokenToDouble(reader.Value);
                            break;
                        case 9:
                            if (inner != JsonToken.Null)
                            {
                                floorId = TokenToInt32(reader.Value);
                                hasFloorField = true;
                            }
                            break;
                        default:
                            break;
                    }

                    index++;
                }

                if (!innerDone)
                {
                    result.Truncated = true;
                    return false;
                }

                sample.TimeMs = (float)timeMs;
                sample.Judge = (HitMan)judgeCode;
                sample.IsXPerfect = isXp;
                sample.PlayerId = playerId;
                sample.FloorId = hasFloorField && floorId >= 0 ? floorId : -1;
                sample.HasFloorId = sample.FloorId >= 0;

                bool hasHitParams = hitBpm > 0.0 && hitSpeed > 0.0 && hitPitch > 0.0;
                sample.HasHitParams = hasHitParams;
                if (hasHitParams)
                {
                    sample.Bpm = (float)hitBpm;
                    sample.Speed = (float)hitSpeed;
                    sample.Pitch = (float)hitPitch;
                }

                // Raw value; NormalizeSamples() resolves ms/angle after the header is known.
                sample.OffsetMs = (float)value;
                if (IsFinite(angleDeg))
                {
                    sample.AngleDeg = (float)angleDeg;
                    sample.HasAngle = true;
                }

                samples.Add(sample);
            }
        }

        private static bool TryReadBinary(string filePath, out TimingLogData data, out string error)
        {
            data = null;
            error = null;

            bool gzip = EndsWith(filePath, ".gz");

            FileStream fileStream = null;
            Stream input = null;
            CountingStream counting = null;
            BufferedStream buffered = null;
            BinaryReader reader = null;
            TimingLogData result = new TimingLogData();
            result.FilePath = filePath;

            try
            {
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fileStream.Length == 0)
                {
                    error = "empty file";
                    return false;
                }

                input = gzip ? (Stream)new GZipStream(fileStream, CompressionMode.Decompress) : fileStream;
                if (gzip)
                {

                    counting = new CountingStream(input);
                    buffered = new BufferedStream(counting, StreamBufferSize);
                }
                else
                {
                    buffered = new BufferedStream(input, StreamBufferSize);
                }
                reader = new BinaryReader(buffered, new UTF8Encoding(false));

                try
                {
                    byte[] magic = reader.ReadBytes(4);
                    if (magic.Length < 4 ||
                        magic[0] != BinaryMagic[0] || magic[1] != BinaryMagic[1] ||
                        magic[2] != BinaryMagic[2] || magic[3] != BinaryMagic[3])
                    {
                        error = "not a TimingShow binary log (magic mismatch)";
                        return false;
                    }

                    result.FormatVersion = reader.ReadByte();
                    result.Timestamp = reader.ReadInt64();
                    result.SongName = reader.ReadString();
                    result.LevelPath = reader.ReadString();
                    result.Bpm = reader.ReadDouble();
                    result.Speed = reader.ReadDouble();
                    result.Pitch = reader.ReadDouble();
                    result.IsAngle = reader.ReadByte() != 0;
                    result.HitMarginVersionText = HitMarginVersionToText(reader.ReadByte());
                    result.JudgeCodeVersion = reader.ReadByte();

                    if (result.FormatVersion >= 8)
                    {
                        int playerCount = reader.ReadByte();
                        if (playerCount > 0) result.PlayerCount = playerCount;
                    }
                }
                catch (Exception ex)
                {
                    error = "binary header is incomplete or corrupt: " + ex.GetType().Name;
                    return false;
                }

                List<TimingScatterSample> samples = result.Samples;
                long previousTimeBits = 0;
                long previousValueBits = 0;

                try
                {
                    while (true)
                    {
                        if (samples.Count >= MaxSamples)
                        {
                            result.Truncated = true;
                            break;
                        }

                        int playerId = 0;
                        if (result.FormatVersion >= 8)
                        {
                            try
                            {
                                playerId = reader.ReadByte();
                            }
                            catch (EndOfStreamException)
                            {
                                break;
                            }
                        }

                        int control;
                        try
                        {
                            control = reader.ReadByte();
                        }
                        catch (EndOfStreamException)
                        {
                            break;
                        }

                        double timeMs;
                        if (control == 0)
                        {
                            timeMs = BitConverter.Int64BitsToDouble(previousTimeBits);
                        }
                        else if (!TryDecodeXorDouble(reader, control, ref previousTimeBits, out timeMs))
                        {
                            result.Truncated = true;
                            break;
                        }

                        int valueControl = reader.ReadByte();
                        double value;
                        if (valueControl == 0)
                        {
                            value = BitConverter.Int64BitsToDouble(previousValueBits);
                        }
                        else if (!TryDecodeXorDouble(reader, valueControl, ref previousValueBits, out value))
                        {
                            result.Truncated = true;
                            break;
                        }

                        int rawMarginCode;
                        if (!TryReadVarInt(reader, out rawMarginCode))
                        {
                            result.Truncated = true;
                            break;
                        }

                        int judgeCode;
                        if (!TryReadVarInt(reader, out judgeCode))
                        {
                            result.Truncated = true;
                            break;
                        }

                        bool isXp;
                        try
                        {
                            isXp = reader.ReadByte() != 0;
                        }
                        catch (EndOfStreamException)
                        {
                            result.Truncated = true;
                            break;
                        }

                        float angleDeg = float.NaN;
                        float hitBpm = float.NaN;
                        float hitSpeed = float.NaN;
                        float hitPitch = float.NaN;
                        int floorId = -1;

                        if (result.FormatVersion >= 9)
                        {
                            try
                            {
                                angleDeg = reader.ReadSingle();
                                hitBpm = reader.ReadSingle();
                                hitSpeed = reader.ReadSingle();
                                hitPitch = reader.ReadSingle();

                                int decodedFloorId;
                                if (!TryReadVarInt(reader, out decodedFloorId))
                                {
                                    result.Truncated = true;
                                    break;
                                }
                                floorId = decodedFloorId;
                            }
                            catch (EndOfStreamException)
                            {
                                result.Truncated = true;
                                break;
                            }
                        }

                        TimingScatterSample sample = new TimingScatterSample();
                        sample.TimeMs = (float)timeMs;
                        sample.OffsetMs = (float)value;
                        sample.Judge = (HitMan)judgeCode;
                        sample.IsXPerfect = isXp;
                        sample.HasJudge = true;
                        sample.PlayerId = playerId;
                        sample.FloorId = floorId >= 0 ? floorId : -1;
                        sample.HasFloorId = sample.FloorId >= 0;

                        bool hasHitParams = hitBpm > 0f && hitSpeed > 0f && hitPitch > 0f;
                        sample.HasHitParams = hasHitParams;
                        if (hasHitParams)
                        {
                            sample.Bpm = hitBpm;
                            sample.Speed = hitSpeed;
                            sample.Pitch = hitPitch;
                        }

                        if (IsFinite(angleDeg))
                        {
                            sample.AngleDeg = angleDeg;
                            sample.HasAngle = true;
                        }

                        samples.Add(sample);
                    }
                }
                catch (Exception)
                {

                    result.Truncated = true;
                }

                if (gzip && !result.Truncated && counting != null &&
                    !IsGzipTrailerConsistent(fileStream, counting.BytesRead))
                {
                    result.Truncated = true;
                }

                NormalizeSamples(result);

                data = result;
                return true;
            }
            catch (Exception ex)
            {
                data = null;
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
            finally
            {
                if (reader != null) { try { reader.Dispose(); } catch { } }
                if (buffered != null) { try { buffered.Dispose(); } catch { } }
                if (input != null && !ReferenceEquals(input, fileStream)) { try { input.Dispose(); } catch { } }
                if (fileStream != null) { try { fileStream.Dispose(); } catch { } }
            }
        }

        private static bool TryDecodeXorDouble(BinaryReader reader, int control, ref long previousBits, out double value)
        {
            value = 0.0;

            if ((control & 0x80) == 0) return false;

            int leadingBytes = (control >> 4) & 0x07;
            int significantBytes = control & 0x0F;
            int trailingBytes = 8 - leadingBytes - significantBytes;

            if (significantBytes <= 0 || significantBytes > 8 || trailingBytes < 0) return false;

            ulong xor = 0UL;
            for (int i = 0; i < significantBytes; i++)
            {
                ulong b = reader.ReadByte();
                xor |= b << (8 * (trailingBytes + i));
            }

            long currentBits = previousBits ^ unchecked((long)xor);
            previousBits = currentBits;
            value = BitConverter.Int64BitsToDouble(currentBits);
            return true;
        }

        private static bool TryReadVarInt(BinaryReader reader, out int value)
        {
            value = 0;

            uint raw = 0;
            int bytesRead = 0;

            while (true)
            {
                if (bytesRead >= 5) return false;

                byte b = reader.ReadByte();
                raw |= (uint)(b & 0x7F) << (7 * bytesRead);
                bytesRead++;

                if ((b & 0x80) == 0) break;
            }

            value = VarInt.UnZigZag(raw);
            return true;
        }

        private static bool IsGzipTrailerConsistent(FileStream fileStream, long decompressedBytes)
        {
            try
            {
                if (fileStream.Length < 18) return false;

                long position = fileStream.Position;
                fileStream.Seek(-8, SeekOrigin.End);

                byte[] trailer = new byte[8];
                int read = 0;
                while (read < 8)
                {
                    int n = fileStream.Read(trailer, read, 8 - read);
                    if (n <= 0) break;
                    read += n;
                }

                fileStream.Position = position;

                if (read != 8) return false;

                uint isize = (uint)(trailer[4] | (trailer[5] << 8) | (trailer[6] << 16) | (trailer[7] << 24));
                return isize == (uint)(decompressedBytes & 0xFFFFFFFFL);
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double MsToAngle(double ms, double bpm, double speed, double pitch)
        {
            return ms * 180.0 * bpm * speed * pitch / 60000.0;
        }

        // The "value" column means different things depending on the header isAngle flag.
        // OffsetMs keeps that legacy meaning; AngleDeg is filled whenever it is known.
        private static void NormalizeSamples(TimingLogData data)
        {
            if (data == null || data.Samples == null) return;

            List<TimingScatterSample> samples = data.Samples;
            for (int i = 0; i < samples.Count; i++)
            {
                TimingScatterSample sample = samples[i];
                float raw = sample.OffsetMs;

                if (data.IsAngle)
                {
                    // Legacy semantics: the value column already is the angle.
                    float angle = sample.HasAngle && IsFinite(sample.AngleDeg) ? sample.AngleDeg : raw;
                    sample.AngleDeg = angle;
                    sample.HasAngle = IsFinite(angle);
                    sample.OffsetMs = raw;
                }
                else
                {
                    // The value column is the offset in ms. Derive the angle when the
                    // per-hit parameters are available (new v8/v9 logs).
                    if (!sample.HasAngle && sample.HasHitParams && IsFinite(raw))
                    {
                        sample.AngleDeg = (float)MsToAngle(raw, sample.Bpm, sample.Speed, sample.Pitch);
                        sample.HasAngle = true;
                    }

                    sample.OffsetMs = raw;
                }

                samples[i] = sample;
            }
        }

        private sealed class CountingStream : Stream
        {
            private readonly Stream _inner;

            public CountingStream(Stream inner)
            {
                _inner = inner;
            }

            public long BytesRead { get; private set; }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int n = _inner.Read(buffer, offset, count);
                if (n > 0) BytesRead += n;
                return n;
            }

            public override int ReadByte()
            {
                int b = _inner.ReadByte();
                if (b >= 0) BytesRead++;
                return b;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        private static string HitMarginVersionToText(byte value)
        {
            switch (value)
            {
                case 1: return "Legacy";
                case 2: return "Game34";
                default: return "Unknown";
            }
        }

        private static void SkipContainer(JsonTextReader reader)
        {
            if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray)
                reader.Skip();
        }

        private static bool EndsWith(string path, string suffix)
        {
            return path != null && path.Length >= suffix.Length &&
                   path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        private static string TokenToString(object value)
        {
            if (value == null) return null;
            string s = value as string;
            return s ?? Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static double TokenToDouble(object value)
        {
            if (value == null) return 0.0;

            double d;
            if (value is double) return (double)value;

            IConvertible convertible = value as IConvertible;
            if (convertible != null)
            {
                try
                {
                    return convertible.ToDouble(CultureInfo.InvariantCulture);
                }
                catch
                {

                }
            }

            return double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : 0.0;
        }

        private static int TokenToInt32(object value)
        {
            if (value == null) return 0;

            IConvertible convertible = value as IConvertible;
            if (convertible != null)
            {
                try
                {
                    return convertible.ToInt32(CultureInfo.InvariantCulture);
                }
                catch
                {

                }
            }

            int i;
            return int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out i) ? i : 0;
        }

        private static long TokenToInt64(object value)
        {
            if (value == null) return 0L;

            IConvertible convertible = value as IConvertible;
            if (convertible != null)
            {
                try
                {
                    return convertible.ToInt64(CultureInfo.InvariantCulture);
                }
                catch
                {

                }
            }

            long l;
            return long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out l) ? l : 0L;
        }

        private static bool TokenToBoolean(object value)
        {
            if (value == null) return false;

            if (value is bool) return (bool)value;

            string s = value as string;
            if (s != null)
            {
                bool b;
                if (bool.TryParse(s, out b)) return b;
                int i;
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out i)) return i != 0;
                return false;
            }

            IConvertible convertible = value as IConvertible;
            if (convertible != null)
            {
                try
                {
                    return convertible.ToBoolean(CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }

            return false;
        }
    }
}
