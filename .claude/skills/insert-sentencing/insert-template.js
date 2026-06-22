/**
 * 量刑表插入辅助脚本模板
 * 用法：
 *   1. 复制此文件到项目根目录
 *   2. 按需修改 insertEntries() 函数中的新条目
 *   3. 运行：node insert.js
 *
 * 脚本会自动读取 4 个量刑表文件，执行插入，并验证结构完整性。
 */

const fs = require('fs');
const BASE = 'src/附录/4-量刑表';

// ========== 工具函数 ==========

// 计算显示宽度（中文=2，英文/数字/符号=1）
function width(s) {
  let n = 0;
  for (const c of s) {
    n += (c > '\xff' || '（）、：'.includes(c)) ? 2 : 1;
  }
  return n;
}

// 在行数组中查找包含 pattern 的行索引
function find(lines, pattern) {
  for (let i = 0; i < lines.length; i++) {
    if (lines[i].includes(pattern)) return i;
  }
  return -1;
}

// 读取文件为行数组
function load(name) {
  return fs.readFileSync(`${BASE}/${name}`, 'utf8').split('\r\n');
}

// 写回文件
function save(name, lines) {
  fs.writeFileSync(`${BASE}/${name}`, lines.join('\r\n'), 'utf8');
}

// 分析某节下所有相关条目，打印宽度和 tab 数
function analyzeSection(lines, sectionTitle) {
  let inside = false;
  console.log(`\n--- ${sectionTitle} ---`);
  for (let i = 0; i < lines.length; i++) {
    const l = lines[i];
    if (l.startsWith(`<h2>${sectionTitle}`)) inside = true;
    if (inside && l.startsWith('<h2>') && !l.includes(sectionTitle)) break;
    if (!inside) continue;
    if (l.includes('\t-') || l.includes('\t大不敬') || l.includes('-大不敬') || l.includes('-<article>')) {
      const textPart = l.split('\t')[0].replace(/-$/, '');
      const w = width(textPart);
      const tabs = (l.match(/\t/g) || []).length;
      console.log(`  w=${w} t=${tabs}  ${l.replace(/\t/g, '[T]')}`);
    }
  }
}

// 验证所有文件结构（<x-table> 与 </x-table> 是否配对）
function verify(files) {
  let ok = true;
  for (const [name, lines] of Object.entries(files)) {
    const opens = lines.filter(l => l.includes('<x-table')).length;
    const closes = lines.filter(l => l.includes('</x-table>')).length;
    const match = opens === closes;
    console.log(`${name}: ${opens} x-tables, ${closes} closes ${match ? '✓' : '✗'}`);
    if (!match) ok = false;
  }
  return ok;
}

// ========== 构建新条目 ==========

// 大不敬条目：自动计算 tab 数量
function entryDaBuji(text, article) {
  const w = width(text);
  // 根据已有条目经验：
  // w<=44 → 3 tabs → 大不敬 at 64；w>44 → 2 tabs → 大不敬 at 56
  const tabs = w <= 44 ? '\t\t\t' : '\t\t';
  return `${text}${tabs}大不敬\t<article>${article}</article>`;
}

// dash 条目（无十恶）：自动计算 tab 数量
// 参考同一节中宽度相近的已有条目，dash 前至少 1 tab
function entryDash(text, article, fileType) {
  const w = width(text);
  let beforeDash = '\t';  // 至少 1 tab

  if (fileType === 'zhang') {
    // 杖刑文件
    if (w <= 42) beforeDash = '\t\t\t\t';  // 主食级别
    else if (w <= 46) beforeDash = '\t\t\t'; // 监当官司级别
    else beforeDash = '\t';                  // 长文本
  } else {
    // 流刑/徒刑文件
    if (w <= 44) beforeDash = '\t\t\t';
    else if (w <= 48) beforeDash = '\t\t';
    else beforeDash = '\t';
  }

  const afterDash = fileType === 'liu' || fileType === 'tu' ? '\t\t' : '\t\t';
  return `${text}${beforeDash}-${afterDash}<article>${article}</article>`;
}

// ========== 入口：在此定义要插入的条目 ==========

function insertEntries() {
  // 读取所有文件
  const files = {
    si:    load('1-死刑.xml'),
    liu:   load('2-流刑.xml'),
    tu:    load('3-徒刑.xml'),
    zhang: load('4-杖刑.xml'),
  };

  // --- 示例：在 死刑·绞 节末尾追加 ---
  {
    const lines = files.si;
    const anchor = find(lines, '</x-table>');
    // 在 </x-table> 前插入
    lines.splice(anchor, 0,
      entryDaBuji('制备御船失职（工匠制备御船误不牢固已进御）', 104)
    );
  }

  // --- 示例：在 流刑·流三千里 末尾追加 ---
  {
    const lines = files.liu;
    const anchor = find(lines, '制作御膳失职（监当官司制作御膳误犯食禁已进御）');
    lines.splice(anchor + 1, 0,
      entryDaBuji('制备御船失职（工匠制备御船误不牢固未进御）', 104),
      entryDaBuji('制备御船失职（工匠制备皇太子船误不牢固已进御）', 104),
      entryDash('制备御船失职（监当官司制备御船误不牢固已进御）', 104, 'liu')
    );
  }

  // --- 示例：在 徒刑·徒三年 末尾追加 ---
  {
    const lines = files.tu;
    const anchor = find(lines, '制作御膳失职（监当官司制作皇太子膳误犯食禁已进御）');
    lines.splice(anchor + 1, 0,
      entryDaBuji('制备御船失职（工匠制备皇太子船误不牢固未进御）', 104),
      entryDash('制备御船失职（监当官司制备御船误不牢固未进御）', 104, 'tu'),
      entryDash('制备御船失职（监当官司制备皇太子船误不牢固已进御）', 104, 'tu')
    );
  }

  // --- 示例：在 徒刑·徒一年半 末尾追加 ---
  {
    const lines = files.tu;
    const anchor = find(lines, '制作御膳失职（监当官司制作御膳混入污秽之物已进御）');
    lines.splice(anchor + 1, 0,
      entryDash('制备御船失职（工匠制备御船未及时整饰未进御）', 104, 'tu'),
      entryDash('制备御船失职（工匠制备御船缺少设施未进御）', 104, 'tu'),
      entryDash('制备御船失职（工匠制备皇太子船未及时整饰已进御）', 104, 'tu'),
      entryDash('制备御船失职（工匠制备皇太子船缺少设施已进御）', 104, 'tu'),
      entryDash('制备御船失职（监当官司制备御船未及时整饰已进御）', 104, 'tu'),
      entryDash('制备御船失职（监当官司制备御船缺少设施已进御）', 104, 'tu')
    );
  }

  // --- 示例：在 杖刑·杖一百 末尾追加 ---
  {
    const lines = files.zhang;
    const lastZhang = find(lines, '制作御膳失职（监当官司制作皇太子膳混入污秽之物未进御）');
    lines.splice(lastZhang + 1, 0,
      entryDash('制备御船失职（监当官司制备皇太子船未及时整饰未进御）', 104, 'zhang'),
      entryDash('制备御船失职（监当官司制备皇太子船缺少设施未进御）', 104, 'zhang')
    );
  }

  // ========== 写回与验证 ==========
  save('1-死刑.xml', files.si);
  save('2-流刑.xml', files.liu);
  save('3-徒刑.xml', files.tu);
  save('4-杖刑.xml', files.zhang);

  // 读回验证
  const saved = {
    '1-死刑.xml': load('1-死刑.xml'),
    '2-流刑.xml': load('2-流刑.xml'),
    '3-徒刑.xml': load('3-徒刑.xml'),
    '4-杖刑.xml': load('4-杖刑.xml'),
  };

  console.log('\n结构验证：');
  const ok = verify(saved);

  console.log('\n新增条目统计：');
  for (const [name, lines] of Object.entries(saved)) {
    const count = lines.filter(l => l.includes('制备御船')).length;
    if (count > 0) console.log(`  ${name}: ${count} 条`);
  }

  console.log(ok ? '\n✓ 全部完成' : '\n✗ 结构有误，请检查');
}

// 运行
insertEntries();
