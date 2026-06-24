using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Generator;

internal partial class Transformer
{

    private static void TransformXTable(XmlElement root)
    {
        foreach (XmlElement node in root.SelectNodes(".//x-table"))
        {
            var type = node.GetAttribute("type");
            if (type == "arrow")
                TransformXTableArrow(node);
            else
                TransformXTableTable(node);
        }
    }

    private static void TransformXTableArrow(XmlElement node)
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

        var doc = node.OwnerDocument;
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

        if (pairs.Count == 0 && firstRows.Count == 0) return;

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

    private static void TransformXTableTable(XmlElement node)
    {
        var doc = node.OwnerDocument;

        static string Esc(string s) => s.Replace("\\n", "<br/>");

        static string ProcessContent(XmlDocument doc, string content)
        {
            if (content.Length == 0)
                return "";
            var temp = doc.CreateElement("temp");
            temp.InnerXml = content;
            TransformArticleLink(temp);
            TransformArticleRef(temp);
            return temp.InnerXml;
        }

        static List<string> ParseLine(string line)
        {
            var parts = line.Split('\t');
            var cells = new List<string>();
            foreach (var part in parts)
                if (part.Length > 0)
                    cells.Add(part.Trim());
            return cells;
        }

        // Flatten child nodes to text, preserving XML elements as OuterXml
        var flatText = new System.Text.StringBuilder();
        foreach (var child in node.ChildNodes)
        {
            if (child is XmlText t)
                flatText.Append(t.InnerText);
            else if (child is XmlElement e)
                flatText.Append(e.OuterXml);
        }

        var lines = flatText.ToString().Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count < 2) return;

        // Parse header: * means merge with previous column
        var headerCells = ParseLine(lines[0]);


        var headerTexts = new List<string>();
        var headerColspans = new List<int>();
        foreach (var cell in headerCells)
        {
            if (cell == "*")
                headerColspans[headerColspans.Count - 1]++;
            else
            {
                headerTexts.Add(cell);
                headerColspans.Add(1);
            }
        }

        int physicalCols = headerCells.Count;

        // Parse data rows (physical columns)
        var dataRows = new List<List<string>>();
        for (int i = 1; i < lines.Count; i++)
        {
            var cells = ParseLine(lines[i]);
            while (cells.Count < physicalCols)
                cells.Add("-");
            dataRows.Add(cells);
        }

        // Build rendered view per row: * cells are merged into previous cell via colspan
        // Each rendered cell is (Value, Colspan, PhysicalIndex)
        var renderedRows = new List<List<(string Value, int Colspan, int PhysIdx)>>();
        foreach (var row in dataRows)
        {
            var rendered = new List<(string, int, int)>();
            int i = 0;
            while (i < physicalCols)
            {
                if (row[i] == "*")
                {
                    if (rendered.Count > 0)
                    {
                        var prev = rendered[^1];
                        rendered[^1] = (prev.Item1, prev.Item2 + 1, prev.Item3);
                    }
                    i++;
                }
                else
                {
                    rendered.Add((row[i], 1, i));
                    i++;
                }
            }
            renderedRows.Add(rendered);
        }

        // Group consecutive rows by first rendered cell value
        var groups = new List<(int PrefixLen, List<int> RowIndices)>();
        int idx = 0;
        while (idx < renderedRows.Count)
        {
            var firstVal = renderedRows[idx][0].Item1;
            int endIdx = idx + 1;
            while (endIdx < renderedRows.Count && renderedRows[endIdx][0].Item1 == firstVal)
                endIdx++;

            var groupIndices = Enumerable.Range(idx, endIdx - idx).ToList();

            int prefixLen = 0;
            if (groupIndices.Count > 1)
            {
                var firstRow = renderedRows[groupIndices[0]];
                int maxCompare = firstRow.Count;
                for (int c = 0; c < maxCompare; c++)
                {
                    var val = firstRow[c].Item1;
                    if (groupIndices.All(r => c < renderedRows[r].Count && renderedRows[r][c].Item1 == val))
                        prefixLen++;
                    else
                        break;
                }
            }

            groups.Add((prefixLen, groupIndices));
            idx = endIdx;
        }

        // Build table
        var table = doc.CreateElement("table");
        table.SetAttribute("class", "x-table");

        var title = node.GetAttribute("title");
        if (title.Length > 0)
        {
            var caption = doc.CreateElement("caption");
            caption.InnerText = title;
            table.AppendChild(caption);
        }

        // thead
        var thead = doc.CreateElement("thead");
        var headerTr = doc.CreateElement("tr");
        for (int i = 0; i < headerTexts.Count; i++)
        {
            var th = doc.CreateElement("th");
            th.InnerXml = ProcessContent(doc, Esc(headerTexts[i]));
            if (headerColspans[i] > 1)
                th.SetAttribute("colspan", headerColspans[i].ToString());
            headerTr.AppendChild(th);
        }
        thead.AppendChild(headerTr);
        table.AppendChild(thead);

        // tbody
        var tbody = doc.CreateElement("tbody");
        foreach (var (prefixLen, rowIndices) in groups)
        {
            int rowspan = rowIndices.Count;
            for (int ri = 0; ri < rowIndices.Count; ri++)
            {
                var tr = doc.CreateElement("tr");
                var rendered = renderedRows[rowIndices[ri]];

                for (int c = 0; c < rendered.Count; c++)
                {
                    if (ri > 0 && c < prefixLen)
                        continue;

                    var (value, colspan, physIdx) = rendered[c];

                    XmlElement cellEl;
                    if (physIdx == 0 || headerCells[physIdx] == "*")
                        cellEl = doc.CreateElement("th");
                    else
                        cellEl = doc.CreateElement("td");

                    cellEl.InnerXml = ProcessContent(doc, value == "-" ? "" : Esc(value));

                    if (ri == 0 && c < prefixLen)
                        cellEl.SetAttribute("rowspan", rowspan.ToString());

                    if (colspan > 1)
                        cellEl.SetAttribute("colspan", colspan.ToString());

                    tr.AppendChild(cellEl);
                }

                tbody.AppendChild(tr);
            }
        }
        table.AppendChild(tbody);

        node.ParentNode.ReplaceChild(table, node);
    }

}
