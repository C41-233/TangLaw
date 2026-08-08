import { watch } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';

const ROOT = dirname(fileURLToPath(import.meta.url));
const SRC_DIR = join(ROOT, 'src');
const GENERATOR = join(ROOT, 'bin', 'Generator.exe');
const DEBOUNCE_MS = 1000;

let building = false;
let pending = false;
let debounceTimer = null;

function log(msg) {
  console.log(`[watch ${new Date().toLocaleTimeString('zh-CN', { hour12: false })}] ${msg}`);
}

// 防抖：连续事件合并，只调度一次生成
function scheduleBuild() {
  clearTimeout(debounceTimer);
  debounceTimer = setTimeout(triggerBuild, DEBOUNCE_MS);
}

function triggerBuild() {
  if (building) {
    // 生成进行中：只记录待处理，生成结束后补建
    pending = true;
    return;
  }
  startBuild();
}

function startBuild() {
  building = true;
  log('开始生成...');
  const child = spawn(GENERATOR, ['src', 'docs/contents'], { cwd: ROOT, stdio: 'inherit' });
  child.on('close', (code) => {
    building = false;
    log(code === 0 ? '生成成功' : `生成失败（退出码 ${code}）`);
    if (pending) {
      pending = false;
      log('生成期间 src 又有变化，重新生成...');
      startBuild();
    } else {
      log('等待 src 变化...');
    }
  });
}

const watcher = watch(SRC_DIR, { recursive: true }, (eventType, filename) => {
  log(`检测到变化: src/${filename ?? '（未知文件）'}（${eventType}）`);
  scheduleBuild();
});
watcher.on('error', (err) => {
  log(`监听出错: ${err.message}`);
});

log(`正在监听 ${SRC_DIR}，按 Ctrl+C 退出`);
startBuild(); // 启动时立即生成一次，保证 docs 与 src 同步
