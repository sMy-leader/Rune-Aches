using System;
using System.Collections.Generic;
using System.Globalization;

namespace RuneAshes.Data
{
    public static class RuneCsvParser
    {
        public static List<RuneDef> Parse(string csv)
        {
            var rows = CsvReader.Read(csv);
            var result = new List<RuneDef>(rows.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var lineNumber = i + 2;

                var id = GetRequired(row, "id", lineNumber);
                var name = Get(row, "name");
                var categoryText = GetRequired(row, "category", lineNumber);
                var energyCost = ParseInt(Get(row, "energy_cost"), 0, "energy_cost", lineNumber);
                var baseValue = ParseFloat(Get(row, "base_value"), 0f, "base_value", lineNumber);
                var maxLevel = ParseInt(Get(row, "max_level"), 0, "max_level", lineNumber);
                var description = Get(row, "desc");
                var tags = SplitList(Get(row, "tags"), '|');

                var paramKeys = new List<string>();
                var paramValues = new List<string>();
                var paramText = Get(row, "params");
                if (!string.IsNullOrWhiteSpace(paramText))
                {
                    foreach (var pair in SplitList(paramText, '|'))
                    {
                        var equals = pair.IndexOf('=');
                        if (equals <= 0)
                        {
                            throw new FormatException("Line " + lineNumber + ": params entry '" + pair + "' must be key=value.");
                        }

                        paramKeys.Add(pair.Substring(0, equals).Trim());
                        paramValues.Add(pair.Substring(equals + 1).Trim());
                    }
                }

                RuneCategory category;
                if (!Enum.TryParse(categoryText, true, out category))
                {
                    throw new FormatException("Line " + lineNumber + ": unknown category '" + categoryText + "'.");
                }

                result.Add(RuneDef.Create(
                    id,
                    name,
                    category,
                    energyCost,
                    baseValue,
                    maxLevel,
                    description,
                    tags.ToArray(),
                    paramKeys.ToArray(),
                    paramValues.ToArray()));
            }

            return result;
        }

        private static string Get(Dictionary<string, string> row, string key)
        {
            string value;
            return row.TryGetValue(key, out value) ? value : string.Empty;
        }

        private static string GetRequired(Dictionary<string, string> row, string key, int lineNumber)
        {
            var value = Get(row, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException("Line " + lineNumber + ": column '" + key + "' is required.");
            }

            return value;
        }

        private static int ParseInt(string text, int fallback, string column, int lineNumber)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallback;
            }

            int value;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                throw new FormatException("Line " + lineNumber + ": column '" + column + "' is not an int ('" + text + "').");
            }

            return value;
        }

        private static float ParseFloat(string text, float fallback, string column, int lineNumber)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallback;
            }

            float value;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                throw new FormatException("Line " + lineNumber + ": column '" + column + "' is not a float ('" + text + "').");
            }

            return value;
        }

        private static List<string> SplitList(string text, char separator)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return list;
            }

            foreach (var part in text.Split(separator))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    list.Add(trimmed);
                }
            }

            return list;
        }
    }
}
