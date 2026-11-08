using System.Collections.Generic;
using System.Text;

namespace VGC.Localization.Import
{
    /// <summary>
    /// RFC 4180 の CSV を読む。セル内の改行・カンマ・"" によるエスケープに対応する。
    /// </summary>
    public static class CsvParser
    {
        public static List<string[]> Parse(string csv)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(csv))
                return rows;

            var row = new List<string>();
            var cell = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < csv.Length; i++)
            {
                var c = csv[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(cell.ToString());
                        cell.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(cell.ToString());
                        cell.Clear();
                        rows.Add(row.ToArray());
                        row.Clear();
                        break;
                    default:
                        cell.Append(c);
                        break;
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row.ToArray());
            }

            return rows;
        }
    }
}
