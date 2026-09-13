<template>
  <div class="permissions-page ops-glass-card">
    <!-- Header Search Bar -->
    <div class="ops-card-title search-bar">
      <div class="title-left">
        <el-icon color="#06b6d4"><Key /></el-icon>
        <span>RBAC 权限节点与 API 访问控制库</span>
        <el-tag type="info" size="small" class="ml-2">共有 {{ totalCount }} 项系统权限节点</el-tag>
      </div>
      <div class="title-right">
        <el-input
          v-model="query.keyword"
          placeholder="搜索权限编码/名称/路径..."
          prefix-icon="Search"
          clearable
          style="width: 230px; margin-right: 12px;"
          @clear="loadData"
          @keyup.enter="loadData"
        />
        <el-select
          v-model="query.method"
          placeholder="请求方法"
          clearable
          style="width: 130px; margin-right: 12px;"
          @change="loadData"
        >
          <el-option label="GET" value="GET" />
          <el-option label="POST" value="POST" />
          <el-option label="PUT" value="PUT" />
          <el-option label="DELETE" value="DELETE" />
        </el-select>
        <el-button type="primary" icon="Plus" @click="openAddDialog">新增权限节点</el-button>
      </div>
    </div>

    <!-- Permission Nodes Table -->
    <el-table :data="permList" v-loading="loading" style="width: 100%; margin-top: 16px;">
      <el-table-column prop="id" label="ID" width="70" align="center" />
      <el-table-column prop="permissionCode" label="权限编码" min-width="170">
        <template #default="scope">
          <code>{{ scope.row.permissionCode || scope.row.code }}</code>
        </template>
      </el-table-column>
      <el-table-column prop="permissionName" label="权限名称" min-width="160">
        <template #default="scope">
          <span class="perm-name-text">{{ scope.row.permissionName || scope.row.name }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="path" label="前端路由/API" min-width="180">
        <template #default="scope">
          <span class="path-text" v-if="scope.row.path">{{ scope.row.path }}</span>
          <span class="no-path" v-else>公共/全域节点</span>
        </template>
      </el-table-column>
      <el-table-column prop="method" label="HTTP 方法" width="110" align="center">
        <template #default="scope">
          <el-tag size="small" :type="getMethodTagType(scope.row.method)">
            {{ scope.row.method || 'GET' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="description" label="功能描述说明" min-width="220" show-overflow-tooltip />
      <el-table-column prop="createTime" label="创建时间" width="180">
        <template #default="scope">
          {{ formatDate(scope.row.createTime || scope.row.createdAt) }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="130" align="center" fixed="right">
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

    <!-- Pagination -->
    <div class="pagination-box">
      <el-pagination
        v-model:current-page="query.pageIndex"
        v-model:page-size="query.pageSize"
        :total="totalCount"
        layout="total, prev, pager, next"
        @current-change="loadData"
      />
    </div>

    <!-- Create / Edit Permission Dialog -->
    <el-dialog
      v-model="showModal"
      :title="form.id === 0 ? '新增权限节点' : '修改权限节点属性'"
      width="540px"
      append-to-body
      align-center
    >
      <el-form :model="form" label-width="110px">
        <el-form-item label="权限编码" required>
          <el-input
            v-model="form.permissionCode"
            placeholder="例如: asset:export 或 system:log:delete"
            :disabled="form.id !== 0"
          />
          <span class="form-tip" v-if="form.id === 0">权限编码由小写英文与冒号组成，在系统中必须唯一</span>
        </el-form-item>
        <el-form-item label="权限名称" required>
          <el-input v-model="form.permissionName" placeholder="例如: 设备台账批量导出" />
        </el-form-item>
        <el-form-item label="路由/API 路径">
          <el-input v-model="form.path" placeholder="例如: /assets/export 或 /api/log/clean" />
        </el-form-item>
        <el-form-item label="HTTP 请求方法">
          <el-radio-group v-model="form.method">
            <el-radio label="GET">GET</el-radio>
            <el-radio label="POST">POST</el-radio>
            <el-radio label="PUT">PUT</el-radio>
            <el-radio label="DELETE">DELETE</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="描述说明">
          <el-input
            v-model="form.description"
            type="textarea"
            :rows="3"
            placeholder="说明此权限节点针对的功能模块与安全访问拦截规则..."
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showModal = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">保存权限配置</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { getPermissionPageList, savePermission, deletePermission } from '../../api'
import { formatDate } from '../../utils/date'
import { ElMessage, ElMessageBox } from 'element-plus'

const loading = ref(false)
const permList = ref([])
const totalCount = ref(0)

const query = reactive({
  keyword: '',
  method: '',
  pageIndex: 1,
  pageSize: 10
})

const showModal = ref(false)
const submitLoading = ref(false)
const form = reactive({
  id: 0,
  permissionCode: '',
  permissionName: '',
  path: '',
  method: 'GET',
  description: ''
})

const loadData = async () => {
  loading.value = true
  try {
    const res = await getPermissionPageList(query)
    if (res && res.data) {
      permList.value = res.data.items || res.data.Items || (Array.isArray(res.data) ? res.data : [])
      totalCount.value = res.data.totalCount || res.data.TotalCount || permList.value.length
    } else if (res && (res.items || Array.isArray(res))) {
      permList.value = res.items || res
      totalCount.value = res.totalCount || permList.value.length
    } else {
      permList.value = []
      totalCount.value = 0
    }
  } catch (ex) {
    console.error('加载权限节点数据失败:', ex)
    permList.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

const getMethodTagType = (m) => {
  const method = (m || 'GET').toUpperCase()
  if (method === 'GET') return 'success'
  if (method === 'POST') return 'primary'
  if (method === 'PUT') return 'warning'
  if (method === 'DELETE') return 'danger'
  return 'info'
}

const openAddDialog = () => {
  Object.assign(form, {
    id: 0,
    permissionCode: '',
    permissionName: '',
    path: '',
    method: 'GET',
    description: ''
  })
  showModal.value = true
}

const openEditDialog = (row) => {
  Object.assign(form, {
    id: row.id,
    permissionCode: row.permissionCode || row.code,
    permissionName: row.permissionName || row.name,
    path: row.path || '',
    method: row.method || 'GET',
    description: row.description || ''
  })
  showModal.value = true
}

const handleSubmit = async () => {
  if (!form.permissionName) {
    ElMessage.warning('请填写权限名称')
    return
  }
  if (form.id === 0 && !form.permissionCode) {
    ElMessage.warning('请填写权限编码')
    return
  }

  submitLoading.value = true
  try {
    const res = await savePermission(form)
    if (res && res.success) {
      ElMessage.success(form.id === 0 ? '权限节点创建成功' : '权限节点修改成功')
      showModal.value = false
      await loadData()
    }
  } finally {
    submitLoading.value = false
  }
}

const handleDelete = (row) => {
  const name = row.permissionName || row.name
  ElMessageBox.confirm(`确定要删除权限节点 [${name}] 吗？删除后此节点的授权关联将被自动清除。`, '删除确认', {
    confirmButtonText: '确定删除',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    const res = await deletePermission(row.id)
    if (res && res.success) {
      ElMessage.success(res.message || '权限节点已被删除')
      await loadData()
    }
  }).catch(() => {})
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.permissions-page {
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

.perm-name-text {
  font-weight: 600;
  color: #f1f5f9;
}

.path-text {
  font-family: monospace;
  font-size: 0.85rem;
  color: #38bdf8;
}

.no-path {
  font-size: 0.8rem;
  color: #64748b;
  font-style: italic;
}

.form-tip {
  font-size: 0.75rem;
  color: #94a3b8;
  margin-top: 4px;
  display: block;
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
