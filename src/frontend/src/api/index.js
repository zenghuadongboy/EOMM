import request from './request'

// Auth APIs
export const login = (data) => request.post('/auth/login', data)
export const getMe = () => request.get('/auth/me')
export const changePassword = (data) => request.post('/auth/change-password', data)

// Dashboard APIs
export const getDashboardOverview = () => request.get('/dashboard/overview')

// Asset APIs
export const getAssetList = (params) => request.get('/asset/list', { params })
export const getAssetById = (id) => request.get(`/asset/${id}`)
export const saveAsset = (data) => request.post('/asset/save', data)
export const deleteAsset = (id) => request.delete(`/asset/${id}`)
export const simulateAssetMetric = (data) => request.post('/asset/simulate-metric', data)

// Alarm APIs
export const getAlarmRecords = (params) => request.get('/alarm/records', { params })
export const processAlarm = (data) => request.post('/alarm/process', data)
export const getAlarmRules = () => request.get('/alarm/rules')
export const saveAlarmRule = (data) => request.post('/alarm/rule/save', data)
export const deleteAlarmRule = (id) => request.delete(`/alarm/rule/${id}`)

// WorkOrder APIs
export const getWorkOrderList = (params) => request.get('/workorder/list', { params })
export const saveWorkOrder = (data) => request.post('/workorder/save', data)
export const createWorkOrderFromAlarm = (data) => request.post('/workorder/create-from-alarm', data)
export const changeWorkOrderStatus = (data) => request.post('/workorder/change-status', data)
export const getWorkOrderLogs = (id) => request.get(`/workorder/logs/${id}`)

// System User APIs
export const getUserList = () => request.get('/user/list')
export const saveUser = (data) => request.post('/user/save', data)
export const toggleUserStatus = (id) => request.post(`/user/toggle-status/${id}`)
export const resetUserPassword = (id) => request.post(`/user/reset-password/${id}`)

// System Role APIs
export const getRoleList = () => request.get('/role/list')
export const getPermissionList = () => request.get('/role/permissions')
export const saveRole = (data) => request.post('/role/save', data)
export const assignRolePermissions = (data) => request.post('/role/assign-permissions', data)

// System Permission APIs (权限管理)
export const getPermissionPageList = (params) => request.get('/permission/list', { params })
export const savePermission = (data) => request.post('/permission/save', data)
export const deletePermission = (id) => request.delete(`/permission/${id}`)

// Audit Log APIs
export const getOperationLogs = (params) => request.get('/log/operation', { params })
export const getSystemLogs = (params) => request.get('/log/system', { params })
