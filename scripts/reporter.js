/**
 * 基于.NET的企业运维管理平台 - 自动化数据上报与压力测试模拟探针 (Node.js 脚本)
 * 
 * 运行方式:
 *   1. 持续实时上报模拟:  node scripts/reporter.js
 *   2. 单次批量更新模式:  node scripts/reporter.js --once
 *   3. 模拟紧急故障投掷:  node scripts/reporter.js --fault
 */

const BASE_URL = process.env.API_URL || 'http://localhost:5000/api';

// ANSI 彩色输出格式化
const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  red: '\x1b[31m',
  cyan: '\x1b[36m',
  blue: '\x1b[34m'
};

function log(msg, color = colors.reset) {
  const timestamp = new Date().toLocaleTimeString();
  console.log(`${colors.cyan}[${timestamp}]${colors.reset} ${color}${msg}${colors.reset}`);
}

async function request(endpoint, options = {}) {
  const url = `${BASE_URL}${endpoint}`;
  const headers = { 'Content-Type': 'application/json', ...options.headers };
  
  try {
    const res = await fetch(url, { ...options, headers });
    const json = await res.json();
    return json;
  } catch (err) {
    log(`HTTP 请求异常 [${endpoint}]: ${err.message}`, colors.red);
    return { success: false, message: err.message };
  }
}

let token = '';

async function login() {
  log('正在连接后台 API 进行探针身份鉴权认证...', colors.blue);
  const res = await request('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username: 'ops', password: 'ops123' })
  });

  if (res.success && res.data) {
    token = res.data.token;
    log(`探针鉴权成功! 当前认证身份: [${res.data.realName}] (${res.data.roleName})`, colors.green);
    return true;
  } else {
    log(`鉴权失败: ${res.message || '后台 API (http://localhost:5000) 尚未启动'}`, colors.red);
    return false;
  }
}

async function getAssets() {
  const res = await request('/asset/list?pageSize=50', {
    headers: { Authorization: `Bearer ${token}` }
  });
  return res.success ? (res.data.items || []) : [];
}

async function simulateMetric(assetId, status, cpuUsage, memoryUsage, diskUsage) {
  const res = await request('/asset/simulate-metric', {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
    body: JSON.stringify({ assetId, status, cpuUsage, memoryUsage, diskUsage })
  });
  return res.success;
}

// 模拟随机浮动数据上报
async function runLoop(isFaultMode = false) {
  const assets = await getAssets();
  if (!assets.length) {
    log('未获取到有效的资产设备列表，跳过本轮上报', colors.yellow);
    return;
  }

  log(`--- 本轮自动监测数据上报启动 (覆盖 ${assets.length} 台资产设备) ---`, colors.cyan);

  for (const asset of assets) {
    let newStatus = asset.status;
    let newCpu = asset.cpuUsage;
    let newMem = asset.memoryUsage;
    let newDisk = asset.diskUsage;

    if (isFaultMode && (asset.type === 'Database' || asset.type === 'CloudHost')) {
      // 故障投掷模式: 将部分关键设备强行设定为过载/故障
      newStatus = Math.random() > 0.5 ? 'Fault' : 'Online';
      newCpu = Math.floor(88 + Math.random() * 11.5); // 88% ~ 99.5%
      newMem = Math.floor(91 + Math.random() * 8.5);
      log(`[故障投掷] 设备 [${asset.name}] (${asset.ipAddress}) 被强行置为 ${newStatus}, CPU=${newCpu}%`, colors.red);
    } else {
      // 正常波动模式: 围绕原值产生波浪微幅震荡
      const cpuChange = (Math.random() - 0.45) * 6.0;
      const memChange = (Math.random() - 0.45) * 4.0;
      
      newCpu = Math.min(99.9, Math.max(5.0, parseFloat((newCpu + cpuChange).toFixed(1))));
      newMem = Math.min(99.0, Math.max(10.0, parseFloat((newMem + memChange).toFixed(1))));
    }

    const ok = await simulateMetric(asset.id, newStatus, newCpu, newMem, newDisk);
    if (ok) {
      const color = newCpu >= 85 || newStatus !== 'Online' ? colors.yellow : colors.green;
      log(`资产 [${asset.assetNo}] ${asset.name} 上报成功 -> 状态:${newStatus} | CPU:${newCpu}% | 内存:${newMem}%`, color);
    }
  }
}

async function main() {
  console.log(`\n======================================================`);
  console.log(`  基于.NET的企业运维管理平台 - 探针数据自动化上报程序 `);
  console.log(`======================================================\n`);

  const args = process.argv.slice(2);
  const isOnce = args.includes('--once');
  const isFault = args.includes('--fault');

  const ok = await login();
  if (!ok) return;

  if (isOnce) {
    log('以 [单次上报] 模式运行...', colors.cyan);
    await runLoop(isFault);
    log('单次数据上报已成功完成!', colors.green);
    process.exit(0);
  } else {
    log(`以 [持续轮询上报] 模式运行 (每 4 秒自动巡检上报一轮)... 按 Ctrl+C 停止`, colors.bright);
    await runLoop(isFault);
    setInterval(async () => {
      await runLoop(isFault);
    }, 4000);
  }
}

main();
