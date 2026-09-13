<template>
  <div class="assets-page ops-glass-card">
    <div class="ops-card-title search-bar">
      <div class="title-left">
        <el-icon color="#3b82f6"><Cpu /></el-icon>
        IT 设备资产全生命周期管理台账
      </div>
      <div class="actions-right flex-center gap-12">
        <div class="realtime-indicator">
          <span class="pulse-dot green"></span>
          自动刷新 (3s)
        </div>
        <el-button type="primary" icon="Plus" @click="openAddDialog">新增资产设备</el-button>
      </div>
    </div>

    <!-- Search & Filter Form -->
    <div class="filter-box">
      <el-input
        v-model="query.keyword"
        placeholder="搜索名称 / 编号 / IP 地址"
        style="width: 240px"
        clearable
        @clear="loadData"
        @keyup.enter="loadData"
      >
        <template #prefix><el-icon><Search /></el-icon></template>
      </el-input>

      <el-select v-model="query.type" placeholder="资产类型" style="width: 150px" clearable @change="loadData">
        <el-option label="物理服务器" value="Server" />
        <el-option label="云主机" value="CloudHost" />
        <el-option label="网络设备" value="NetworkDevice" />
        <el-option label="存储设备" value="Storage" />
        <el-option label="数据库" value="Database" />
      </el-select>

      <el-select v-model="query.status" placeholder="运行状态" style="width: 140px" clearable @change="loadData">
        <el-option label="在线 (Online)" value="Online" />
        <el-option label="离线 (Offline)" value="Offline" />
        <el-option label="故障 (Fault)" value="Fault" />
        <el-option label="维修中 (Maintenance)" value="Maintenance" />
      </el-select>

      <el-button type="primary" plain @click="loadData">查询筛选</el-button>
      <el-button @click="resetQuery">重置</el-button>
    </div>

    <!-- Assets Data Table -->
    <el-table :data="tableData" style="width: 100%; margin-top: 16px;">
      <el-table-column prop="assetNo" label="资产编号" width="130" />
      <el-table-column prop="name" label="设备名称" min-width="180">
        <template #default="scope">
          <div class="asset-name-cell">
            <span class="name">{{ scope.row.name }}</span>
            <span class="spec">{{ scope.row.hardwareSpec }}</span>
          </div>
        </template>
      </el-table-column>
      <el-table-column prop="type" label="类型" width="110">
        <template #default="scope">
          <el-tag size="small" effect="plain">{{ formatType(scope.row.type) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="ipAddress" label="IP 地址" width="130">
        <template #default="scope">
          <code>{{ scope.row.ipAddress }}:{{ scope.row.port }}</code>
        </template>
      </el-table-column>
      <el-table-column prop="department" label="所属部门/责任人" width="150">
        <template #default="scope">
          <div>{{ scope.row.department }}</div>
          <div class="sub-text">{{ scope.row.manager }}</div>
        </template>
      </el-table-column>
      <el-table-column label="资源监控指标 (CPU/内存/磁盘)" width="200">
        <template #default="scope">
          <div class="metrics-cell">
            <div class="metric-item">
              <span class="m-label">CPU:</span>
              <el-progress :percentage="scope.row.cpuUsage" :color="getMetricColor(scope.row.cpuUsage)" :stroke-width="6" />
            </div>
            <div class="metric-item">
              <span class="m-label">MEM:</span>
              <el-progress :percentage="scope.row.memoryUsage" :color="getMetricColor(scope.row.memoryUsage)" :stroke-width="6" />
            </div>
          </div>
        </template>
      </el-table-column>
      <el-table-column prop="status" label="运行状态" width="120" align="center">
        <template #default="scope">
          <span :class="['status-tag', scope.row.status.toLowerCase()]">
            <span class="pulse-dot"></span>
            {{ formatStatus(scope.row.status) }}
          </span>
        </template>
      </el-table-column>
      <el-table-column width="220" fixed="right" align="center">
        <template #header>
          <div class="action-header">
            <el-icon color="#3b82f6"><Setting /></el-icon>
            <span>管理操作</span>
          </div>
        </template>
        <template #default="scope">
          <div class="action-btn-group">
            <button class="ops-act-btn edit-btn" @click="openEditDialog(scope.row)">
              <el-icon><Edit /></el-icon>编辑
            </button>
            <button class="ops-act-btn sim-btn" @click="openSimulateDialog(scope.row)">
              <el-icon><Odometer /></el-icon>压力
            </button>
            <button class="ops-act-btn del-btn" @click="handleDelete(scope.row)">
              <el-icon><Delete /></el-icon>删除
            </button>
          </div>
        </template>
      </el-table-column>
    </el-table>

    <!-- Pagination -->
    <div class="pagination-box">
      <el-pagination
        v-model:current-page="query.pageIndex"
        v-model:page-size="query.pageSize"
        :total="total"
        layout="total, prev, pager, next, jumper"
        @current-change="loadData"
      />
    </div>

    <!-- Edit/Add Dialog -->
    <el-dialog v-model="showEditModal" :title="form.id === 0 ? '新增资产设备' : '编辑资产设备'" width="560px" append-to-body align-center>
      <el-form :model="form" label-width="100px">
        <el-form-item label="资产编号">
          <el-input v-model="form.assetNo" :disabled="form.id !== 0" placeholder="例如: AST-SRV-008" />
        </el-form-item>
        <el-form-item label="设备名称">
          <el-input v-model="form.name" placeholder="请输入设备名称" />
        </el-form-item>
        <el-form-item label="资产分类">
          <el-select v-model="form.type" style="width: 100%">
            <el-option label="物理服务器" value="Server" />
            <el-option label="云主机" value="CloudHost" />
            <el-option label="网络设备" value="NetworkDevice" />
            <el-option label="存储设备" value="Storage" />
            <el-option label="数据库" value="Database" />
          </el-select>
        </el-form-item>
        <el-form-item label="IP 地址 / 端口">
          <el-row :gutter="10">
            <el-col :span="16"><el-input v-model="form.ipAddress" placeholder="192.168.1.10" /></el-col>
            <el-col :span="8"><el-input-number v-model="form.port" :min="1" :max="65535" style="width: 100%" /></el-col>
          </el-row>
        </el-form-item>
        <el-form-item label="所属部门/责任人">
          <el-row :gutter="10">
            <el-col :span="12"><el-input v-model="form.department" placeholder="部门" /></el-col>
            <el-col :span="12"><el-input v-model="form.manager" placeholder="责任人" /></el-col>
          </el-row>
        </el-form-item>
        <el-form-item label="运行状态">
          <el-select v-model="form.status" style="width: 100%">
            <el-option label="在线 (Online)" value="Online" />
            <el-option label="离线 (Offline)" value="Offline" />
            <el-option label="故障 (Fault)" value="Fault" />
            <el-option label="维修中 (Maintenance)" value="Maintenance" />
          </el-select>
        </el-form-item>
        <el-form-item label="硬件规格说明">
          <el-input v-model="form.hardwareSpec" placeholder="例如: 32C 128GB SSD 1TB" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showEditModal = false">取消</el-button>
        <el-button type="primary" @click="submitForm">保存提交</el-button>
      </template>
    </el-dialog>

    <!-- Simulate Stress Dialog -->
    <el-dialog v-model="showSimulateModal" title="设备故障与资源压力测试模拟器" width="480px" append-to-body align-center>
      <div class="sim-intro">
        该工具允许运维工程师手动设置资产的状态与 CPU 负载，测试后台 <code>BackgroundService</code> 告警引擎的判定与实时触发能力。
      </div>
      <el-form :model="simForm" label-width="100px" style="margin-top: 16px;">
        <el-form-item label="设备运行状态">
          <el-select v-model="simForm.status" style="width: 100%">
            <el-option label="在线 (Online)" value="Online" />
            <el-option label="离线 (Offline)" value="Offline" />
            <el-option label="故障/异常 (Fault)" value="Fault" />
            <el-option label="维修中 (Maintenance)" value="Maintenance" />
          </el-select>
        </el-form-item>
        <el-form-item label="模拟 CPU 负载">
          <el-slider v-model="simForm.cpuUsage" :min="0" :max="100" show-input />
        </el-form-item>
        <el-form-item label="模拟内存占用">
          <el-slider v-model="simForm.memoryUsage" :min="0" :max="100" show-input />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showSimulateModal = false">取消</el-button>
        <el-button type="warning" @click="submitSimulate">应用模拟参数</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { getAssetList, saveAsset, deleteAsset, simulateAssetMetric } from '../../api'
import { ElMessage, ElMessageBox } from 'element-plus'

const loading = ref(false)
const tableData = ref([])
const total = ref(0)
let timer = null

const query = reactive({
  keyword: '',
  type: '',
  status: '',
  pageIndex: 1,
  pageSize: 10
})

const showEditModal = ref(false)
const form = reactive({
  id: 0,
  assetNo: '',
  name: '',
  type: 'Server',
  ipAddress: '192.168.10.100',
  port: 80,
  department: 'IT运维部',
  manager: '张管理员',
  status: 'Online',
  hardwareSpec: '16C 64GB SSD 500GB'
})

const showSimulateModal = ref(false)
const simForm = reactive({
  assetId: 0,
  status: 'Online',
  cpuUsage: 95.0,
  memoryUsage: 88.0
})

const formatType = (t) => {
  const map = { Server: '物理服务器', CloudHost: '云主机', NetworkDevice: '网络设备', Storage: '存储设备', Database: '数据库' }
  return map[t] || t
}

const formatStatus = (s) => {
  const map = { Online: '在线', Offline: '离线', Fault: '故障', Maintenance: '维修中' }
  return map[s] || s
}

const getMetricColor = (val) => {
  if (val >= 90) return '#ef4444'
  if (val >= 75) return '#f59e0b'
  return '#10b981'
}

const loadData = async (silent = false) => {
  if (!silent) loading.value = true
  try {
    const res = await getAssetList(query)
    if (res.data) {
      tableData.value = res.data.items || []
      total.value = res.data.totalCount || 0
    }
  } finally {
    if (!silent) loading.value = false
  }
}

const resetQuery = () => {
  query.keyword = ''
  query.type = ''
  query.status = ''
  query.pageIndex = 1
  loadData()
}

const openAddDialog = () => {
  Object.assign(form, {
    id: 0,
    assetNo: 'AST-SRV-' + Math.floor(100 + Math.random() * 900),
    name: '',
    type: 'Server',
    ipAddress: '192.168.10.' + Math.floor(10 + Math.random() * 200),
    port: 80,
    department: 'IT运维部',
    manager: '张管理员',
    status: 'Online',
    hardwareSpec: '16C 64GB SSD 500GB'
  })
  showEditModal.value = true
}

const openEditDialog = (row) => {
  Object.assign(form, row)
  showEditModal.value = true
}

const submitForm = async () => {
  if (!form.name || !form.ipAddress) {
    ElMessage.warning('请填写必要的资产名称与 IP 地址')
    return
  }
  const res = await saveAsset(form)
  if (res.success) {
    ElMessage.success('资产信息保存成功')
    showEditModal.value = false
    loadData()
  }
}

const handleDelete = (row) => {
  ElMessageBox.confirm(`确认移除设备资产 [${row.name}] 吗?`, '删除警告', {
    confirmButtonText: '删除',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    const res = await deleteAsset(row.id)
    if (res.success) {
      ElMessage.success('资产移除成功')
      loadData()
    }
  })
}

const openSimulateDialog = (row) => {
  simForm.assetId = row.id
  simForm.status = row.status
  simForm.cpuUsage = row.cpuUsage
  simForm.memoryUsage = row.memoryUsage
  showSimulateModal.value = true
}

const submitSimulate = async () => {
  const res = await simulateAssetMetric(simForm)
  if (res.success) {
    ElMessage.success('已为该设备应用测试模拟参数！告警引擎将在后台进行评估')
    showSimulateModal.value = false
    loadData()
  }
}

onMounted(() => {
  loadData()
  timer = setInterval(() => loadData(true), 3000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped>
.assets-page {
  padding: 20px;
}

.search-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.flex-center {
  display: flex;
  align-items: center;
}

.action-header {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  justify-content: center;
  font-weight: 600;
  color: #cbd5e1;
}

.action-btn-group {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}

.ops-act-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 9px;
  font-size: 0.75rem;
  font-weight: 500;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.22s cubic-bezier(0.4, 0, 0.2, 1);
  outline: none;
  border: none;
}

.ops-act-btn.edit-btn {
  background: rgba(59, 130, 246, 0.14);
  color: #60a5fa;
  border: 1px solid rgba(59, 130, 246, 0.3);
}
.ops-act-btn.edit-btn:hover {
  background: rgba(59, 130, 246, 0.28);
  color: #93c5fd;
  border-color: #3b82f6;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
}

.ops-act-btn.sim-btn {
  background: rgba(245, 158, 11, 0.14);
  color: #fbbf24;
  border: 1px solid rgba(245, 158, 11, 0.3);
}
.ops-act-btn.sim-btn:hover {
  background: rgba(245, 158, 11, 0.28);
  color: #fde047;
  border-color: #f59e0b;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(245, 158, 11, 0.3);
}

.ops-act-btn.del-btn {
  background: rgba(239, 68, 68, 0.14);
  color: #f87171;
  border: 1px solid rgba(239, 68, 68, 0.3);
}
.ops-act-btn.del-btn:hover {
  background: rgba(239, 68, 68, 0.28);
  color: #fca5a5;
  border-color: #ef4444;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3);
}

.gap-12 {
  gap: 12px;
}

.realtime-indicator {
  font-size: 0.78rem;
  color: #34d399;
  display: flex;
  align-items: center;
  gap: 6px;
  background: rgba(16, 185, 129, 0.1);
  padding: 4px 10px;
  border-radius: 12px;
  border: 1px solid rgba(16, 185, 129, 0.2);
}

.filter-box {
  display: flex;
  gap: 12px;
  margin-top: 16px;
  flex-wrap: wrap;
}

.asset-name-cell {
  display: flex;
  flex-direction: column;
}

.asset-name-cell .name {
  font-weight: 600;
  color: #f1f5f9;
}

.asset-name-cell .spec {
  font-size: 0.75rem;
  color: #64748b;
}

.sub-text {
  font-size: 0.75rem;
  color: #94a3b8;
}

.metrics-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.metric-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.72rem;
}

.m-label {
  width: 32px;
  color: #94a3b8;
}

.pagination-box {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.sim-intro {
  font-size: 0.85rem;
  color: #94a3b8;
  line-height: 1.5;
  background: rgba(15, 23, 42, 0.6);
  padding: 10px;
  border-radius: 6px;
  border-left: 3px solid #f59e0b;
}
</style>
