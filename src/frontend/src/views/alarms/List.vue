<template>
  <div class="alarms-page ops-glass-card">
    <div class="ops-card-title">
      <el-icon color="#ef4444"><Bell /></el-icon>
      运维告警中心与工单闭环处置
    </div>

    <!-- Filter -->
    <div class="filter-box">
      <el-select v-model="query.status" placeholder="处理状态" style="width: 150px" clearable @change="loadData">
        <el-option label="待处理 (Pending)" value="Pending" />
        <el-option label="处理中 (Processing)" value="Processing" />
        <el-option label="已解决 (Resolved)" value="Resolved" />
        <el-option label="已忽略 (Ignored)" value="Ignored" />
      </el-select>

      <el-select v-model="query.severity" placeholder="告警级别" style="width: 140px" clearable @change="loadData">
        <el-option label="紧急 (Critical)" value="Critical" />
        <el-option label="重要 (Warning)" value="Warning" />
        <el-option label="一般 (Info)" value="Info" />
      </el-select>

      <el-button type="primary" plain @click="loadData">刷 新</el-button>
    </div>

    <!-- Alarm Table -->
    <el-table :data="tableData" v-loading="loading" style="width: 100%; margin-top: 16px;">
      <el-table-column prop="alarmNo" label="告警编号" width="180" />
      <el-table-column prop="assetName" label="关联故障资产" min-width="160">
        <template #default="scope">
          <div style="font-weight: 600; color: #f1f5f9;">{{ scope.row.assetName }}</div>
          <code style="font-size: 0.75rem; color: #94a3b8;">{{ scope.row.ipAddress }}</code>
        </template>
      </el-table-column>
      <el-table-column prop="content" label="告警详细描述" min-width="220" />
      <el-table-column prop="severity" label="告警级别" width="110" align="center">
        <template #default="scope">
          <el-tag :type="getSeverityTagType(scope.row.severity)" effect="dark">
            {{ scope.row.severity }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="status" label="流转状态" width="120" align="center">
        <template #default="scope">
          <span :class="['status-tag', scope.row.status.toLowerCase()]">
            {{ formatStatus(scope.row.status) }}
          </span>
        </template>
      </el-table-column>
      <el-table-column prop="triggeredAt" label="触发时间" width="160">
        <template #default="scope">
          {{ formatDate(scope.row.triggeredAt) }}
        </template>
      </el-table-column>
      <el-table-column label="处置工程师 / 结果" min-width="150">
        <template #default="scope">
          <div v-if="scope.row.handler">
            <div>{{ scope.row.handler }}</div>
            <div class="sub-text">{{ scope.row.resolutionNotes || '处理中...' }}</div>
          </div>
          <span v-else class="sub-text">未派单处置</span>
        </template>
      </el-table-column>
      <el-table-column width="140" fixed="right" align="center">
        <template #header>
          <div class="action-header">
            <el-icon color="#3b82f6"><Setting /></el-icon>
            <span>工单操作</span>
          </div>
        </template>
        <template #default="scope">
          <button
            v-if="scope.row.status === 'Pending' || scope.row.status === 'Processing'"
            class="ops-act-btn edit-btn"
            @click="openProcessDialog(scope.row)"
          >
            <el-icon><Edit /></el-icon>响应处置
          </button>
          <span v-else class="sub-text">已归档</span>
        </template>
      </el-table-column>
    </el-table>

    <!-- Pagination -->
    <div class="pagination-box">
      <el-pagination
        v-model:current-page="query.pageIndex"
        v-model:page-size="query.pageSize"
        :total="total"
        layout="total, prev, pager, next"
        @current-change="loadData"
      />
    </div>

    <!-- Process Alarm Dialog -->
    <el-dialog v-model="showProcessModal" title="告警工单处置与闭环登记" width="500px" append-to-body align-center>
      <div class="alarm-detail-box">
        <div class="detail-row"><span class="lbl">告警编号:</span> {{ currentAlarm.alarmNo }}</div>
        <div class="detail-row"><span class="lbl">故障资产:</span> {{ currentAlarm.assetName }}</div>
        <div class="detail-row"><span class="lbl">告警内容:</span> {{ currentAlarm.content }}</div>
      </div>

      <el-form :model="processForm" label-width="100px" style="margin-top: 16px;">
        <el-form-item label="更新处置状态">
          <el-radio-group v-model="processForm.status">
            <el-radio-button label="Processing">接单处理中</el-radio-button>
            <el-radio-button label="Resolved">故障彻底解决</el-radio-button>
            <el-radio-button label="Ignored">误报/忽略</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="故障处置说明/复盘记录">
          <el-input
            v-model="processForm.resolutionNotes"
            type="textarea"
            :rows="3"
            placeholder="请简要填写故障原因排查过程与修复结论..."
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showProcessModal = false">取消</el-button>
        <el-button type="primary" @click="submitProcess">提交处置结论</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { getAlarmRecords, processAlarm } from '../../api'
import { formatDate } from '../../utils/date'
import { ElMessage } from 'element-plus'

const loading = ref(false)
const tableData = ref([])
const total = ref(0)

const query = reactive({
  status: '',
  severity: '',
  pageIndex: 1,
  pageSize: 10
})

const showProcessModal = ref(false)
const currentAlarm = ref({})
const processForm = reactive({
  alarmId: 0,
  status: 'Resolved',
  resolutionNotes: ''
})

const getSeverityTagType = (sev) => {
  if (sev === 'Critical') return 'danger'
  if (sev === 'Warning') return 'warning'
  return 'info'
}

const formatStatus = (s) => {
  const map = { Pending: '待处理', Processing: '处理中', Resolved: '已解决', Ignored: '已忽略' }
  return map[s] || s
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await getAlarmRecords(query)
    if (res.data) {
      tableData.value = res.data.items || []
      total.value = res.data.totalCount || 0
    }
  } finally {
    loading.value = false
  }
}

const openProcessDialog = (row) => {
  currentAlarm.value = row
  processForm.alarmId = row.id
  processForm.status = 'Resolved'
  processForm.resolutionNotes = ''
  showProcessModal.value = true
}

const submitProcess = async () => {
  if (!processForm.resolutionNotes) {
    ElMessage.warning('请填写故障处置说明记录')
    return
  }
  const res = await processAlarm(processForm)
  if (res.success) {
    ElMessage.success('告警记录处置成功，已同步至全量日志与审计')
    showProcessModal.value = false
    loadData()
  }
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.alarms-page {
  padding: 20px;
}

.filter-box {
  display: flex;
  gap: 12px;
  margin-top: 16px;
}

.sub-text {
  font-size: 0.75rem;
  color: #64748b;
}

.pagination-box {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.alarm-detail-box {
  background: rgba(15, 23, 42, 0.6);
  padding: 12px;
  border-radius: 8px;
  font-size: 0.85rem;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.alarm-detail-box .lbl {
  color: #94a3b8;
  width: 70px;
  display: inline-block;
}
</style>
