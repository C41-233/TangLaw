using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var table = doc.CreateElement("table");
            table.SetAttribute("class", "x-table");

            var pairs = new List<(string Left, string Right)>();
            foreach (var rawLine in node.InnerText.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;
                var arrow = line.IndexOf("->", StringComparison.Ordinal);
                if (arrow < 0) continue;
                pairs.Add((line[..arrow].Trim(), line[(arrow + 2)..].Trim()));
            }

            if (pairs.Count == 0) continue;

            var children = new Dictionary<string, List<string>>();
            var allRights = new HashSet<string>();
            foreach (var (left, right) in pairs)
            {
                if (!children.ContainsKey(left))
                    children[left] = new List<string>();
                children[left].Add(right);
                allRights.Add(right);
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
                visited.Add(rootItem);
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

            int maxLevel = levels.Keys.Max();
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

            for (int level = 0; level <= maxLevel; level++)
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
                    td.InnerText = item;
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
