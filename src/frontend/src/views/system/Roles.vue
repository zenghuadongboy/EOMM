<template>
  <div class="roles-page ops-glass-card">
    <div class="ops-card-title search-bar">
      <div class="title-left">
        <el-icon color="#10b981"><Lock /></el-icon>
        <span>RBAC 角色访问控制与权限分配中心</span>
        <el-tag type="info" size="small" class="ml-2">共有 {{ roleList.length }} 个角色</el-tag>
      </div>
      <div class="title-right">
        <el-input
          v-model="searchKeyword"
          placeholder="搜索角色名称或标识..."
          prefix-icon="Search"
          clearable
          style="width: 220px; margin-right: 12px;"
        />
        <el-button type="primary" icon="Plus" @click="openAddDialog">创建自定义角色</el-button>
      </div>
    </div>

    <!-- Role List Cards Grid -->
    <el-row :gutter="16" style="margin-top: 16px;">
      <el-col :xs="24" :sm="12" :md="8" v-for="r in filteredRoleList" :key="r.id">
        <div class="role-card ops-glass-card">
          <div class="role-header">
            <div class="role-title">
              <div class="name-row">
                <span class="name">{{ r.name || r.roleName }}</span>
                <el-tag size="small" :type="getRoleTagType(r.code || r.roleCode)" class="ml-2">
                  {{ r.code || r.roleCode }}
                </el-tag>
              </div>
              <span class="create-time">创建于: {{ formatDate(r.createdAt || r.createTime) }}</span>
            </div>
            <div class="action-btns">
              <el-tooltip content="编辑角色与权限" placement="top">
                <el-button type="primary" circle icon="Edit" size="small" @click="openEditDialog(r)" />
              </el-tooltip>
              <el-tooltip content="配置独立授权矩阵" placement="top">
                <el-button type="success" circle icon="Key" size="small" @click="openPermissionModal(r)" />
              </el-tooltip>
              <el-tooltip content="删除角色" placement="top" v-if="(r.code || r.roleCode) !== 'Admin'">
                <el-button type="danger" circle icon="Delete" size="small" @click="handleDeleteRole(r)" />
              </el-tooltip>
            </div>
          </div>

          <p class="role-desc">{{ r.description || '暂无描述信息' }}</p>

          <!-- Current Assigned Permissions Preview -->
          <div class="perm-preview-section">
            <div class="perm-preview-header">
              <span>已分配权限 ({{ getRolePermissions(r).length }}/{{ allPermissions.length }})</span>
              <el-button text type="primary" size="small" @click="openDetailDrawer(r)">查看详情</el-button>
            </div>
            <div class="perm-tags-wrapper">
              <el-tag
                v-for="p in getRolePermissions(r).slice(0, 4)"
                :key="p.id"
                size="small"
                effect="plain"
                class="perm-tag"
              >
                {{ p.permissionName || p.name }}
              </el-tag>

              <el-tag v-if="getRolePermissions(r).length > 4" size="small" type="info" effect="plain" class="perm-tag">
                +{{ getRolePermissions(r).length - 4 }} 项...
              </el-tag>
              
              <span v-if="getRolePermissions(r).length === 0" class="no-perm-tip">暂无分配权限节点</span>
            </div>
          </div>
        </div>
      </el-col>
    </el-row>

    <!-- Create / Edit Role Modal (with Integrated Permission Selection) -->
    <el-dialog
      v-model="showSaveModal"
      :title="roleForm.id === 0 ? '创建角色并分配权限' : `编辑角色 - [${roleForm.name}]`"
      width="620px"
      append-to-body
      align-center
      destroy-on-close
    >
      <el-form :model="roleForm" label-width="90px">
        <el-form-item label="角色标识" required>
          <el-input
            v-model="roleForm.code"
            placeholder="例如: NetworkAdmin"
            :disabled="roleForm.id !== 0"
          />
          <span class="form-tip" v-if="roleForm.id === 0">角色标识必须唯一且创建后不可修改</span>
        </el-form-item>
        <el-form-item label="角色名称" required>
          <el-input v-model="roleForm.name" placeholder="例如: 网络运维主管" />
        </el-form-item>
        <el-form-item label="描述说明">
          <el-input v-model="roleForm.description" type="textarea" :rows="2" placeholder="请输入角色职责与管理范围说明..." />
        </el-form-item>

        <!-- Integrated Permission Selection Section -->
        <el-form-item label="分配权限">
          <div class="perm-selection-container">
            <div class="perm-selection-header">
              <span class="select-count-info">已选 {{ (roleForm.permissionIds || []).length }} / {{ allPermissions.length }} 项权限</span>
              <div>
                <el-button size="small" @click="selectAllPermissions">全选</el-button>
                <el-button size="small" @click="clearAllPermissions">清空</el-button>
              </div>
            </div>
            <div class="perm-checkbox-scroll">
              <el-checkbox-group v-model="roleForm.permissionIds">
                <div v-for="p in allPermissions" :key="p.id" class="perm-checkbox-row">
                  <el-checkbox :label="p.id" :value="p.id">
                    <span class="p-title">{{ p.permissionName || p.name }}</span>
                    <span class="p-code-badge">[{{ p.permissionCode || p.code }}]</span>
                    <el-tag size="small" type="info" class="ml-2" v-if="p.path">{{ p.path }}</el-tag>
                  </el-checkbox>
                </div>
              </el-checkbox-group>
            </div>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showSaveModal = false">取消</el-button>
        <el-button type="primary" :loading="saveLoading" @click="submitSaveRole">保存角色配置</el-button>
      </template>
    </el-dialog>

    <!-- Quick Permission Matrix Modal -->
    <el-dialog v-model="showPermModal" :title="`角色 [${currentRole.name || currentRole.roleName}] 快速授权矩阵`" width="500px" append-to-body align-center>
      <div class="perm-tree-box">
        <div class="perm-selection-header mb-2">
          <span>授权功能节点勾选</span>
          <div>
            <el-button size="small" @click="selectedPermIds = allPermissions.map(p => p.id)">全选</el-button>
            <el-button size="small" @click="selectedPermIds = []">清空</el-button>
          </div>
        </div>
        <el-checkbox-group v-model="selectedPermIds">
          <div v-for="p in allPermissions" :key="p.id" class="perm-item">
            <el-checkbox :label="p.id" :value="p.id">
              <span class="p-name">{{ p.permissionName || p.name }}</span>
              <code class="p-code">({{ p.permissionCode || p.code }})</code>
            </el-checkbox>
          </div>
        </el-checkbox-group>
      </div>
      <template #footer>
        <el-button @click="showPermModal = false">取消</el-button>
        <el-button type="primary" @click="submitAssign">保存授权修改</el-button>
      </template>
    </el-dialog>

    <!-- Role Permission Details Query Drawer -->
    <el-drawer
      v-model="showDrawer"
      :title="`角色 [${drawerRole ? (drawerRole.name || drawerRole.roleName) : ''}] 权限详细清单`"
      size="420px"
    >
      <div v-if="drawerRole" class="drawer-content">
        <div class="drawer-role-info ops-glass-card mb-4">
          <div><strong>角色编码：</strong><code>{{ drawerRole.code || drawerRole.roleCode }}</code></div>
          <div><strong>描述说明：</strong>{{ drawerRole.description || '无' }}</div>
          <div><strong>关联权限数量：</strong><span class="highlight-count">{{ getRolePermissions(drawerRole).length }}</span> 项</div>
        </div>

        <el-input
          v-model="drawerKeyword"
          placeholder="检索包含的权限名称或代码..."
          prefix-icon="Search"
          clearable
          class="mb-3"
        />

        <div class="drawer-perm-list">
          <div
            v-for="p in filteredDrawerPermissions"
            :key="p.id"
            class="drawer-perm-item"
          >
            <div class="item-top">
              <span class="item-name">{{ p.permissionName || p.name }}</span>
              <el-tag size="small" type="success">{{ p.method || 'GET' }}</el-tag>
            </div>
            <div class="item-bottom">
              <code>Code: {{ p.permissionCode || p.code }}</code>
              <span class="item-path" v-if="p.path">Path: {{ p.path }}</span>
            </div>
          </div>

          <el-empty v-if="filteredDrawerPermissions.length === 0" description="暂无匹配的权限节点" />
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { getRoleList, getPermissionList, saveRole, assignRolePermissions } from '../../api'
import { ElMessage, ElMessageBox } from 'element-plus'
import request from '../../api/request'

const roleList = ref([])
const allPermissions = ref([])
const searchKeyword = ref('')

const showSaveModal = ref(false)
const saveLoading = ref(false)
const roleForm = reactive({
  id: 0,
  code: '',
  name: '',
  description: '',
  permissionIds: []
})

const showPermModal = ref(false)
const currentRole = ref({})
const selectedPermIds = ref([])

const showDrawer = ref(false)
const drawerRole = ref(null)
const drawerKeyword = ref('')

const filteredRoleList = computed(() => {
  if (!Array.isArray(roleList.value)) return []
  if (!searchKeyword.value) return roleList.value
  const kw = searchKeyword.value.toLowerCase()
  return roleList.value.filter(r => {
    const rName = (r.name || r.roleName || '').toLowerCase()
    const rCode = (r.code || r.roleCode || '').toLowerCase()
    return rName.includes(kw) || rCode.includes(kw)
  })
})

const filteredDrawerPermissions = computed(() => {
  if (!drawerRole.value) return []
  const perms = getRolePermissions(drawerRole.value)
  if (!drawerKeyword.value) return perms
  const kw = drawerKeyword.value.toLowerCase()
  return perms.filter(p => {
    const pName = (p.permissionName || p.name || '').toLowerCase()
    const pCode = (p.permissionCode || p.code || '').toLowerCase()
    return pName.includes(kw) || pCode.includes(kw)
  })
})

const loadData = async () => {
  try {
    const rRes = await getRoleList()
    if (rRes && rRes.data) roleList.value = rRes.data
    const pRes = await getPermissionList()
    if (pRes && pRes.data) allPermissions.value = pRes.data
  } catch (ex) {
    console.error('加载角色及权限数据失败:', ex)
  }
}

const getRolePermissions = (role) => {
  if (!role || !Array.isArray(role.permissionIds) || !Array.isArray(allPermissions.value)) return []
  return allPermissions.value.filter(p => p && role.permissionIds.includes(p.id))
}

const getRoleTagType = (code) => {
  if (code === 'Admin') return 'danger'
  if (code === 'OpsStaff') return 'warning'
  return 'success'
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  try {
    return new Date(dateStr).toLocaleDateString()
  } catch {
    return dateStr
  }
}

// Dialog Actions
const openAddDialog = () => {
  Object.assign(roleForm, {
    id: 0,
    code: '',
    name: '',
    description: '',
    permissionIds: []
  })
  showSaveModal.value = true
}

const openEditDialog = (role) => {
  Object.assign(roleForm, {
    id: role.id,
    code: role.code || role.roleCode,
    name: role.name || role.roleName,
    description: role.description || '',
    permissionIds: Array.isArray(role.permissionIds) ? [...role.permissionIds] : []
  })
  showSaveModal.value = true
}

const selectAllPermissions = () => {
  if (Array.isArray(allPermissions.value)) {
    roleForm.permissionIds = allPermissions.value.map(p => p.id)
  }
}

const clearAllPermissions = () => {
  roleForm.permissionIds = []
}

const submitSaveRole = async () => {
  if (!roleForm.name) {
    ElMessage.warning('请填写角色名称')
    return
  }
  if (roleForm.id === 0 && !roleForm.code) {
    ElMessage.warning('请填写角色标识')
    return
  }

  saveLoading.value = true
  try {
    const res = await saveRole({
      id: roleForm.id,
      roleCode: roleForm.code,
      roleName: roleForm.name,
      description: roleForm.description,
      permissionIds: roleForm.permissionIds
    })

    if (res && res.success) {
      ElMessage.success(roleForm.id === 0 ? '角色及权限配置创建成功' : '角色修改成功')
      showSaveModal.value = false
      await loadData()
    }
  } finally {
    saveLoading.value = false
  }
}

const openPermissionModal = (role) => {
  currentRole.value = role
  selectedPermIds.value = Array.isArray(role.permissionIds) ? [...role.permissionIds] : []
  showPermModal.value = true
}

const submitAssign = async () => {
  const res = await assignRolePermissions({
    roleId: currentRole.value.id,
    permissionIds: selectedPermIds.value
  })
  if (res && res.success) {
    ElMessage.success('授权矩阵配置已保存更新')
    showPermModal.value = false
    await loadData()
  }
}

const openDetailDrawer = (role) => {
  drawerRole.value = role
  drawerKeyword.value = ''
  showDrawer.value = true
}

const handleDeleteRole = (role) => {
  const name = role.name || role.roleName
  ElMessageBox.confirm(`确定要删除角色 [${name}] 吗？`, '安全防呆提示', {
    confirmButtonText: '确定删除',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    const res = await request.delete(`/role/${role.id}`)
    if (res && res.success) {
      ElMessage.success(res.message || '角色删除成功')
      await loadData()
    }
  }).catch(() => {})
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

.role-card {
  padding: 18px;
  margin-bottom: 16px;
  border-radius: 10px;
  transition: transform 0.2s ease;
}

.role-card:hover {
  transform: translateY(-2px);
}

.role-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.role-title {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.name-row {
  display: flex;
  align-items: center;
}

.role-title .name {
  font-size: 1.15rem;
  font-weight: 700;
  color: #f1f5f9;
}

.create-time {
  font-size: 0.75rem;
  color: #64748b;
}

.action-btns {
  display: flex;
  gap: 6px;
}

.role-desc {
  font-size: 0.85rem;
  color: #94a3b8;
  margin-top: 12px;
  min-height: 40px;
  line-height: 1.4;
}

.perm-preview-section {
  margin-top: 14px;
  padding-top: 12px;
  border-top: 1px dashed #334155;
}

.perm-preview-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.82rem;
  color: #94a3b8;
  margin-bottom: 8px;
}

.perm-tags-wrapper {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  min-height: 28px;
  align-items: center;
}

.perm-tag {
  border-radius: 4px;
}

.no-perm-tip {
  font-size: 0.78rem;
  color: #64748b;
  font-style: italic;
}

.form-tip {
  font-size: 0.75rem;
  color: #94a3b8;
  margin-top: 4px;
  display: block;
}

.perm-selection-container {
  width: 100%;
  border: 1px solid #334155;
  border-radius: 6px;
  padding: 10px;
  background: rgba(15, 23, 42, 0.4);
}

.perm-selection-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 8px;
  border-bottom: 1px solid #334155;
}

.select-count-info {
  font-size: 0.82rem;
  color: #10b981;
  font-weight: 500;
}

.perm-checkbox-scroll {
  max-height: 220px;
  overflow-y: auto;
  padding-top: 8px;
}

.perm-checkbox-row {
  padding: 4px 0;
}

.p-title {
  font-weight: 500;
  color: #f8fafc;
}

.p-code-badge {
  font-size: 0.78rem;
  color: #94a3b8;
  margin-left: 6px;
  font-family: monospace;
}

.perm-tree-box {
  max-height: 380px;
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

.drawer-role-info {
  padding: 12px;
  border-radius: 6px;
  font-size: 0.88rem;
  color: #cbd5e1;
  line-height: 1.6;
}

.highlight-count {
  color: #10b981;
  font-size: 1.1rem;
  font-weight: bold;
}

.drawer-perm-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: calc(100vh - 240px);
  overflow-y: auto;
}

.drawer-perm-item {
  padding: 10px 12px;
  background: rgba(30, 41, 59, 0.6);
  border: 1px solid #334155;
  border-radius: 6px;
}

.item-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}

.item-name {
  font-weight: 600;
  color: #f8fafc;
}

.item-bottom {
  display: flex;
  flex-direction: column;
  font-size: 0.78rem;
  color: #94a3b8;
  gap: 2px;
}

.item-path {
  color: #64748b;
}

.ml-2 {
  margin-left: 8px;
}
.mb-2 {
  margin-bottom: 8px;
}
.mb-3 {
  margin-bottom: 12px;
}
.mb-4 {
  margin-bottom: 16px;
}
</style>
