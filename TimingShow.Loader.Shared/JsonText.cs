using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TimingShow.Update
{
    internal static class JsonText
    {
        public static Dictionary<string, object> ParseObject(string text)
        {
            Dictionary<string, object> result = ParseValue(text) as Dictionary<string, object>;
            if (result == null) throw new FormatException("The JSON text is not an object.");
            return result;
        }

        public static object ParseValue(string text)
        {
            Reader reader = new Reader(text);
            object value = reader.ReadValue();
            reader.EnsureEnd();
            return value;
        }

        private sealed class Reader
        {
            private readonly string _text;
            private int _index;

            public Reader(string text)
            {
                _text = text ?? string.Empty;
            }

            public void EnsureEnd()
            {
                SkipWhiteSpace();
                if (_index < _text.Length) throw Error("unexpected trailing content");
            }

            public object ReadValue()
            {
                SkipWhiteSpace();
                if (_index >= _text.Length) throw Error("unexpected end of the JSON text");

                char current = _text[_index];
                switch (current)
                {
                    case '{':
                        return ReadObject();
                    case '[':
                        return ReadArray();
                    case '"':
                        return ReadString();
                    case 't':
                        ReadLiteral("true");
                        return true;
                    case 'f':
                        ReadLiteral("false");
                        return false;
                    case 'n':
                        ReadLiteral("null");
                        return null;
                    default:
                        return ReadNumber();
                }
            }

            private Dictionary<string, object> ReadObject()
            {
                Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                _index++;
                SkipWhiteSpace();
                if (_index < _text.Length && _text[_index] == '}') { _index++; return values; }

                while (true)
                {
                    SkipWhiteSpace();
                    string key = ReadString();
                    SkipWhiteSpace();
                    if (_index >= _text.Length || _text[_index] != ':') throw Error("expected ':'");
                    _index++;
                    values[key] = ReadValue();
                    SkipWhiteSpace();
                    if (_index >= _text.Length) throw Error("unterminated object");
                    char next = _text[_index++];
                    if (next == '}') return values;
                    if (next != ',') throw Error("expected ',' or '}'");
                }
            }

            private List<object> ReadArray()
            {
                List<object> values = new List<object>();
                _index++;
                SkipWhiteSpace();
                if (_index < _text.Length && _text[_index] == ']') { _index++; return values; }

                while (true)
                {
                    values.Add(ReadValue());
                    SkipWhiteSpace();
                    if (_index >= _text.Length) throw Error("unterminated array");
                    char next = _text[_index++];
                    if (next == ']') return values;
                    if (next != ',') throw Error("expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                SkipWhiteSpace();
                if (_index >= _text.Length || _text[_index] != '"') throw Error("expected a string");
                _index++;

                StringBuilder builder = new StringBuilder();
                while (true)
                {
                    if (_index >= _text.Length) throw Error("unterminated string");
                    char current = _text[_index++];
                    if (current == '"') return builder.ToString();
                    if (current != '\\')
                    {
                        if (current < ' ') throw Error("control character in a string");
                        builder.Append(current);
                        continue;
                    }

                    if (_index >= _text.Length) throw Error("unterminated escape sequence");
                    char escape = _text[_index++];
                    switch (escape)
                    {
                        case '"':
                            builder.Append('"');
                            break;
                        case '\\':
                            builder.Append('\\');
                            break;
                        case '/':
                            builder.Append('/');
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            if (_index + 4 > _text.Length) throw Error("truncated unicode escape");
                            int code = int.Parse(_text.Substring(_index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            _index += 4;
                            builder.Append((char)code);
                            break;
                        default:
                            throw Error("invalid escape sequence");
                    }
                }
            }

            private object ReadNumber()
            {
                int start = _index;
                if (_index < _text.Length && _text[_index] == '-') _index++;
                bool digits = false;
                while (_index < _text.Length && char.IsDigit(_text[_index])) { _index++; digits = true; }
                if (!digits) throw Error("expected a value");

                if (_index < _text.Length && _text[_index] == '.')
                {
                    _index++;
                    while (_index < _text.Length && char.IsDigit(_text[_index])) _index++;
                }
                if (_index < _text.Length && (_text[_index] == 'e' || _text[_index] == 'E'))
                {
                    _index++;
                    if (_index < _text.Length && (_text[_index] == '+' || _text[_index] == '-')) _index++;
                    while (_index < _text.Length && char.IsDigit(_text[_index])) _index++;
                }

                string text = _text.Substring(start, _index - start);
                long integer;
                if (!text.Contains(".") && !text.Contains("e") && !text.Contains("E") && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return integer;
                double number;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) throw Error("invalid number");
                return number;
            }

            private void ReadLiteral(string literal)
            {
                if (_index + literal.Length > _text.Length) throw Error("invalid literal");
                if (string.CompareOrdinal(_text, _index, literal, 0, literal.Length) != 0) throw Error("invalid literal");
                _index += literal.Length;
                SkipWhiteSpace();
            }

            private void SkipWhiteSpace()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index])) _index++;
            }

            private FormatException Error(string message)
            {
                return new FormatException(message + " at offset " + _index + ".");
            }
        }
    }
}
