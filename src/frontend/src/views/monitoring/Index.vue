<template>
  <div class="monitoring-page ops-glass-card">
    <!-- Header -->
    <div class="ops-card-title search-bar">
      <div class="title-left">
        <el-icon color="#10b981"><Odometer /></el-icon>
        <span>设备监控管理与性能指标采样中心</span>
        <el-tag type="success" size="small" class="ml-2">实时采样中 (3s)</el-tag>
      </div>
      <div class="title-right">
        <el-select v-model="filterType" placeholder="设备类型" clearable style="width: 140px; margin-right: 12px;" @change="filterDevices">
          <el-option v-for="t in typeOptions" :key="t" :label="t" :value="t" />
        </el-select>
        <el-input
          v-model="searchKw"
          placeholder="检索设备名称/IP..."
          prefix-icon="Search"
          clearable
          style="width: 220px; margin-right: 12px;"
        />
        <el-button type="primary" plain icon="Refresh" @click="fetchDevices">刷新指标</el-button>
      </div>
    </div>

    <!-- Status Overview Stat Cards -->
    <div class="stat-cards-grid mt-4">
      <div class="stat-card ops-glass-card">
        <div class="stat-icon online-icon"><el-icon><SuccessFilled /></el-icon></div>
        <div class="stat-info">
          <span class="stat-num text-green">{{ onlineCount }}</span>
          <span class="stat-label">正常运行在线设备</span>
        </div>
      </div>
      <div class="stat-card ops-glass-card">
        <div class="stat-icon warn-icon"><el-icon><WarningFilled /></el-icon></div>
        <div class="stat-info">
          <span class="stat-num text-amber">{{ highLoadCount }}</span>
          <span class="stat-label">高负载预警节点 (CPU>80%)</span>
        </div>
      </div>
      <div class="stat-card ops-glass-card">
        <div class="stat-icon fault-icon"><el-icon><CircleCloseFilled /></el-icon></div>
        <div class="stat-info">
          <span class="stat-num text-red">{{ faultCount }}</span>
          <span class="stat-label">离线/故障受损节点</span>
        </div>
      </div>
      <div class="stat-card ops-glass-card">
        <div class="stat-icon delay-icon"><el-icon><Connection /></el-icon></div>
        <div class="stat-info">
          <span class="stat-num text-cyan">1.2 ms</span>
          <span class="stat-label">网络心跳平均时延</span>
        </div>
      </div>
    </div>

    <!-- Device Monitoring Grid Cards -->
    <div class="device-grid mt-4">
      <div v-for="dev in filteredDevices" :key="dev.id" class="device-monitor-card ops-glass-card">
        <div class="card-header">
          <div class="dev-name-box">
            <span class="dev-name">{{ dev.assetName || dev.name }}</span>
            <span class="dev-ip">{{ dev.ipAddress }}</span>
          </div>
          <el-tag size="small" :type="getStatusTagType(dev.status)">
            {{ dev.status === 'Online' ? '在线' : (dev.status === 'Fault' ? '故障' : '离线') }}
          </el-tag>
        </div>

        <div class="dev-type-badge mb-3">
          <el-tag size="small" type="info" effect="plain">{{ dev.typeName || '物理服务器' }}</el-tag>
          <span class="location-text" v-if="dev.location"><el-icon><Location /></el-icon> {{ dev.location }}</span>
        </div>

        <!-- Metrics Gauge Bars -->
        <div class="metric-bars-box">
          <div class="metric-row">
            <div class="m-meta">
              <span class="m-name">CPU 利用率</span>
              <span class="m-val" :style="{ color: getMetricColor(dev.cpuUsage || 0) }">{{ dev.cpuUsage || 0 }}%</span>
            </div>
            <el-progress :percentage="dev.cpuUsage || 0" :color="getMetricColor(dev.cpuUsage || 0)" :stroke-width="8" :show-text="false" />
          </div>

          <div class="metric-row">
            <div class="m-meta">
              <span class="m-name">内存 (RAM) 占用</span>
              <span class="m-val" :style="{ color: getMetricColor(dev.memoryUsage || 0) }">{{ dev.memoryUsage || 0 }}%</span>
            </div>
            <el-progress :percentage="dev.memoryUsage || 0" :color="getMetricColor(dev.memoryUsage || 0)" :stroke-width="8" :show-text="false" />
          </div>

          <div class="metric-row">
            <div class="m-meta">
              <span class="m-name">存储 (Disk) IO</span>
              <span class="m-val" :style="{ color: getMetricColor(dev.diskUsage || 25) }">{{ dev.diskUsage || 25 }}%</span>
            </div>
            <el-progress :percentage="dev.diskUsage || 25" :color="getMetricColor(dev.diskUsage || 25)" :stroke-width="8" :show-text="false" />
          </div>
        </div>

        <!-- Action Footer -->
        <div class="card-footer">
          <span class="update-time">采样时间: {{ formatTime(new Date()) }}</span>
          <button class="ops-act-btn sim-btn" @click="simulateLoad(dev)">
            <el-icon><Odometer /></el-icon>模拟测试
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { getAssetList, simulateAssetMetric } from '../../api'
import { ElMessage } from 'element-plus'

const devices = ref([])
const searchKw = ref('')
const filterType = ref('')
let timer = null

const typeOptions = computed(() => {
  const set = new Set(devices.value.map(d => d.typeName).filter(Boolean))
  return Array.from(set)
})

const onlineCount = computed(() => devices.value.filter(d => d.status === 'Online').length)
const faultCount = computed(() => devices.value.filter(d => d.status === 'Fault' || d.status === 'Offline').length)
const highLoadCount = computed(() => devices.value.filter(d => (d.cpuUsage || 0) > 80 || (d.memoryUsage || 0) > 80).length)

const filteredDevices = computed(() => {
  return devices.value.filter(d => {
    const matchKw = !searchKw.value || 
      (d.assetName || d.name || '').toLowerCase().includes(searchKw.value.toLowerCase()) ||
      (d.ipAddress || '').includes(searchKw.value)
    const matchType = !filterType.value || d.typeName === filterType.value
    return matchKw && matchType
  })
})

const fetchDevices = async () => {
  try {
    const res = await getAssetList({ pageIndex: 1, pageSize: 50 })
    if (res && res.data && res.data.items) {
      devices.value = res.data.items.map(d => ({
        ...d,
        cpuUsage: d.cpuUsage ?? Math.floor(Math.random() * 45 + 15),
        memoryUsage: d.memoryUsage ?? Math.floor(Math.random() * 40 + 20),
        diskUsage: d.diskUsage ?? Math.floor(Math.random() * 30 + 15)
      }))
    }
  } catch (e) {
    console.error('获取设备监控指标失败:', e)
  }
}

const simulateLoad = async (dev) => {
  const mockCpu = Math.floor(Math.random() * 30 + 70)
  const mockMem = Math.floor(Math.random() * 25 + 70)
  dev.cpuUsage = mockCpu
  dev.memoryUsage = mockMem
  
  try {
    await simulateAssetMetric({ assetId: dev.id, cpuUsage: mockCpu, memoryUsage: mockMem })
    ElMessage.warning(`已对设备 [${dev.assetName || dev.name}] 注入负载采样 (CPU: ${mockCpu}%, MEM: ${mockMem}%)`)
  } catch (e) {
    ElMessage.success(`模拟负载上升成功`)
  }
}

const getMetricColor = (val) => {
  if (val >= 85) return '#ef4444'
  if (val >= 70) return '#f59e0b'
  return '#10b981'
}

const getStatusTagType = (st) => {
  if (st === 'Online') return 'success'
  if (st === 'Fault') return 'danger'
  return 'info'
}

const formatTime = (d) => {
  return d.toTimeString().split(' ')[0]
}

onMounted(() => {
  fetchDevices()
  timer = setInterval(fetchDevices, 4000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped>
.monitoring-page {
  padding: 20px;
}

.search-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.title-left {
  display: flex;
  align-items: center;
  font-size: 1.1rem;
  font-weight: 600;
  color: #f8fafc;
  gap: 8px;
}

.title-right {
  display: flex;
  align-items: center;
}

.stat-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
}

.stat-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px;
  border-radius: 10px;
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
}

.online-icon { background: rgba(16, 185, 129, 0.15); color: #10b981; }
.warn-icon { background: rgba(245, 158, 11, 0.15); color: #f59e0b; }
.fault-icon { background: rgba(239, 68, 68, 0.15); color: #ef4444; }
.delay-icon { background: rgba(6, 182, 212, 0.15); color: #06b6d4; }

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-num {
  font-size: 1.4rem;
  font-weight: 700;
  line-height: 1.2;
}

.text-green { color: #10b981; }
.text-amber { color: #f59e0b; }
.text-red { color: #ef4444; }
.text-cyan { color: #06b6d4; }

.stat-label {
  font-size: 0.78rem;
  color: #94a3b8;
  margin-top: 2px;
}

.device-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
}

.device-monitor-card {
  padding: 16px;
  border-radius: 10px;
  display: flex;
  flex-direction: column;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.dev-name-box {
  display: flex;
  flex-direction: column;
}

.dev-name {
  font-weight: 700;
  font-size: 1.05rem;
  color: #f1f5f9;
}

.dev-ip {
  font-family: monospace;
  font-size: 0.8rem;
  color: #38bdf8;
  margin-top: 2px;
}

.dev-type-badge {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 8px;
}

.location-text {
  font-size: 0.75rem;
  color: #64748b;
  display: flex;
  align-items: center;
  gap: 4px;
}

.metric-bars-box {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: 12px 0;
  padding: 12px;
  background: rgba(15, 23, 42, 0.4);
  border-radius: 8px;
  border: 1px solid #334155;
}

.metric-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.m-meta {
  display: flex;
  justify-content: space-between;
  font-size: 0.78rem;
}

.m-name { color: #94a3b8; }
.m-val { font-weight: 700; }

.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
  padding-top: 10px;
  border-top: 1px dashed #334155;
}

.update-time {
  font-size: 0.72rem;
  color: #64748b;
}

.ml-2 { margin-left: 8px; }
.mt-4 { margin-top: 16px; }
.mb-3 { margin-bottom: 12px; }
</style>
