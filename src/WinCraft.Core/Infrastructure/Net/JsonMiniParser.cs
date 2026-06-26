using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WinCraft.Infrastructure.Net
{
    /// <summary>
    /// Minimal recursive-descent JSON parser that produces BCL primitives
    /// (<see cref="Dictionary{String,Object}"/>, <see cref="List{Object}"/>,
    /// <see cref="string"/>, <see cref="double"/>, <see cref="bool"/>, null).
    /// No external dependencies; works on net30 and net45.
    /// </summary>
    public static class JsonMiniParser
    {
        public static object Parse(string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            int pos = 0;
            object result = ParseValue(json, ref pos);
            SkipWhitespace(json, ref pos);

            if (pos != json.Length)
                throw NewFormatException(json, pos, "Unexpected trailing characters");

            return result;
        }

        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length)
            {
                char c = json[pos];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                {
                    pos++;
                    continue;
                }
                break;
            }
        }

        private static FormatException NewFormatException(string json, int pos, string message)
        {
            int start = Math.Max(0, pos - 20);
            int length = Math.Min(json.Length - start, 60);
            string snippet = json.Substring(start, length).Replace("\n", "\\n").Replace("\r", "\\r");
            return new FormatException(
                string.Format(CultureInfo.InvariantCulture,
                    "{0} (position {1}, near \"{2}\")", message, pos, snippet));
        }

        private static object ParseValue(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length)
                throw NewFormatException(json, pos, "Unexpected end of JSON");

            char c = json[pos];
            switch (c)
            {
                case '{': return ParseObject(json, ref pos);
                case '[': return ParseArray(json, ref pos);
                case '"': return ParseString(json, ref pos);
                case 't': return ParseLiteral(json, ref pos, "true", true);
                case 'f': return ParseLiteral(json, ref pos, "false", false);
                case 'n': ParseLiteral(json, ref pos, "null", null); return null;
                default:
                    if (c == '-' || (c >= '0' && c <= '9'))
                        return ParseNumber(json, ref pos);
                    throw NewFormatException(json, pos, "Unexpected character '" + c + "'");
            }
        }

        private static Dictionary<string, object> ParseObject(string json, ref int pos)
        {
            pos++;
            SkipWhitespace(json, ref pos);

            if (pos < json.Length && json[pos] == '}')
            {
                pos++;
                return new Dictionary<string, object>();
            }

            var obj = new Dictionary<string, object>();
            while (true)
            {
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != '"')
                    throw NewFormatException(json, pos, "Expected string key");
                string key = ParseString(json, ref pos);

                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':')
                    throw NewFormatException(json, pos, "Expected ':'");
                pos++;

                object value = ParseValue(json, ref pos);
                obj[key] = value;

                SkipWhitespace(json, ref pos);
                if (pos >= json.Length)
                    throw NewFormatException(json, pos, "Expected ',' or '}'");
                if (json[pos] == '}')
                {
                    pos++;
                    return obj;
                }
                if (json[pos] != ',')
                    throw NewFormatException(json, pos, "Expected ',' or '}'");
                pos++;
            }
        }

        private static List<object> ParseArray(string json, ref int pos)
        {
            pos++;
            SkipWhitespace(json, ref pos);

            if (pos < json.Length && json[pos] == ']')
            {
                pos++;
                return [];
            }

            var arr = new List<object>();
            while (true)
            {
                object value = ParseValue(json, ref pos);
                arr.Add(value);

                SkipWhitespace(json, ref pos);
                if (pos >= json.Length)
                    throw NewFormatException(json, pos, "Expected ',' or ']'");
                if (json[pos] == ']')
                {
                    pos++;
                    return arr;
                }
                if (json[pos] != ',')
                    throw NewFormatException(json, pos, "Expected ',' or ']'");
                pos++;
            }
        }

        private static string ParseString(string json, ref int pos)
        {
            pos++;
            var sb = new StringBuilder();
            int start = pos;

            while (pos < json.Length)
            {
                char c = json[pos];
                if (c == '"')
                {
                    if (pos > start)
                        sb.Append(json, start, pos - start);
                    pos++;
                    return sb.ToString();
                }
                if (c == '\\')
                {
                    if (pos > start)
                        sb.Append(json, start, pos - start);
                    pos++;
                    if (pos >= json.Length)
                        throw NewFormatException(json, pos, "Unexpected end of string escape");
                    char esc = json[pos];
                    switch (esc)
                    {
                        case '"':  sb.Append('"');  break;
                        case '\\': sb.Append('\\'); break;
                        case '/':  sb.Append('/');  break;
                        case 'b':  sb.Append('\b'); break;
                        case 'f':  sb.Append('\f'); break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        case 'u':
                            if (pos + 4 >= json.Length)
                                throw NewFormatException(json, pos, "Incomplete \\u escape");
                            string hex = json.Substring(pos + 1, 4);
                            int code;
                            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                                throw NewFormatException(json, pos, "Invalid \\u escape: " + hex);
                            sb.Append((char)code);
                            pos += 4;
                            break;
                        default:
                            throw NewFormatException(json, pos, "Unknown escape \\" + esc);
                    }
                    pos++;
                    start = pos;
                }
                else
                {
                    pos++;
                }
            }

            throw NewFormatException(json, pos, "Unterminated string");
        }

        private static double ParseNumber(string json, ref int pos)
        {
            int start = pos;
            if (pos < json.Length && json[pos] == '-')
                pos++;

            if (pos >= json.Length || json[pos] < '0' || json[pos] > '9')
                throw NewFormatException(json, pos, "Expected digit");

            while (pos < json.Length && json[pos] >= '0' && json[pos] <= '9')
                pos++;

            if (pos < json.Length && json[pos] == '.')
            {
                pos++;
                if (pos >= json.Length || json[pos] < '0' || json[pos] > '9')
                    throw NewFormatException(json, pos, "Expected digit after '.'");
                while (pos < json.Length && json[pos] >= '0' && json[pos] <= '9')
                    pos++;
            }

            if (pos < json.Length && (json[pos] == 'e' || json[pos] == 'E'))
            {
                pos++;
                if (pos < json.Length && (json[pos] == '+' || json[pos] == '-'))
                    pos++;
                if (pos >= json.Length || json[pos] < '0' || json[pos] > '9')
                    throw NewFormatException(json, pos, "Expected digit in exponent");
                while (pos < json.Length && json[pos] >= '0' && json[pos] <= '9')
                    pos++;
            }

            string numStr = json.Substring(start, pos - start);
            double value;
            if (!double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw NewFormatException(json, start, "Invalid number: " + numStr);
            return value;
        }

        private static object ParseLiteral(string json, ref int pos, string expected, object value)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                if (pos + i >= json.Length || json[pos + i] != expected[i])
                    throw NewFormatException(json, pos, "Expected '" + expected + "'");
            }
            pos += expected.Length;
            return value;
        }
    }
}
