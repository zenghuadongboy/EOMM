<template>
  <div class="users-page ops-glass-card">
    <div class="ops-card-title search-bar">
      <div class="title-left">
        <el-icon color="#3b82f6"><User /></el-icon>
        系统用户与运维账号管理
      </div>
      <el-button type="primary" icon="Plus" @click="openAddDialog">创建新账号</el-button>
    </div>

    <!-- User Table -->
    <el-table :data="tableData" v-loading="loading" style="width: 100%; margin-top: 16px;">
      <el-table-column prop="username" label="登录用户名" width="140">
        <template #default="scope">
          <code>{{ scope.row.username }}</code>
        </template>
      </el-table-column>
      <el-table-column prop="realName" label="真实姓名" width="130" />
      <el-table-column prop="roleName" label="分配角色" width="140">
        <template #default="scope">
          <el-tag type="success" effect="plain">{{ scope.row.roleName }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="department" label="所属部门" width="140" />
      <el-table-column prop="email" label="联系邮箱 / 手机" min-width="180">
        <template #default="scope">
          <div>{{ scope.row.email }}</div>
          <div class="sub-text">{{ scope.row.phone }}</div>
        </template>
      </el-table-column>
      <el-table-column label="账号状态" width="100" align="center">
        <template #default="scope">
          <el-switch v-model="scope.row.isEnabled" @change="handleToggleStatus(scope.row)" />
        </template>
      </el-table-column>
      <el-table-column prop="lastLoginAt" label="最后登录时间" width="160">
        <template #default="scope">
          {{ formatDate(scope.row.lastLoginAt) }}
        </template>
      </el-table-column>
      <el-table-column width="190" fixed="right" align="center">
        <template #header>
          <div class="action-header">
            <el-icon color="#3b82f6"><User /></el-icon>
            <span>账号操作</span>
          </div>
        </template>
        <template #default="scope">
          <div class="action-btn-group">
            <button class="ops-act-btn edit-btn" @click="openEditDialog(scope.row)">
              <el-icon><Edit /></el-icon>编辑
            </button>
            <button class="ops-act-btn warn-btn" @click="handleResetPwd(scope.row)">
              <el-icon><RefreshRight /></el-icon>重置
            </button>
          </div>
        </template>
      </el-table-column>
    </el-table>

    <!-- Dialog -->
    <el-dialog v-model="showModal" :title="form.id === 0 ? '创建新运维账号' : '修改账号信息'" width="480px" append-to-body align-center>
      <el-form :model="form" label-width="90px">
        <el-form-item label="登录用户名">
          <el-input v-model="form.username" :disabled="form.id !== 0" placeholder="例如: zhang_ops" />
        </el-form-item>
        <el-form-item label="真实姓名">
          <el-input v-model="form.realName" placeholder="例如: 张运维" />
        </el-form-item>
        <el-form-item label="角色授权">
          <el-select v-model="form.roleId" style="width: 100%">
            <el-option v-for="r in roleOptions" :key="r.id" :label="r.name" :value="r.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="所属部门">
          <el-input v-model="form.department" placeholder="例如: 基础架构组" />
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="form.email" placeholder="user@company.com" />
        </el-form-item>
        <el-form-item label="手机号">
          <el-input v-model="form.phone" placeholder="13800000000" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showModal = false">取消</el-button>
        <el-button type="primary" @click="submitForm">保存账号</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { getUserList, saveUser, toggleUserStatus, resetUserPassword, getRoleList } from '../../api'
import { formatDate } from '../../utils/date'
import { ElMessage, ElMessageBox } from 'element-plus'

const loading = ref(false)
const tableData = ref([])
const roleOptions = ref([])

const showModal = ref(false)
const form = reactive({
  id: 0,
  username: '',
  realName: '',
  roleId: 1,
  department: 'IT运维部',
  email: '',
  phone: '',
  isEnabled: true
})

const loadData = async () => {
  loading.value = true
  try {
    const res = await getUserList()
    if (res.data) tableData.value = res.data
    const rRes = await getRoleList()
    if (rRes.data) roleOptions.value = rRes.data
  } finally {
    loading.value = false
  }
}

const openAddDialog = () => {
  Object.assign(form, {
    id: 0,
    username: '',
    realName: '',
    roleId: roleOptions.value[0]?.id || 1,
    department: 'IT运维部',
    email: '',
    phone: '',
    isEnabled: true
  })
  showModal.value = true
}

const openEditDialog = (row) => {
  Object.assign(form, row)
  showModal.value = true
}

const submitForm = async () => {
  if (!form.username || !form.realName) {
    ElMessage.warning('请填写必要的用户名与真实姓名')
    return
  }
  const res = await saveUser(form)
  if (res.success) {
    ElMessage.success('账号信息保存成功 (新账号初始密码为 123456)')
    showModal.value = false
    loadData()
  }
}

const handleToggleStatus = async (row) => {
  const res = await toggleUserStatus(row.id)
  if (res.success) {
    ElMessage.success(res.message)
  }
}

const handleResetPwd = (row) => {
  ElMessageBox.confirm(`确定重置账号 [${row.username}] 的密码为默认值 123456 吗?`, '重置确认', {
    type: 'warning'
  }).then(async () => {
    const res = await resetUserPassword(row.id)
    if (res.success) {
      ElMessage.success(res.message)
    }
  })
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.users-page {
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
