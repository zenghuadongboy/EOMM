import { defineStore } from 'pinia'
import { login as loginApi, getMe as getMeApi } from '../api'

export const useUserStore = defineStore('user', {
  state: () => ({
    token: localStorage.getItem('ops_token') || '',
    userInfo: JSON.parse(localStorage.getItem('ops_user') || '{}'),
    permissions: JSON.parse(localStorage.getItem('ops_perms') || '[]')
  }),
  getters: {
    isLoggedIn: (state) => !!state.token,
    hasPermission: (state) => (code) => {
      if (state.userInfo.roleCode === 'Admin') return true
      return state.permissions.includes(code)
    }
  },
  actions: {
    async login(loginForm) {
      const res = await loginApi(loginForm)
      if (res.success && res.data) {
        this.token = res.data.token
        this.userInfo = {
          userId: res.data.userId,
          username: res.data.username,
          realName: res.data.realName,
          roleCode: res.data.roleCode,
          roleName: res.data.roleName
        }
        this.permissions = res.data.permissions || []

        localStorage.setItem('ops_token', this.token)
        localStorage.setItem('ops_user', JSON.stringify(this.userInfo))
        localStorage.setItem('ops_perms', JSON.stringify(this.permissions))
        return true
      }
      return false
    },
    logout() {
      this.token = ''
      this.userInfo = {}
      this.permissions = []
      localStorage.removeItem('ops_token')
      localStorage.removeItem('ops_user')
      localStorage.removeItem('ops_perms')
    }
  }
})
