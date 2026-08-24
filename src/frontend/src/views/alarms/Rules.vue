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
      <el-table-column prop="name" label="规则策略名称" min-width="160">
        <template #default="scope">
          <div style="font-weight: 600; color: #f1f5f9;">{{ scope.row.name }}</div>
          <div class="sub-text">{{ scope.row.description }}</div>
        </template>
      </el-table-column>
      <el-table-column prop="metricType" label="检测指标维度" width="140">
        <template #default="scope">
          <el-tag type="info">{{ scope.row.metricType }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="判断逻辑与阈值" width="160">
        <template #default="scope">
          <code>指标 {{ scope.row.operator }} {{ scope.row.threshold }}</code>
        </template>
      </el-table-column>
      <el-table-column prop="severity" label="触发告警级别" width="120" align="center">
        <template #default="scope">
          <el-tag :type="scope.row.severity === 'Critical' ? 'danger' : 'warning'" effect="dark">
            {{ scope.row.severity }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="轮询周期" width="110">
        <template #default="scope">
          {{ scope.row.intervalSeconds }} 秒/次
        </template>
      </el-table-column>
      <el-table-column label="开启状态" width="100" align="center">
        <template #default="scope">
          <el-switch v-model="scope.row.isEnabled" @change="toggleRuleStatus(scope.row)" />
        </template>
      </el-table-column>
      <el-table-column label="操作" width="140" fixed="right">
        <template #default="scope">
          <el-button link type="primary" size="small" @click="openEditDialog(scope.row)">编辑</el-button>
          <el-button link type="danger" size="small" @click="handleDelete(scope.row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- Edit/Add Rule Dialog -->
    <el-dialog v-model="showModal" :title="form.id === 0 ? '创建告警策略规则' : '编辑告警策略规则'" width="520px">
      <el-form :model="form" label-width="110px">
        <el-form-item label="策略名称">
          <el-input v-model="form.name" placeholder="例如: 内存占用过高警报" />
        </el-form-item>
        <el-form-item label="检测指标维度">
          <el-select v-model="form.metricType" style="width: 100%">
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
  metricType: 'CpuUsage',
  operator: '>',
  threshold: 85.0,
  severity: 'Warning',
  intervalSeconds: 60,
  isEnabled: true,
  description: ''
})

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
    metricType: 'CpuUsage',
    operator: '>',
    threshold: 85.0,
    severity: 'Warning',
    intervalSeconds: 60,
    isEnabled: true,
    description: ''
  })
  showModal.value = true
}

const openEditDialog = (row) => {
  Object.assign(form, row)
  showModal.value = true
}

const submitForm = async () => {
  if (!form.name) {
    ElMessage.warning('请输入规则名称')
    return
  }
  const res = await saveAlarmRule(form)
  if (res.success) {
    ElMessage.success('告警规则配置保存成功')
    showModal.value = false
    loadData()
  }
}

const toggleRuleStatus = async (row) => {
  const res = await saveAlarmRule(row)
  if (res.success) {
    ElMessage.success(`告警规则 [${row.name}] 已${row.isEnabled ? '启用' : '禁用'}`)
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
