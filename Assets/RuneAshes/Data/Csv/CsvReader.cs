using System;
using System.Collections.Generic;
using System.Text;

namespace RuneAshes.Data
{
    /// <summary>
    /// 最小 CSV 读取器：支持 UTF-8 BOM、# 注释、空行、双引号包裹与 "" 转义。
    /// 不支持引号内换行（配置表约定不使用）。
    /// </summary>
    public static class CsvReader
    {
        public static List<Dictionary<string, string>> Read(string text)
        {
            var rows = new List<Dictionary<string, string>>();
            if (string.IsNullOrEmpty(text))
            {
                return rows;
            }

            if (text[0] == '\uFEFF')
            {
                text = text.Substring(1);
            }

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            string[] header = null;

            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var fields = ParseLine(line);
                if (header == null)
                {
                    header = new string[fields.Count];
                    for (var i = 0; i < fields.Count; i++)
                    {
                        header[i] = fields[i].Trim();
                    }

                    continue;
                }

                var row = new Dictionary<string, string>(header.Length, StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < header.Length; i++)
                {
                    row[header[i]] = i < fields.Count ? fields[i].Trim() : string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static List<string> ParseLine(string line)
        {
            var fields = new List<string>();
            var builder = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            builder.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(builder.ToString());
                    builder.Length = 0;
                }
                else
                {
                    builder.Append(c);
                }
            }

            fields.Add(builder.ToString());
            return fields;
        }
    }
}
