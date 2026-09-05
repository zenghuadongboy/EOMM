<template>
  <div class="workorders-container">
    <!-- 头部卡片查询区 -->
    <el-card class="search-card" shadow="never">
      <div class="search-form">
        <el-input
          v-model="queryParams.keyword"
          placeholder="搜索工单编号 / 标题 / 负责人..."
          clearable
          style="width: 260px"
          @keyup.enter="fetchList"
        />
        <el-select v-model="queryParams.status" placeholder="工单状态" clearable style="width: 140px; margin-left: 12px">
          <el-option label="待处理 (Pending)" value="Pending" />
          <el-option label="处理中 (Processing)" value="Processing" />
          <el-option label="已完成 (Completed)" value="Completed" />
          <el-option label="已关闭 (Closed)" value="Closed" />
        </el-select>

        <el-select v-model="queryParams.priority" placeholder="优先级" clearable style="width: 140px; margin-left: 12px">
          <el-option label="一般 (Medium)" value="Medium" />
          <el-option label="紧急 (High)" value="High" />
          <el-option label="严重 (Critical)" value="Critical" />
        </el-select>

        <el-button type="primary" style="margin-left: 16px" @click="fetchList">查询</el-button>
        <el-button @click="resetQuery">重置</el-button>
        <el-button type="success" style="margin-left: auto" @click="openCreateModal">
          <el-icon><Plus /></el-icon> 创建运维工单
        </el-button>
      </div>
    </el-card>

    <!-- 主表格卡片 -->
    <el-card class="table-card" shadow="never" style="margin-top: 16px">
      <el-table :data="tableData" v-loading="loading" border stripe style="width: 100%">
        <el-table-column prop="workOrderNo" label="工单编号" width="180">
          <template #default="{ row }">
            <span class="mono-code">{{ row.workOrderNo }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="title" label="工单标题" min-width="220" show-overflow-tooltip />
        <el-table-column label="关联资产" width="180">
          <template #default="{ row }">
            <span>{{ row.asset ? `${row.asset.name} (${row.asset.ipAddress})` : '未关联' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="来源类型" width="130">
          <template #default="{ row }">
            <el-tag :type="row.source === 'AlarmTriggered' ? 'danger' : 'info'" size="small">
              {{ row.source === 'AlarmTriggered' ? '告警联动' : '手动派发' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="优先级" width="110">
          <template #default="{ row }">
            <el-tag :type="getPriorityTag(row.priority)" size="small">
              {{ row.priority }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="当前状态" width="120">
          <template #default="{ row }">
            <el-tag :type="getStatusTag(row.status)" effect="dark" size="small">
              {{ row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="handler" label="负责人" width="110" />
        <el-table-column prop="createdAt" label="派发时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="openStatusModal(row)">
              变更状态
            </el-button>
            <el-button link type="info" size="small" @click="viewLogs(row)">
              流转履历
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-wrapper" style="margin-top: 16px; text-align: right">
        <el-pagination
          v-model:current-page="queryParams.page"
          v-model:page-size="queryParams.pageSize"
          :total="total"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="fetchList"
          @current-change="fetchList"
        />
      </div>
    </el-card>

    <!-- 新建/编辑对话框 -->
    <el-dialog v-model="createModalVisible" title="新建运维处置工单" width="560px">
      <el-form :model="form" label-width="110px">
        <el-form-item label="关联设备" required>
          <el-select v-model="form.assetId" placeholder="选择关联的IT设备" style="width: 100%">
            <el-option v-for="a in assetOptions" :key="a.id" :label="`${a.name} (${a.ipAddress})`" :value="a.id" />
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

    <!-- 状态变更对话框 -->
    <el-dialog v-model="statusModalVisible" title="工单状态流转变更" width="500px">
      <el-form :model="statusForm" label-width="110px">
        <el-form-item label="当前工单">
          <span style="font-weight: bold">{{ currentOrder?.workOrderNo }}</span>
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

    <!-- 履历抽屉 -->
    <el-drawer v-model="logDrawerVisible" title="工单全流程处置履历" size="480px">
      <el-timeline v-if="logList.length > 0">
        <el-timeline-item
          v-for="log in logList"
          :key="log.id"
          :timestamp="formatDate(log.createdAt)"
          type="primary"
        >
          <h4>操作人: {{ log.operatorName }}</h4>
          <p style="color: #606266; margin-top: 4px">
            状态流转: <el-tag size="small">{{ log.previousStatus }}</el-tag> ➔ <el-tag size="small" type="success">{{ log.newStatus }}</el-tag>
          </p>
          <p style="margin-top: 4px; font-size: 13px">{{ log.actionNote }}</p>
        </el-timeline-item>
      </el-timeline>
      <el-empty v-else description="暂无历史流转记录" />
    </el-drawer>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import axios from 'axios'

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
    const res = await axios.get('/api/workorder/list', { params: queryParams })
    if (res.data.code === 200) {
      tableData.value = res.data.data.items
      total.value = res.data.data.total
    }
  } catch (err) {
    ElMessage.error('获取工单列表失败')
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
    const res = await axios.get('/api/asset/list', { params: { page: 1, pageSize: 100 } })
    if (res.data.code === 200) {
      assetOptions.value = res.data.data.items
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
    const res = await axios.post('/api/workorder/save', form)
    if (res.data.code === 200) {
      ElMessage.success('工单派发成功')
      createModalVisible.value = false
      fetchList()
    }
  } catch (err) {
    ElMessage.error('工单保存失败')
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
    const res = await axios.post('/api/workorder/change-status', statusForm)
    if (res.data.code === 200) {
      ElMessage.success('状态更新成功')
      statusModalVisible.value = false
      fetchList()
    }
  } catch (err) {
    ElMessage.error('状态流转失败')
  } finally {
    submitLoading.value = false
  }
}

const viewLogs = async (row) => {
  try {
    const res = await axios.get(`/api/workorder/logs/${row.id}`)
    if (res.data.code === 200) {
      logList.value = res.data.data
      logDrawerVisible.value = true
    }
  } catch (e) {
    ElMessage.error('获取履历日志失败')
  }
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

const formatDate = (str) => {
  if (!str) return '-'
  return new Date(str).toLocaleString('zh-CN', { hour12: false })
}

onMounted(() => {
  fetchList()
  fetchAssets()
})
</script>

<style scoped>
.workorders-container {
  padding: 16px;
}
.search-form {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
}
.mono-code {
  font-family: 'Fira Code', Consolas, monospace;
  font-weight: 600;
  color: #409eff;
}
</style>
