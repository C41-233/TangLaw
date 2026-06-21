using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Generator;

internal partial class Transformer
{

    private static void TransformXTable(XmlElement root)
    {

        static int CalcColspan(string node, Dictionary<string, List<string>> children)
        {
            if (!children.TryGetValue(node, out var kids))
                return 1;
            int sum = 0;
            foreach (var kid in kids)
                sum += CalcColspan(kid, children);
            return sum;
        }

        var doc = root.OwnerDocument;
        foreach (XmlElement node in root.SelectNodes(".//x-table"))
        {
            if (node.GetAttribute("type") != "arrow") continue;

            var table = doc.CreateElement("table");
            table.SetAttribute("class", "x-table");

            var title = node.GetAttribute("title");
            if (title.Length > 0)
            {
                var caption = doc.CreateElement("caption");
                caption.InnerText = title;
                table.AppendChild(caption);
            }

            // Phase 1: parse all lines, separate * -> first-row definitions
            var firstRows = new List<List<string>>();
            var dataLines = new List<string>();

            foreach (var rawLine in node.InnerText.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;
                var arrow = line.IndexOf("->", StringComparison.Ordinal);
                if (arrow < 0) continue;
                var left = line[..arrow].Trim();
                if (left == "*")
                {
                    // * -> xxx 作为表格第一行普通单元格
                    var right = line[(arrow + 2)..];
                    var cells = right.Split("->", StringSplitOptions.TrimEntries);
                    var row = new List<string>();
                    foreach (var cell in cells)
                    {
                        var trimmed = cell.Trim();
                        var starStart = trimmed.IndexOf('*');
                        var starEnd = trimmed.LastIndexOf('*');
                        if (starStart >= 0 && starEnd > starStart)
                        {
                            var key = trimmed[(starStart + 1)..starEnd];
                            var suffix = trimmed[(starEnd + 1)..];
                            row.Add((key + suffix).Replace("\\n", "<br/>"));
                        }
                        else
                        {
                            row.Add(trimmed.Replace("\\n", "<br/>"));
                        }
                    }
                    if (row.Count > 0)
                        firstRows.Add(row);
                }
                else
                {
                    dataLines.Add(line);
                }
            }

            // Phase 2: build tree from data lines
            var pairs = new List<(string Left, string Right)>();
            foreach (var line in dataLines)
            {
                var arrow = line.IndexOf("->", StringComparison.Ordinal);
                pairs.Add((line[..arrow].Trim(), line[(arrow + 2)..].Trim()));
            }

            if (pairs.Count == 0 && firstRows.Count == 0) continue;

            var children = new Dictionary<string, List<string>>();
            var allRights = new HashSet<string>();
            var displayNames = new Dictionary<string, string>();
            foreach (var (left, right) in pairs)
            {
                if (!children.ContainsKey(left))
                    children[left] = new List<string>();

                var starStart = right.IndexOf('*');
                var starEnd = right.LastIndexOf('*');
                if (starStart >= 0 && starEnd > starStart)
                {
                    var key = right[(starStart + 1)..starEnd];
                    if (!children[left].Contains(key))
                        children[left].Add(key);
                    allRights.Add(key);
                    var suffix = right[(starEnd + 1)..];
                    if (suffix.Length > 0)
                        displayNames[key] = key + suffix;
                }
                else
                {
                    if (!children[left].Contains(right))
                        children[left].Add(right);
                    allRights.Add(right);
                }
            }

            var roots = new List<string>();
            foreach (var left in children.Keys)
                if (!allRights.Contains(left))
                    roots.Add(left);

            var levels = new Dictionary<int, List<string>>();
            var queue = new Queue<(string, int)>();
            var visited = new HashSet<string>();
            foreach (var rootItem in roots)
            {
                if (visited.Add(rootItem))
                    queue.Enqueue((rootItem, 0));
            }

            while (queue.Count > 0)
            {
                var (item, level) = queue.Dequeue();
                if (!levels.ContainsKey(level))
                    levels[level] = new List<string>();
                levels[level].Add(item);

                if (children.TryGetValue(item, out var kids))
                {
                    foreach (var kid in kids)
                    {
                        if (visited.Add(kid))
                            queue.Enqueue((kid, level + 1));
                    }
                }
            }

            int totalCols = 0;
            foreach (var rootItem in roots)
                totalCols += CalcColspan(rootItem, children);

            var startCols = new Dictionary<string, int>();
            int col = 0;
            foreach (var rootItem in roots)
            {
                startCols[rootItem] = col;
                col += CalcColspan(rootItem, children);
            }

            int maxLevel = levels.Keys.Count > 0 ? levels.Keys.Max() : -1;
            for (int level = 0; level < maxLevel; level++)
            {
                if (!levels.TryGetValue(level, out var items)) continue;
                foreach (var item in items)
                {
                    if (!children.TryGetValue(item, out var kids)) continue;
                    int childStart = startCols[item];
                    foreach (var kid in kids)
                    {
                        startCols[kid] = childStart;
                        childStart += CalcColspan(kid, children);
                    }
                }
            }

            // Phase 3: render first rows from * -> definitions (as normal cells, not headers)
            foreach (var rowCells in firstRows)
            {
                var tr = doc.CreateElement("tr");
                if (rowCells.Count == 1 && totalCols > 1)
                {
                    var td = doc.CreateElement("td");
                    td.SetAttribute("colspan", totalCols.ToString());
                    td.InnerXml = rowCells[0];
                    tr.AppendChild(td);
                }
                else
                {
                    foreach (var cell in rowCells)
                    {
                        var td = doc.CreateElement("td");
                        td.InnerXml = cell;
                        tr.AppendChild(td);
                    }
                    for (int i = rowCells.Count; i < totalCols; i++)
                        tr.AppendChild(doc.CreateElement("td"));
                }
                table.AppendChild(tr);
            }

            // Phase 4: render tree rows
            // When * -> first rows exist, skip level 0 (star's children) to avoid redundancy
            int startLevel = firstRows.Count > 0 ? 1 : 0;
            for (int level = startLevel; level <= maxLevel; level++)
            {
                if (!levels.TryGetValue(level, out var items)) continue;

                var tr = doc.CreateElement("tr");
                var sortedItems = items.OrderBy(item => startCols[item]).ToList();

                int currentCol = 0;
                foreach (var item in sortedItems)
                {
                    while (currentCol < startCols[item])
                    {
                        tr.AppendChild(doc.CreateElement("td"));
                        currentCol++;
                    }

                    var td = doc.CreateElement("td");
                    var content = displayNames.TryGetValue(item, out var display) ? display : item;
                    td.InnerXml = content.Replace("\\n", "<br/>");
                    int span = CalcColspan(item, children);
                    if (span > 1)
                        td.SetAttribute("colspan", span.ToString());
                    tr.AppendChild(td);
                    currentCol += span;
                }

                while (currentCol < totalCols)
                {
                    tr.AppendChild(doc.CreateElement("td"));
                    currentCol++;
                }

                table.AppendChild(tr);
            }

            node.ParentNode.ReplaceChild(table, node);
        }
    }


}
