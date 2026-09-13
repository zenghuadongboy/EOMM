<template>
  <div class="workorders-page ops-glass-card">
    <!-- Header Title & Action Bar -->
    <div class="ops-card-title search-bar">
      <div class="title-left">
        <el-icon color="#3b82f6"><Tickets /></el-icon>
        <span>运维工单全流程管理与闭环处置</span>
        <el-tag type="info" size="small" class="ml-2">共有 {{ total }} 项运维工单</el-tag>
      </div>
      <div class="title-right">
        <el-input
          v-model="queryParams.keyword"
          placeholder="搜索工单编号 / 标题 / 负责人..."
          prefix-icon="Search"
          clearable
          style="width: 240px; margin-right: 12px"
          @clear="fetchList"
          @keyup.enter="fetchList"
        />
        <el-select
          v-model="queryParams.status"
          placeholder="工单状态"
          clearable
          style="width: 140px; margin-right: 12px"
          @change="fetchList"
        >
          <el-option label="待处理 (Pending)" value="Pending" />
          <el-option label="处理中 (Processing)" value="Processing" />
          <el-option label="已完成 (Completed)" value="Completed" />
          <el-option label="已关闭 (Closed)" value="Closed" />
        </el-select>

        <el-select
          v-model="queryParams.priority"
          placeholder="优先级"
          clearable
          style="width: 130px; margin-right: 12px"
          @change="fetchList"
        >
          <el-option label="一般 (Medium)" value="Medium" />
          <el-option label="紧急 (High)" value="High" />
          <el-option label="严重 (Critical)" value="Critical" />
        </el-select>

        <el-button type="primary" icon="Plus" @click="openCreateModal">创建运维工单</el-button>
      </div>
    </div>

    <!-- Main Table -->
    <el-table :data="tableData" v-loading="loading" style="width: 100%; margin-top: 16px">
      <el-table-column prop="workOrderNo" label="工单编号" width="180">
        <template #default="{ row }">
          <code class="mono-code">{{ row.workOrderNo }}</code>
        </template>
      </el-table-column>
      <el-table-column prop="title" label="工单标题" min-width="200" show-overflow-tooltip>
        <template #default="{ row }">
          <span class="order-title-text">{{ row.title }}</span>
        </template>
      </el-table-column>
      <el-table-column label="关联故障资产" min-width="180">
        <template #default="{ row }">
          <div v-if="row.asset">
            <span class="asset-name-text">{{ row.asset.assetName || row.asset.name }}</span>
            <span class="asset-ip" v-if="row.asset.ipAddress">({{ row.asset.ipAddress }})</span>
          </div>
          <span class="no-asset" v-else>未关联资产</span>
        </template>
      </el-table-column>
      <el-table-column label="来源类型" width="120" align="center">
        <template #default="{ row }">
          <el-tag :type="row.source === 'AlarmTriggered' ? 'danger' : 'info'" size="small">
            {{ row.source === 'AlarmTriggered' ? '告警联动' : '手动派发' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="优先级" width="100" align="center">
        <template #default="{ row }">
          <el-tag :type="getPriorityTag(row.priority)" size="small">
            {{ row.priority }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="当前状态" width="120" align="center">
        <template #default="{ row }">
          <el-tag :type="getStatusTag(row.status)" effect="dark" size="small">
            {{ row.status }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="handler" label="负责人" width="110" align="center">
        <template #default="{ row }">
          <span class="handler-name">{{ row.handler || '未指派' }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="createdAt" label="派发时间" width="160">
        <template #default="{ row }">
          {{ formatDate(row.createdAt || row.createTime) }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="180" align="center" fixed="right">
        <template #default="{ row }">
          <div class="action-btn-group">
            <button class="ops-act-btn edit-btn" @click="openStatusModal(row)">
              <el-icon><Edit /></el-icon>变更状态
            </button>
            <button class="ops-act-btn sim-btn" @click="viewLogs(row)">
              <el-icon><Document /></el-icon>履历
            </button>
          </div>
        </template>
      </el-table-column>
    </el-table>

    <!-- Pagination -->
    <div class="pagination-box">
      <el-pagination
        v-model:current-page="queryParams.page"
        v-model:page-size="queryParams.pageSize"
        :total="total"
        layout="total, prev, pager, next"
        @current-change="fetchList"
      />
    </div>

    <!-- Create WorkOrder Dialog -->
    <el-dialog v-model="createModalVisible" title="新建运维处置工单" width="560px" append-to-body align-center>
      <el-form :model="form" label-width="110px">
        <el-form-item label="关联设备" required>
          <el-select v-model="form.assetId" placeholder="选择关联的IT设备" style="width: 100%">
            <el-option v-for="a in assetOptions" :key="a.id" :label="`${a.assetName || a.name} (${a.ipAddress})`" :value="a.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="工单标题" required>
          <el-input v-model="form.title" placeholder="如: Web服务器CPU过载应急排查" />
        </el-form-item>
        <el-form-item label="优先级">
          <el-radio-group v-model="form.priority">
            <el-radio label="Medium">一般</el-radio>
            <el-radio label="High">紧急</el-radio>
            <el-radio label="Critical">严重</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="负责人">
          <el-input v-model="form.handler" placeholder="请输入运维负责人账号" />
        </el-form-item>
        <el-form-item label="工单任务描述">
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="详细说明维护要求与操作步骤..." />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createModalVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSave">创建派发</el-button>
      </template>
    </el-dialog>

    <!-- Change Status Dialog -->
    <el-dialog v-model="statusModalVisible" title="工单状态流转变更" width="500px" append-to-body align-center>
      <el-form :model="statusForm" label-width="110px">
        <el-form-item label="当前工单">
          <span style="font-weight: bold; color: #38bdf8">{{ currentOrder?.workOrderNo }}</span>
        </el-form-item>
        <el-form-item label="目标状态" required>
          <el-radio-group v-model="statusForm.newStatus">
            <el-radio label="Processing">开始处理 (Processing)</el-radio>
            <el-radio label="Completed">处理完成 (Completed)</el-radio>
            <el-radio label="Closed">关闭归档 (Closed)</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="流转动作说明">
          <el-input v-model="statusForm.note" placeholder="例如: 已定位故障原因并完成服务重启" />
        </el-form-item>
        <el-form-item label="最终复盘结论" v-if="statusForm.newStatus === 'Completed' || statusForm.newStatus === 'Closed'">
          <el-input v-model="statusForm.resolutionNotes" type="textarea" :rows="3" placeholder="填写问题根因、解决措施与预防建议..." />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="statusModalVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleStatusSubmit">确认流转</el-button>
      </template>
    </el-dialog>

    <!-- WorkOrder Log Timeline Drawer -->
    <el-drawer v-model="logDrawerVisible" title="工单全流程处置履历" size="480px">
      <el-timeline v-if="logList.length > 0">
        <el-timeline-item
          v-for="log in logList"
          :key="log.id"
          :timestamp="formatDate(log.createdAt || log.createTime)"
          type="primary"
        >
          <h4 style="color: #f1f5f9;">操作人: {{ log.operatorName || log.username }}</h4>
          <p style="color: #94a3b8; margin-top: 4px">
            状态流转: <el-tag size="small">{{ log.previousStatus }}</el-tag> ➔ <el-tag size="small" type="success">{{ log.newStatus }}</el-tag>
          </p>
          <p style="margin-top: 4px; font-size: 13px; color: #cbd5e1;">{{ log.actionNote }}</p>
        </el-timeline-item>
      </el-timeline>
      <el-empty v-else description="暂无历史流转记录" />
    </el-drawer>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { Plus, Search, Edit, Document, Tickets } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import {
  getWorkOrderList,
  saveWorkOrder,
  changeWorkOrderStatus,
  getWorkOrderLogs,
  getAssetList
} from '../../api'
import { formatDate } from '../../utils/date'

const loading = ref(false)
const submitLoading = ref(false)
const tableData = ref([])
const total = ref(0)

const queryParams = reactive({
  keyword: '',
  status: '',
  priority: '',
  page: 1,
  pageSize: 10
})

const assetOptions = ref([])

const createModalVisible = ref(false)
const form = reactive({
  assetId: null,
  title: '',
  priority: 'Medium',
  handler: 'admin',
  description: ''
})

const statusModalVisible = ref(false)
const currentOrder = ref(null)
const statusForm = reactive({
  workOrderId: 0,
  newStatus: 'Processing',
  note: '',
  resolutionNotes: ''
})

const logDrawerVisible = ref(false)
const logList = ref([])

const fetchList = async () => {
  loading.value = true
  try {
    const res = await getWorkOrderList(queryParams)
    if (res && res.data) {
      tableData.value = res.data.items || []
      total.value = res.data.totalCount || res.data.total || 0
    }
  } catch (err) {
  } finally {
    loading.value = false
  }
}

const resetQuery = () => {
  queryParams.keyword = ''
  queryParams.status = ''
  queryParams.priority = ''
  queryParams.page = 1
  fetchList()
}

const fetchAssets = async () => {
  try {
    const res = await getAssetList({ pageIndex: 1, pageSize: 100 })
    if (res && res.data) {
      assetOptions.value = res.data.items || []
    }
  } catch (e) {}
}

const openCreateModal = () => {
  form.assetId = assetOptions.value.length > 0 ? assetOptions.value[0].id : null
  form.title = ''
  form.priority = 'Medium'
  form.handler = 'admin'
  form.description = ''
  createModalVisible.value = true
}

const handleSave = async () => {
  if (!form.assetId) return ElMessage.warning('请选择关联设备')
  if (!form.title) return ElMessage.warning('请输入工单标题')

  submitLoading.value = true
  try {
    const res = await saveWorkOrder(form)
    if (res && (res.code === 200 || res.success)) {
      ElMessage.success('工单派发成功')
      createModalVisible.value = false
      fetchList()
    }
  } catch (err) {
  } finally {
    submitLoading.value = false
  }
}

const openStatusModal = (row) => {
  currentOrder.value = row
  statusForm.workOrderId = row.id
  statusForm.newStatus = row.status === 'Pending' ? 'Processing' : 'Completed'
  statusForm.note = ''
  statusForm.resolutionNotes = ''
  statusModalVisible.value = true
}

const handleStatusSubmit = async () => {
  submitLoading.value = true
  try {
    const res = await changeWorkOrderStatus(statusForm)
    if (res && (res.code === 200 || res.success)) {
      ElMessage.success('状态更新成功')
      statusModalVisible.value = false
      fetchList()
    }
  } catch (err) {
  } finally {
    submitLoading.value = false
  }
}

const viewLogs = async (row) => {
  try {
    const res = await getWorkOrderLogs(row.id)
    if (res && res.data) {
      logList.value = res.data
      logDrawerVisible.value = true
    }
  } catch (e) {}
}

const getPriorityTag = (p) => {
  if (p === 'Critical' || p === 'High') return 'danger'
  return 'warning'
}

const getStatusTag = (s) => {
  switch (s) {
    case 'Pending': return 'info'
    case 'Processing': return 'primary'
    case 'Completed': return 'success'
    case 'Closed': return 'danger'
    default: return 'info'
  }
}

onMounted(() => {
  fetchList()
  fetchAssets()
})
</script>

<style scoped>
.workorders-page {
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

.mono-code {
  font-family: monospace;
  font-weight: 600;
  color: #38bdf8;
}

.order-title-text {
  font-weight: 600;
  color: #f1f5f9;
}

.asset-name-text {
  font-weight: 600;
  color: #e2e8f0;
}

.asset-ip {
  font-size: 0.8rem;
  color: #94a3b8;
  margin-left: 4px;
}

.no-asset {
  font-size: 0.8rem;
  color: #64748b;
  font-style: italic;
}

.handler-name {
  color: #e2e8f0;
  font-weight: 500;
}

.pagination-box {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.ml-2 {
  margin-left: 8px;
}
</style>
