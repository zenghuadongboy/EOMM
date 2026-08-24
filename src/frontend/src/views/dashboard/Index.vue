<template>
  <div class="dashboard-page">
    <!-- Top Stat Cards Grid -->
    <el-row :gutter="16" class="stat-cards-row">
      <el-col :span="6">
        <div class="ops-glass-card stat-card">
          <div class="stat-icon primary">
            <el-icon :size="28"><Cpu /></el-icon>
          </div>
          <div class="stat-info">
            <div class="label">IT 资产设备总量</div>
            <div class="value">{{ overview.totalAssets || 0 }} <span class="unit">台</span></div>
          </div>
        </div>
      </el-col>

      <el-col :span="6">
        <div class="ops-glass-card stat-card">
          <div class="stat-icon success">
            <el-icon :size="28"><CircleCheck /></el-icon>
          </div>
          <div class="stat-info">
            <div class="label">正常在线运行率</div>
            <div class="value">{{ onlineRate }}%</div>
          </div>
        </div>
      </el-col>

      <el-col :span="6">
        <div class="ops-glass-card stat-card">
          <div class="stat-icon warning">
            <el-icon :size="28"><Odometer /></el-icon>
          </div>
          <div class="stat-info">
            <div class="label">全网平均 CPU 负载</div>
            <div class="value warning-text">{{ (overview.averageCpuUsage || 0).toFixed(1) }} <span class="unit">%</span></div>
          </div>
        </div>
      </el-col>

      <el-col :span="6">
        <div class="ops-glass-card stat-card">
          <div class="stat-icon danger">
            <el-icon :size="28"><Bell /></el-icon>
          </div>
          <div class="stat-info">
            <div class="label">待处置警报数</div>
            <div class="value danger-text">{{ overview.pendingAlarms || 0 }} <span class="unit">条</span></div>
          </div>
        </div>
      </el-col>
    </el-row>

    <!-- Charts Row 1 -->
    <el-row :gutter="16" class="charts-row">
      <el-col :span="15">
        <div class="ops-glass-card chart-card">
          <div class="ops-card-title flex-between">
            <div class="title-left">
              <el-icon color="#3b82f6"><TrendCharts /></el-icon>
              全网设备实时 CPU / 内存 动态负载波形 (实时心电图)
            </div>
            <div class="realtime-indicator">
              <span class="pulse-dot green"></span>
              探针数据流实时上报驱动中 (每3s刷新)
            </div>
          </div>
          <div ref="realtimeChartRef" class="echarts-container"></div>
        </div>
      </el-col>

      <el-col :span="9">
        <div class="ops-glass-card chart-card">
          <div class="ops-card-title">
            <el-icon color="#06b6d4"><PieChart /></el-icon>
            资产分类架构占比
          </div>
          <div ref="typeChartRef" class="echarts-container"></div>
        </div>
      </el-col>
    </el-row>

    <!-- Charts Row 2 -->
    <el-row :gutter="16" class="charts-row">
      <el-col :span="9">
        <div class="ops-glass-card chart-card">
          <div class="ops-card-title">
            <el-icon color="#f59e0b"><DataAnalysis /></el-icon>
            平均故障修复时长 (MTTR)
          </div>
          <div class="mttr-box">
            <div class="mttr-value">{{ overview.averageMttrHours || 1.8 }}</div>
            <div class="mttr-unit">Hours / 故障平均修复工时</div>
            <div class="mttr-desc">
              系统监控故障从自动化触发、工单响应到最终解决闭环的算术平均值。
            </div>
          </div>
        </div>
      </el-col>

      <el-col :span="15">
        <div class="ops-glass-card chart-card">
          <div class="ops-card-title">
            <el-icon color="#ef4444"><List /></el-icon>
            高频故障资产热点 Top 5
          </div>
          <el-table :data="overview.topFaultAssets || []" size="small">
            <el-table-column prop="assetName" label="设备名称" min-width="150" />
            <el-table-column prop="ipAddress" label="IP 地址" width="130" />
            <el-table-column prop="faultCount" label="告警累计次数" width="120" align="center">
              <template #default="scope">
                <el-tag type="danger" effect="dark" round>{{ scope.row.faultCount }} 次</el-tag>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { getDashboardOverview } from '../../api'
import * as echarts from 'echarts'

const overview = ref({})
let timer = null

const onlineRate = computed(() => {
  if (!overview.value.totalAssets) return '100.0'
  const rate = (overview.value.onlineAssets / overview.value.totalAssets) * 100
  return rate.toFixed(1)
})

const realtimeChartRef = ref(null)
const typeChartRef = ref(null)

let realtimeChartInstance = null
let typeChartInstance = null

// Realtime Queue for 12 data points
const realtimeTimeLabels = ref([])
const realtimeCpuQueue = ref([])
const realtimeMemQueue = ref([])

const initRealtimeQueue = () => {
  const now = new Date()
  for (let i = 11; i >= 0; i--) {
    const t = new Date(now.getTime() - i * 3000)
    const timeStr = `${String(t.getHours()).padStart(2, '0')}:${String(t.getMinutes()).padStart(2, '0')}:${String(t.getSeconds()).padStart(2, '0')}`
    realtimeTimeLabels.value.push(timeStr)
    realtimeCpuQueue.value.push(45.0)
    realtimeMemQueue.value.push(55.0)
  }
}

const updateRealtimeChart = (newCpu, newMem) => {
  if (!realtimeChartRef.value) return
  if (!realtimeChartInstance) {
    realtimeChartInstance = echarts.init(realtimeChartRef.value)
  }

  const now = new Date()
  const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:${String(now.getSeconds()).padStart(2, '0')}`

  realtimeTimeLabels.value.shift()
  realtimeTimeLabels.value.push(timeStr)

  realtimeCpuQueue.value.shift()
  realtimeCpuQueue.value.push(newCpu)

  realtimeMemQueue.value.shift()
  realtimeMemQueue.value.push(newMem)

  realtimeChartInstance.setOption({
    tooltip: { trigger: 'axis' },
    legend: { textStyle: { color: '#cbd5e1' } },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: {
      type: 'category',
      data: realtimeTimeLabels.value,
      axisLine: { lineStyle: { color: '#475569' } }
    },
    yAxis: {
      type: 'value',
      min: 0,
      max: 100,
      splitLine: { lineStyle: { color: '#334155' } },
      axisLine: { lineStyle: { color: '#475569' } }
    },
    series: [
      {
        name: '平均 CPU 使用率 (%)',
        type: 'line',
        smooth: true,
        data: realtimeCpuQueue.value,
        itemStyle: { color: '#f59e0b' },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(245, 158, 11, 0.35)' },
            { offset: 1, color: 'rgba(245, 158, 11, 0.0)' }
          ])
        }
      },
      {
        name: '平均 内存 占用率 (%)',
        type: 'line',
        smooth: true,
        data: realtimeMemQueue.value,
        itemStyle: { color: '#3b82f6' },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(59, 130, 246, 0.35)' },
            { offset: 1, color: 'rgba(59, 130, 246, 0.0)' }
          ])
        }
      }
    ]
  })
}

const updateTypeChart = () => {
  if (!typeChartRef.value) return
  if (!typeChartInstance) {
    typeChartInstance = echarts.init(typeChartRef.value)
  }
  const typeItems = overview.value.assetsByType || []
  typeChartInstance.setOption({
    tooltip: { trigger: 'item' },
    legend: { bottom: '5%', left: 'center', textStyle: { color: '#cbd5e1' } },
    series: [{
      name: '资产类型',
      type: 'pie',
      radius: ['40%', '70%'],
      avoidLabelOverlap: false,
      itemStyle: { borderRadius: 10, borderColor: '#1e293b', borderWidth: 2 },
      label: { show: false },
      data: typeItems
    }]
  })
}

const loadData = async () => {
  const res = await getDashboardOverview()
  if (res.data) {
    overview.value = res.data
    const cpu = res.data.averageCpuUsage || 45.0
    const mem = res.data.averageMemoryUsage || 55.0
    nextTick(() => {
      updateRealtimeChart(cpu, mem)
      updateTypeChart()
    })
  }
}

onMounted(() => {
  initRealtimeQueue()
  loadData()
  timer = setInterval(loadData, 3000)
  window.addEventListener('resize', () => {
    realtimeChartInstance?.resize()
    typeChartInstance?.resize()
  })
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped>
.dashboard-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.flex-between {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.realtime-indicator {
  font-size: 0.75rem;
  color: #34d399;
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 500;
}

.stat-card {
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  width: 56px;
  height: 56px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-icon.primary { background: rgba(59, 130, 246, 0.15); color: #3b82f6; }
.stat-icon.success { background: rgba(16, 185, 129, 0.15); color: #10b981; }
.stat-icon.danger { background: rgba(239, 68, 68, 0.15); color: #ef4444; }
.stat-icon.warning { background: rgba(245, 158, 11, 0.15); color: #f59e0b; }

.stat-info .label {
  font-size: 0.85rem;
  color: #94a3b8;
}

.stat-info .value {
  font-size: 1.6rem;
  font-weight: 700;
  color: #f8fafc;
  margin-top: 4px;
}

.stat-info .unit {
  font-size: 0.85rem;
  color: #64748b;
  font-weight: 400;
}

.danger-text { color: #f87171 !important; }
.warning-text { color: #fbbf24 !important; }

.chart-card {
  padding: 20px;
  height: 360px;
  display: flex;
  flex-direction: column;
}

.echarts-container {
  flex: 1;
  width: 100%;
}

.mttr-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  text-align: center;
}

.mttr-value {
  font-size: 4rem;
  font-weight: 800;
  color: #38bdf8;
  line-height: 1;
}

.mttr-unit {
  font-size: 0.9rem;
  color: #94a3b8;
  margin-top: 8px;
}

.mttr-desc {
  font-size: 0.8rem;
  color: #64748b;
  max-width: 280px;
  margin-top: 16px;
}
</style>
