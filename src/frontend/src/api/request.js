import axios from 'axios'
import { ElMessage } from 'element-plus'

const service = axios.create({
  baseURL: '/api',
  timeout: 10000
})

let isRelogging = false

service.interceptors.request.use(
  config => {
    const token = localStorage.getItem('ops_token')
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`
    }
    return config
  },
  error => {
    return Promise.reject(error)
  }
)

service.interceptors.response.use(
  response => {
    const res = response.data
    if (res.success === false || (res.code && res.code !== 200)) {
      ElMessage.error(res.message || '操作失败')
      return Promise.reject(new Error(res.message || 'Error'))
    }
    return res
  },
  error => {
    if (error.response && error.response.status === 401) {
      if (!isRelogging) {
        isRelogging = true
        ElMessage.error('登录状态已失效，请重新登录')
        localStorage.removeItem('ops_token')
        localStorage.removeItem('ops_user')
        localStorage.removeItem('ops_perms')
        setTimeout(() => {
          window.location.href = '/#/login'
          isRelogging = false
        }, 500)
      }
    } else {
      ElMessage.error(error.response?.data?.message || error.message || '服务器连接异常')
    }
    return Promise.reject(error)
  }
)

export default service
