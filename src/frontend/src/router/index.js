import { createRouter, createWebHashHistory } from 'vue-router'
import MainLayout from '../views/layout/MainLayout.vue'
import { useUserStore } from '../store/user'

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
        meta: { title: '运维监控大屏看板', requiresAuth: true }
      },
      {
        path: 'assets',
        name: 'Assets',
        component: () => import('../views/assets/List.vue'),
        meta: { title: 'IT设备资产全生命周期管理', requiresAuth: true }
      },
      {
        path: 'alarms',
        name: 'Alarms',
        component: () => import('../views/alarms/List.vue'),
        meta: { title: '告警中心与处置闭环', requiresAuth: true }
      },
      {
        path: 'alarms/rules',
        name: 'AlarmRules',
        component: () => import('../views/alarms/Rules.vue'),
        meta: { title: '自动化告警策略配置', requiresAuth: true }
      },
      {
        path: 'system/users',
        name: 'SystemUsers',
        component: () => import('../views/system/Users.vue'),
        meta: { title: '系统账号与用户管理', requiresAuth: true }
      },
      {
        path: 'system/roles',
        name: 'SystemRoles',
        component: () => import('../views/system/Roles.vue'),
        meta: { title: 'RBAC角色与权限管理', requiresAuth: true }
      },
      {
        path: 'system/logs',
        name: 'SystemLogs',
        component: () => import('../views/system/Logs.vue'),
        meta: { title: '全量操作审计与系统日志', requiresAuth: true }
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
  } else {
    next()
  }
})

export default router
