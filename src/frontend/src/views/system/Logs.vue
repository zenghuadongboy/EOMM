<template>
  <div class="logs-page ops-glass-card">
    <el-tabs v-model="activeTab">
      <!-- Tab 1: Operation Audit Logs -->
      <el-tab-pane label="全量用户操作行为审计日志" name="operation">
        <div class="filter-box">
          <el-input
            v-model="opQuery.username"
            placeholder="搜索操作人用户名"
            style="width: 200px"
            clearable
            @clear="loadOpLogs"
            @keyup.enter="loadOpLogs"
          />
          <el-select v-model="opQuery.module" placeholder="业务模块" style="width: 150px" clearable @change="loadOpLogs">
            <el-option label="身份认证" value="身份认证" />
            <el-option label="资产管理" value="资产管理" />
            <el-option label="资产监控" value="资产监控" />
            <el-option label="告警管理" value="告警管理" />
            <el-option label="告警策略" value="告警策略" />
            <el-option label="用户管理" value="用户管理" />
          </el-select>
          <el-button type="primary" plain @click="loadOpLogs">筛选刷新</el-button>
        </div>

        <el-table :data="opLogs" v-loading="opLoading" style="width: 100%; margin-top: 16px;">
          <el-table-column prop="createdAt" label="操作时间" width="160">
            <template #default="scope">
              {{ formatDate(scope.row.createdAt) }}
            </template>
          </el-table-column>
          <el-table-column prop="username" label="操作账号" width="130">
            <template #default="scope">
              <code>{{ scope.row.username }}</code>
            </template>
          </el-table-column>
          <el-table-column prop="module" label="所属模块" width="120" />
          <el-table-column prop="action" label="操作动作" width="120">
            <template #default="scope">
              <el-tag size="small" effect="plain">{{ scope.row.action }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="description" label="详细描述" min-width="220" />
          <el-table-column prop="ipAddress" label="客户端 IP" width="130" />
          <el-table-column prop="executionTimeMs" label="耗时" width="90" align="center">
            <template #default="scope">
              <span>{{ scope.row.executionTimeMs }} ms</span>
            </template>
          </el-table-column>
          <el-table-column label="结果" width="90" align="center">
            <template #default="scope">
              <el-tag :type="scope.row.success ? 'success' : 'danger'" size="small">
                {{ scope.row.success ? '成功' : '失败' }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>

        <div class="pagination-box">
          <el-pagination
            v-model:current-page="opQuery.pageIndex"
            v-model:page-size="opQuery.pageSize"
            :total="opTotal"
            layout="total, prev, pager, next"
            @current-change="loadOpLogs"
          />
        </div>
      </el-tab-pane>

      <!-- Tab 2: System Internal Logs -->
      <el-tab-pane label="后台服务与运行日志" name="system">
        <div class="filter-box">
          <el-select v-model="sysQuery.level" placeholder="日志级别" style="width: 140px" clearable @change="loadSysLogs">
            <el-option label="Info (信息)" value="Info" />
            <el-option label="Warn (警告)" value="Warn" />
            <el-option label="Error (异常)" value="Error" />
          </el-select>
          <el-button type="primary" plain @click="loadSysLogs">刷新日志</el-button>
        </div>

        <el-table :data="sysLogs" v-loading="sysLoading" style="width: 100%; margin-top: 16px;">
          <el-table-column prop="createdAt" label="生成时间" width="160">
            <template #default="scope">
              {{ formatDate(scope.row.createdAt) }}
            </template>
          </el-table-column>
          <el-table-column prop="level" label="日志级别" width="110" align="center">
            <template #default="scope">
              <el-tag :type="scope.row.level === 'Error' ? 'danger' : scope.row.level === 'Warn' ? 'warning' : 'info'">
                {{ scope.row.level }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="source" label="日志来源服务" width="220" />
          <el-table-column prop="message" label="系统日志消息" min-width="300" />
        </el-table>

        <div class="pagination-box">
          <el-pagination
            v-model:current-page="sysQuery.pageIndex"
            v-model:page-size="sysQuery.pageSize"
            :total="sysTotal"
            layout="total, prev, pager, next"
            @current-change="loadSysLogs"
          />
        </div>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { getOperationLogs, getSystemLogs } from '../../api'
import { formatDate } from '../../utils/date'

const activeTab = ref('operation')

// Operation Logs State
const opLoading = ref(false)
const opLogs = ref([])
const opTotal = ref(0)
const opQuery = reactive({ username: '', module: '', pageIndex: 1, pageSize: 10 })

// System Logs State
const sysLoading = ref(false)
const sysLogs = ref([])
const sysTotal = ref(0)
const sysQuery = reactive({ level: '', pageIndex: 1, pageSize: 10 })

const loadOpLogs = async () => {
  opLoading.value = true
  try {
    const res = await getOperationLogs(opQuery)
    if (res.data) {
      opLogs.value = res.data.items || []
      opTotal.value = res.data.totalCount || 0
    }
  } finally {
    opLoading.value = false
  }
}

const loadSysLogs = async () => {
  sysLoading.value = true
  try {
    const res = await getSystemLogs(sysQuery)
    if (res.data) {
      sysLogs.value = res.data.items || []
      sysTotal.value = res.data.totalCount || 0
    }
  } finally {
    sysLoading.value = false
  }
}

onMounted(() => {
  loadOpLogs()
  loadSysLogs()
})
</script>

<style scoped>
.logs-page {
  padding: 20px;
}

.filter-box {
  display: flex;
  gap: 12px;
  margin-top: 10px;
}

.pagination-box {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
