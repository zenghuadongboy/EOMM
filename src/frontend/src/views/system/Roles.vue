<template>
  <div class="roles-page ops-glass-card">
    <div class="ops-card-title search-bar">
      <div class="title-left">
        <el-icon color="#10b981"><Lock /></el-icon>
        RBAC 角色访问控制与权限矩阵
      </div>
      <el-button type="primary" icon="Plus" @click="openAddDialog">创建自定义角色</el-button>
    </div>

    <!-- Role List Cards -->
    <el-row :gutter="16" style="margin-top: 16px;">
      <el-col :span="8" v-for="r in roleList" :key="r.id">
        <div class="role-card ops-glass-card">
          <div class="role-header">
            <div class="role-title">
              <span class="name">{{ r.name }}</span>
              <code>{{ r.code }}</code>
            </div>
            <button class="ops-act-btn primary-btn" @click="openPermissionModal(r)">
              <el-icon><Key /></el-icon>授权矩阵
            </button>
          </div>

          <p class="role-desc">{{ r.description }}</p>

          <div class="perm-stats">
            包含权限节点: <strong>{{ r.permissionIds ? r.permissionIds.length : 0 }}</strong> 项
          </div>
        </div>
      </el-col>
    </el-row>

    <!-- Permission Assignment Modal -->
    <el-dialog v-model="showPermModal" :title="`为角色 [${currentRole.name}] 分配权限`" width="450px">
      <div class="perm-tree-box">
        <el-checkbox-group v-model="selectedPermIds">
          <div v-for="p in allPermissions" :key="p.id" class="perm-item">
            <el-checkbox :value="p.id">
              <span class="p-name">{{ p.name }}</span>
              <code class="p-code">({{ p.code }})</code>
            </el-checkbox>
          </div>
        </el-checkbox-group>
      </div>
      <template #footer>
        <el-button @click="showPermModal = false">取消</el-button>
        <el-button type="primary" @click="submitAssign">提交权限矩阵修改</el-button>
      </template>
    </el-dialog>

    <!-- Add Role Modal -->
    <el-dialog v-model="showAddModal" title="创建角色" width="420px">
      <el-form :model="roleForm" label-width="90px">
        <el-form-item label="角色标识">
          <el-input v-model="roleForm.code" placeholder="例如: NetworkAdmin" />
        </el-form-item>
        <el-form-item label="角色名称">
          <el-input v-model="roleForm.name" placeholder="例如: 网络运维组长" />
        </el-form-item>
        <el-form-item label="描述说明">
          <el-input v-model="roleForm.description" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAddModal = false">取消</el-button>
        <el-button type="primary" @click="submitAddRole">创建保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { getRoleList, getPermissionList, saveRole, assignRolePermissions } from '../../api'
import { ElMessage } from 'element-plus'

const roleList = ref([])
const allPermissions = ref([])

const showPermModal = ref(false)
const currentRole = ref({})
const selectedPermIds = ref([])

const showAddModal = ref(false)
const roleForm = reactive({ id: 0, code: '', name: '', description: '' })

const loadData = async () => {
  const rRes = await getRoleList()
  if (rRes.data) roleList.value = rRes.data
  const pRes = await getPermissionList()
  if (pRes.data) allPermissions.value = pRes.data
}

const openPermissionModal = (role) => {
  currentRole.value = role
  selectedPermIds.value = [...(role.permissionIds || [])]
  showPermModal.value = true
}

const submitAssign = async () => {
  const res = await assignRolePermissions({
    roleId: currentRole.value.id,
    permissionIds: selectedPermIds.value
  })
  if (res.success) {
    ElMessage.success('角色权限配置已更新成功')
    showPermModal.value = false
    loadData()
  }
}

const openAddDialog = () => {
  Object.assign(roleForm, { id: 0, code: '', name: '', description: '' })
  showAddModal.value = true
}

const submitAddRole = async () => {
  if (!roleForm.code || !roleForm.name) {
    ElMessage.warning('请填写角色标识与角色名称')
    return
  }
  const res = await saveRole(roleForm)
  if (res.success) {
    ElMessage.success('角色创建成功')
    showAddModal.value = false
    loadData()
  }
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.roles-page {
  padding: 20px;
}

.search-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.role-card {
  padding: 16px;
  margin-bottom: 16px;
}

.role-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.role-title {
  display: flex;
  flex-direction: column;
}

.role-title .name {
  font-size: 1.1rem;
  font-weight: 700;
  color: #f1f5f9;
}

.role-title code {
  font-size: 0.78rem;
  color: #94a3b8;
}

.role-desc {
  font-size: 0.82rem;
  color: #94a3b8;
  margin-top: 10px;
  min-height: 38px;
}

.perm-stats {
  margin-top: 12px;
  padding-top: 10px;
  border-top: 1px solid #334155;
  font-size: 0.8rem;
  color: #64748b;
}

.perm-tree-box {
  max-height: 360px;
  overflow-y: auto;
  padding: 8px;
}

.perm-item {
  padding: 6px 0;
  border-bottom: 1px dashed #334155;
}

.p-name {
  font-weight: 500;
  color: #f8fafc;
}

.p-code {
  font-size: 0.75rem;
  color: #94a3b8;
  margin-left: 6px;
}
</style>
