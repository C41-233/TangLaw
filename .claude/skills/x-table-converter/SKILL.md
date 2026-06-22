---
name: x-table-converter
description: 将 HTML table 转写为 x-table type="table" 格式。当用户提到"转写为x-table"、"table转x-table"、"x-table格式"、或在 src/释义 目录下添加/修改表格时使用此技能。即使只提及"转写table"或"转换表格格式"也应触发。
---

# x-table type="table" 转写规则

将 `<table>` 的 HTML 表格转写为 `<x-table type="table">` 的 Tab 分隔文本格式。

## 基本结构

```xml
<x-table title="表格标题" type="table">
列1	列2	列3	...
数据1	数据2	数据3	...
</x-table>
```

- 用 **Tab** 字符分隔列，列之间可使用不定数量的 Tab 进行视觉对齐
- 解析时多个连续空 Tab 段会被折叠，不影响逻辑列数
- `title` 属性 → `<caption>`，无标题则省略

## 转写规则

### 1. 表头行

首行为表头，按 Tab 分隔列名。

### 2. 列合并 (`*`)

表头中 `*` 表示该列合并到前一列（colspan）：

```xml
<!-- <th colspan="2">罪行</th> -->
罪行	*	法定刑	...
```

### 3. 空单元格 (`-`)

用 `-` 表示空 `<td></td>`，注意与表头的 `*` 区分（`*` 仅用于表头合并）：

```xml
<!-- <td></td> 或 <td>-</td> -->
数据1	-	数据3
```

### 4. 行合并（`（...）` 括号语法）

连续行首列前缀相同时，自动分组为 rowspan：

```
<!-- 
<tr><th rowspan="3">厌魅</th></tr>
<tr><th>意图杀人</th>...</tr>
<tr><th>意图致人疾苦</th>...</tr>
-->
厌魅（意图杀人）		皆斩	不道	...
厌魅（意图致人疾苦）	皆斩	不道	...
```

- 括号前部分 = 父行标签（rowspan = 子行数 + 1）
- 括号内部分 = 子行标签

### 5. 保留原始标签

`<article>`、`<article-ref>` 等标签原样保留，不做转义：

```xml
<!-- <td><article>120</article></td> -->
<article>120</article>
```

### 6. 换行

如需在单元格内换行，用 `\n` 表示 `<br/>`。

## 操作步骤

1. 在 **原 `<table>` 之前** 插入 `<x-table>`，原 table 保持不变以便对照
2. 提取 `<caption>` 文本作为 `title` 属性
3. 将 `<thead>` 的行转为表头行（Tab 分隔）
4. 将 `<tbody>` 的行转为数据行
5. `colspan` → 表头对应列后追加 `*`（每多1列加一个 `*`）
6. `rowspan` → 将子行首列改为 `父（子）` 格式，连续排列
7. 空 `<td></td>` → `-`
8. 尾随空列可省略 `-`

## 示例

**输入 HTML:**
```html
<table>
    <caption>对父母的犯罪</caption>
    <thead><tr><th colspan="2">罪行</th><th>法定刑</th></tr></thead>
    <tbody>
        <tr><th colspan="2">谋杀</th><td>皆斩</td></tr>
        <tr><th rowspan="2">厌魅</th></tr>
        <tr><th>意图杀人</th><td>皆斩</td></tr>
    </tbody>
</table>
```

**输出 x-table:**
```xml
<x-table title="对父母的犯罪" type="table">
罪行	*	法定刑
谋杀		皆斩
厌魅（意图杀人）	皆斩
</x-table>
```
