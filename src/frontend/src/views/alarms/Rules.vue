<template>
  <div class="rules-page ops-glass-card">
    <div class="ops-card-title search-bar">
      <div class="title-left">
        <el-icon color="#f59e0b"><Operation /></el-icon>
        自动化告警引擎与策略规则配置
      </div>
      <el-button type="primary" icon="Plus" @click="openAddDialog">创建告警规则</el-button>
    </div>

    <!-- Rules Table -->
    <el-table :data="tableData" v-loading="loading" style="width: 100%; margin-top: 16px;">
      <el-table-column prop="name" label="规则策略名称" min-width="170">
        <template #default="scope">
          <div style="font-weight: 600; color: #f1f5f9;">{{ scope.row.name || scope.row.ruleName }}</div>
          <div class="sub-text">{{ scope.row.description }}</div>
        </template>
      </el-table-column>
      <el-table-column label="检测指标维度" width="160">
        <template #default="scope">
          <el-tag :type="getMetricTagType(scope.row.metric || scope.row.metricType)" effect="dark" round>
            <el-icon style="margin-right: 4px; vertical-align: middle;"><Cpu v-if="(scope.row.metric || scope.row.metricType) === 'CpuUsage'" /><Monitor v-else-if="(scope.row.metric || scope.row.metricType) === 'MemoryUsage'" /><Files v-else-if="(scope.row.metric || scope.row.metricType) === 'DiskUsage'" /><Warning v-else /></el-icon>
            {{ formatMetricName(scope.row.metric || scope.row.metricType) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="判断逻辑与阈值" width="160">
        <template #default="scope">
          <code class="ops-code-badge">指标 {{ scope.row.operator || '>' }} {{ scope.row.threshold }}{{ (scope.row.metric || scope.row.metricType) === 'Offline' ? ' (断连)' : '%' }}</code>
        </template>
      </el-table-column>
      <el-table-column prop="severity" label="触发告警级别" width="120" align="center">
        <template #default="scope">
          <el-tag :type="scope.row.severity === 'Critical' ? 'danger' : scope.row.severity === 'Warning' ? 'warning' : 'info'" effect="dark">
            {{ scope.row.severity === 'Critical' ? '紧急 Critical' : scope.row.severity === 'Warning' ? '重要 Warning' : '一般 Info' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="轮询周期" width="110">
        <template #default="scope">
          {{ scope.row.intervalSeconds || 60 }} 秒/次
        </template>
      </el-table-column>
      <el-table-column label="开启状态" width="100" align="center">
        <template #default="scope">
          <el-switch 
            :model-value="scope.row.status === 'Enable' || scope.row.isEnabled === true" 
            @change="(val) => toggleRuleStatus(scope.row, val)" 
          />
        </template>
      </el-table-column>
      <el-table-column width="160" fixed="right" align="center">
        <template #header>
          <div class="action-header">
            <el-icon color="#3b82f6"><Operation /></el-icon>
            <span>策略操作</span>
          </div>
        </template>
        <template #default="scope">
          <div class="action-btn-group">
            <button class="ops-act-btn edit-btn" @click="openEditDialog(scope.row)">
              <el-icon><Edit /></el-icon>编辑
            </button>
            <button class="ops-act-btn del-btn" @click="handleDelete(scope.row)">
              <el-icon><Delete /></el-icon>删除
            </button>
          </div>
        </template>
      </el-table-column>
    </el-table>

    <!-- Edit/Add Rule Dialog -->
    <el-dialog v-model="showModal" :title="form.id === 0 ? '创建告警策略规则' : '编辑告警策略规则'" width="520px" append-to-body align-center>
      <el-form :model="form" label-width="110px">
        <el-form-item label="策略名称">
          <el-input v-model="form.name" placeholder="例如: 内存占用过高警报" />
        </el-form-item>
        <el-form-item label="检测指标维度">
          <el-select v-model="form.metric" style="width: 100%">
            <el-option label="CPU 使用率 (CpuUsage)" value="CpuUsage" />
            <el-option label="内存占用率 (MemoryUsage)" value="MemoryUsage" />
            <el-option label="磁盘占用率 (DiskUsage)" value="DiskUsage" />
            <el-option label="设备中断/离线 (Offline)" value="Offline" />
          </el-select>
        </el-form-item>
        <el-form-item label="条件与阈值">
          <el-row :gutter="10">
            <el-col :span="8">
              <el-select v-model="form.operator">
                <el-option label="大于 (>)" value=">" />
                <el-option label="大于等于 (>=)" value=">=" />
                <el-option label="等于 (==)" value="==" />
              </el-select>
            </el-col>
            <el-col :span="16">
              <el-input-number v-model="form.threshold" :min="0" :max="100" style="width: 100%" />
            </el-col>
          </el-row>
        </el-form-item>
        <el-form-item label="告警级别">
          <el-radio-group v-model="form.severity">
            <el-radio-button label="Info">一般</el-radio-button>
            <el-radio-button label="Warning">重要</el-radio-button>
            <el-radio-button label="Critical">紧急</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="规则描述">
          <el-input v-model="form.description" type="textarea" :rows="2" placeholder="阐述该规则触发时的业务影响及操作建议" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showModal = false">取消</el-button>
        <el-button type="primary" @click="submitForm">保存规则</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { getAlarmRules, saveAlarmRule, deleteAlarmRule } from '../../api'
import { ElMessage, ElMessageBox } from 'element-plus'

const loading = ref(false)
const tableData = ref([])

const showModal = ref(false)
const form = reactive({
  id: 0,
  name: '',
  metric: 'CpuUsage',
  operator: '>',
  threshold: 85,
  severity: 'Warning',
  intervalSeconds: 60,
  status: 'Enable',
  description: ''
})

const formatMetricName = (val) => {
  if (!val) return '常规指标'
  const map = {
    CpuUsage: 'CPU 使用率',
    MemoryUsage: '内存占用率',
    DiskUsage: '磁盘空间',
    Offline: '设备离线中断'
  }
  return map[val] || val
}

const getMetricTagType = (val) => {
  const map = {
    CpuUsage: 'primary',
    MemoryUsage: 'warning',
    DiskUsage: 'info',
    Offline: 'danger'
  }
  return map[val] || 'primary'
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await getAlarmRules()
    if (res.data) {
      tableData.value = res.data
    }
  } finally {
    loading.value = false
  }
}

const openAddDialog = () => {
  Object.assign(form, {
    id: 0,
    name: '',
    metric: 'CpuUsage',
    operator: '>',
    threshold: 85,
    severity: 'Warning',
    intervalSeconds: 60,
    status: 'Enable',
    description: ''
  })
  showModal.value = true
}

const openEditDialog = (row) => {
  Object.assign(form, {
    id: row.id,
    name: row.ruleName || row.name,
    metric: row.metric || row.metricType || 'CpuUsage',
    operator: row.operator || '>',
    threshold: row.threshold ? Number(row.threshold) : 80,
    severity: row.severity || 'Warning',
    intervalSeconds: row.intervalSeconds || 60,
    status: row.status || 'Enable',
    description: row.description || ''
  })
  showModal.value = true
}

const submitForm = async () => {
  if (!form.name) {
    ElMessage.warning('请输入规则名称')
    return
  }
  const payload = {
    ...form,
    ruleName: form.name,
    threshold: String(form.threshold)
  }
  const res = await saveAlarmRule(payload)
  if (res.success) {
    ElMessage.success('告警规则配置保存成功')
    showModal.value = false
    loadData()
  }
}

const toggleRuleStatus = async (row, val) => {
  row.status = val ? 'Enable' : 'Disable'
  row.isEnabled = val
  const res = await saveAlarmRule(row)
  if (res.success) {
    ElMessage.success(`告警规则 [${row.ruleName || row.name}] 已${val ? '启用' : '禁用'}`)
  }
}

const handleDelete = (row) => {
  ElMessageBox.confirm(`确认移除告警规则 [${row.name}] 吗?`, '警告', {
    confirmButtonText: '确定删除',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    const res = await deleteAlarmRule(row.id)
    if (res.success) {
      ElMessage.success('规则删除成功')
      loadData()
    }
  })
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.rules-page {
  padding: 20px;
}

.search-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.sub-text {
  font-size: 0.75rem;
  color: #64748b;
}
</style>
