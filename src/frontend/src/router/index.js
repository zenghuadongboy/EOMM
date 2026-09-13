import { createRouter, createWebHashHistory } from 'vue-router'
import MainLayout from '../views/layout/MainLayout.vue'
import { useUserStore } from '../store/user'
import { ElMessageBox } from 'element-plus'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/login/Index.vue'),
    meta: { title: '系统登录 - 基于.NET的企业运维管理平台' }
  },
  {
    path: '/',
    component: MainLayout,
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('../views/dashboard/Index.vue'),
        meta: { title: 'Dashboard首页', requiresAuth: true, permCode: 'dashboard:view' }
      },
      {
        path: 'assets',
        name: 'Assets',
        component: () => import('../views/assets/List.vue'),
        meta: { title: '设备资产管理', requiresAuth: true, permCode: 'asset:view' }
      },
      {
        path: 'alarms',
        name: 'Alarms',
        component: () => import('../views/alarms/List.vue'),
        meta: { title: '告警管理', requiresAuth: true, permCode: 'alarm:view' }
      },
      {
        path: 'alarms/rules',
        name: 'AlarmRules',
        component: () => import('../views/alarms/Rules.vue'),
        meta: { title: '自动化告警策略配置', requiresAuth: true, permCode: 'alarm:rule' }
      },
      {
        path: 'workorders',
        name: 'WorkOrders',
        component: () => import('../views/workorders/List.vue'),
        meta: { title: '运维工单全流程管理', requiresAuth: true }
      },
      {
        path: 'system/users',
        name: 'SystemUsers',
        component: () => import('../views/system/Users.vue'),
        meta: { title: '系统账号与用户管理', requiresAuth: true, permCode: 'system:user' }
      },
      {
        path: 'system/roles',
        name: 'SystemRoles',
        component: () => import('../views/system/Roles.vue'),
        meta: { title: 'RBAC角色与权限管理', requiresAuth: true, permCode: 'system:role' }
      },
      {
        path: 'system/permissions',
        name: 'SystemPermissions',
        component: () => import('../views/system/Permissions.vue'),
        meta: { title: '权限节点管理', requiresAuth: true, permCode: 'system:permission' }
      },
      {
        path: 'system/logs',
        name: 'SystemLogs',
        component: () => import('../views/system/Logs.vue'),
        meta: { title: '日志模块管理', requiresAuth: true, permCode: 'system:log' }
      },
      {
        path: 'monitoring',
        name: 'DeviceMonitoring',
        component: () => import('../views/monitoring/Index.vue'),
        meta: { title: '设备监控管理', requiresAuth: true, permCode: 'monitoring:view' }
      },
      {
        path: 'system/settings',
        name: 'SystemSettings',
        component: () => import('../views/system/Settings.vue'),
        meta: { title: '系统管理设置', requiresAuth: true, permCode: 'system:setting' }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  if (to.meta.title) {
    document.title = to.meta.title
  }

  const userStore = useUserStore()
  if (to.meta.requiresAuth && !userStore.isLoggedIn) {
    next('/login')
    return
  }

  if (to.meta.permCode && !userStore.hasPermission(to.meta.permCode)) {
    ElMessageBox.alert(
      `当前账号无权访问模块 [${to.meta.title || '受限模块'}]，请联系系统管理员分配权限。`,
      '访问权限受限',
      {
        confirmButtonText: '确定返回',
        type: 'warning',
        callback: () => {
          if (from.path && from.path !== to.path && from.path !== '/') {
            next(false)
          } else {
            next('/dashboard')
          }
        }
      }
    )
    return
  }

  next()
})

export default router
