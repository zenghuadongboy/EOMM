<template>
  <el-container class="layout-container">
    <!-- Left Sidebar -->
    <el-aside width="240px" class="ops-sidebar">
      <div class="sidebar-logo">
        <el-icon :size="24" color="#3b82f6"><Monitor /></el-icon>
        <span class="logo-text">.NET 运维管理平台</span>
      </div>

      <el-menu
        :default-active="activeMenu"
        class="ops-menu"
        background-color="#0b1329"
        text-color="#94a3b8"
        active-text-color="#3b82f6"
        router
      >
        <el-menu-item index="/dashboard" v-if="hasPerm('dashboard:view')">
          <el-icon><DataAnalysis /></el-icon>
          <span>首页Dashboard</span>
        </el-menu-item>

        <el-menu-item index="/assets" v-if="hasPerm('asset:view')">
          <el-icon><Cpu /></el-icon>
          <span>设备资产管理</span>
        </el-menu-item>

        <el-menu-item index="/monitoring" v-if="hasPerm('monitoring:view')">
          <el-icon><Odometer /></el-icon>
          <span>设备监控管理</span>
        </el-menu-item>

        <el-sub-menu index="alarms" v-if="hasPerm('alarm:view') || hasPerm('alarm:rule')">
          <template #title>
            <el-icon><Bell /></el-icon>
            <span>告警管理</span>
          </template>
          <el-menu-item index="/alarms" v-if="hasPerm('alarm:view')">告警记录查询</el-menu-item>
          <el-menu-item index="/alarms/rules" v-if="hasPerm('alarm:rule')">告警规则管理</el-menu-item>
        </el-sub-menu>

        <el-menu-item index="/workorders" v-if="hasPerm('asset:view') || hasPerm('alarm:view')">
          <el-icon><Tickets /></el-icon>
          <span>工单管理</span>
        </el-menu-item>

        <el-menu-item index="/system/logs" v-if="hasPerm('system:log')">
          <el-icon><Document /></el-icon>
          <span>日志审计</span>
        </el-menu-item>

        <el-sub-menu index="user-perm" v-if="hasPerm('system:user') || hasPerm('system:role') || hasPerm('system:permission')">
          <template #title>
            <el-icon><User /></el-icon>
            <span>用户与权限管理</span>
          </template>
          <el-menu-item index="/system/users" v-if="hasPerm('system:user')">用户管理</el-menu-item>
          <el-menu-item index="/system/roles" v-if="hasPerm('system:role')">角色管理</el-menu-item>
          <el-menu-item index="/system/permissions" v-if="hasPerm('system:permission')">权限节点管理</el-menu-item>
        </el-sub-menu>

        <el-menu-item index="/system/settings" v-if="hasPerm('system:setting')">
          <el-icon><Setting /></el-icon>
          <span>系统管理</span>
        </el-menu-item>
      </el-menu>

      <div class="system-status-widget">
        <div class="widget-title">
          <span class="pulse-dot online"></span> 后台服务正常运行
        </div>
        <div class="widget-info">ASP.NET Core 8.0 | MySQL | Redis</div>
      </div>
    </el-aside>

    <!-- Main Right Content -->
    <el-container>
      <!-- Top Navbar -->
      <el-header class="ops-header">
        <div class="header-left">
          <el-breadcrumb separator="/">
            <el-breadcrumb-item :to="{ path: '/' }">首页</el-breadcrumb-item>
            <el-breadcrumb-item>{{ currentTitle }}</el-breadcrumb-item>
          </el-breadcrumb>
        </div>

        <div class="header-right">
          <!-- Notification Drawer Trigger -->
          <div class="action-item" @click="showNoticeDrawer = true">
            <el-badge :value="pendingAlarmCount" :hidden="pendingAlarmCount === 0" type="danger">
              <el-icon :size="20"><Bell /></el-icon>
            </el-badge>
          </div>

          <!-- User Dropdown -->
          <el-dropdown trigger="click" @command="handleUserCommand">
            <div class="user-profile">
              <el-avatar :size="32" class="avatar-box">
                {{ userStore.userInfo.realName ? userStore.userInfo.realName.substring(0, 1) : '官' }}
              </el-avatar>
              <div class="user-details">
                <span class="name">{{ userStore.userInfo.realName || userStore.userInfo.username }}</span>
                <span class="role-badge">{{ userStore.userInfo.roleName }}</span>
              </div>
              <el-icon><ArrowDown /></el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="password">修改密码</el-dropdown-item>
                <el-dropdown-item command="logout" divided>退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <!-- Main Body -->
      <el-main class="ops-main">
        <router-view />
      </el-main>
    </el-container>

    <!-- Notice Drawer -->
    <el-drawer v-model="showNoticeDrawer" title="实时未处理告警通知" size="380px">
      <div class="notice-list" v-if="pendingAlarms.length">
        <div v-for="item in pendingAlarms" :key="item.id" class="notice-card" :class="item.severity.toLowerCase()">
          <div class="notice-header">
            <span class="severity-tag">{{ item.severity }}</span>
            <span class="time">{{ formatDate(item.triggeredAt) }}</span>
          </div>
          <div class="notice-asset">{{ item.assetName }} ({{ item.ipAddress }})</div>
          <div class="notice-content">{{ item.content }}</div>
        </div>
      </div>
      <el-empty v-else description="暂无待处理告警" />
    </el-drawer>

    <!-- Password Change Dialog -->
    <el-dialog v-model="showPasswordDialog" title="修改登录密码" width="420px" append-to-body align-center>
      <el-form :model="pwdForm" label-width="90px">
        <el-form-item label="原密码">
          <el-input v-model="pwdForm.oldPassword" type="password" show-password />
        </el-form-item>
        <el-form-item label="新密码">
          <el-input v-model="pwdForm.newPassword" type="password" show-password />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showPasswordDialog = false">取消</el-button>
        <el-button type="primary" @click="submitPasswordChange">确定修改</el-button>
      </template>
    </el-dialog>
  </el-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '../../store/user'
import { getAlarmRecords, changePassword } from '../../api'
import { formatDate } from '../../utils/date'
import { ElMessage } from 'element-plus'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const hasPerm = (code) => userStore.hasPermission(code)

const activeMenu = computed(() => route.path)
const currentTitle = computed(() => route.meta.title || '控制台')

const showNoticeDrawer = ref(false)
const pendingAlarms = ref([])
const pendingAlarmCount = computed(() => pendingAlarms.value.length)

const showPasswordDialog = ref(false)
const pwdForm = ref({ oldPassword: '', newPassword: '' })

const fetchPendingAlarms = async () => {
  try {
    const res = await getAlarmRecords({ status: 'Pending', pageIndex: 1, pageSize: 10 })
    if (res.data && res.data.items) {
      pendingAlarms.value = res.data.items
    }
  } catch (e) {
    // Ignore silent error
  }
}

const handleUserCommand = (cmd) => {
  if (cmd === 'logout') {
    userStore.logout()
    ElMessage.success('已安全退出登录')
    router.push('/login')
  } else if (cmd === 'password') {
    pwdForm.value = { oldPassword: '', newPassword: '' }
    showPasswordDialog.value = true
  }
}

const submitPasswordChange = async () => {
  if (!pwdForm.value.oldPassword || !pwdForm.value.newPassword) {
    ElMessage.warning('请填写完整的密码信息')
    return
  }
  const res = await changePassword(pwdForm.value)
  if (res.success) {
    ElMessage.success('密码修改成功，请重新登录')
    showPasswordDialog.value = false
    userStore.logout()
    router.push('/login')
  }
}

onMounted(() => {
  fetchPendingAlarms()
  setInterval(fetchPendingAlarms, 15000)
})
</script>

<style scoped>
.layout-container {
  width: 100vw;
  height: 100vh;
}

.ops-sidebar {
  background-color: var(--bg-sidebar);
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
}

.sidebar-logo {
  height: 64px;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 0 20px;
  border-bottom: 1px solid var(--border-color);
}

.logo-text {
  font-size: 1.05rem;
  font-weight: 700;
  color: #f8fafc;
}

.ops-menu {
  border-right: none;
  flex: 1;
}

.system-status-widget {
  margin: 16px;
  padding: 12px;
  background: rgba(30, 41, 59, 0.6);
  border: 1px solid var(--border-color);
  border-radius: 8px;
}

.widget-title {
  font-size: 0.8rem;
  color: #34d399;
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 600;
}

.widget-info {
  font-size: 0.72rem;
  color: #64748b;
  margin-top: 4px;
}

.ops-header {
  height: 64px;
  background-color: #0f172a;
  border-bottom: 1px solid var(--border-color);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 20px;
}

.action-item {
  cursor: pointer;
  color: #94a3b8;
  transition: color 0.2s;
}

.action-item:hover {
  color: #f8fafc;
}

.user-profile {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 8px;
  transition: background 0.2s;
}

.user-profile:hover {
  background: #1e293b;
}

.avatar-box {
  background: #3b82f6;
  color: #fff;
  font-weight: 600;
}

.user-details {
  display: flex;
  flex-direction: column;
}

.user-details .name {
  font-size: 0.88rem;
  color: #f8fafc;
  font-weight: 500;
}

.user-details .role-badge {
  font-size: 0.7rem;
  color: #94a3b8;
}

.ops-main {
  background-color: #0f172a;
  padding: 24px;
  overflow-y: auto;
}

.notice-card {
  padding: 12px;
  border-radius: 8px;
  background: #1e293b;
  border: 1px solid #334155;
  margin-bottom: 12px;
}

.notice-card.critical {
  border-left: 4px solid #ef4444;
}

.notice-card.warning {
  border-left: 4px solid #f59e0b;
}

.notice-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 6px;
}

.severity-tag {
  font-weight: 700;
  font-size: 0.8rem;

}

.notice-asset {
  font-weight: 600;
  color: #f1f5f9;
  font-size: 0.9rem;
}

.notice-content {
  font-size: 0.82rem;
  color: #94a3b8;
  margin-top: 4px;
}
</style>
