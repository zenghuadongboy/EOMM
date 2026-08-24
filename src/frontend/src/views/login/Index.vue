<template>
  <div class="login-container">
    <div class="bg-glow"></div>
    <div class="login-box ops-glass-card">
      <div class="login-header">
        <div class="logo-icon">
          <el-icon :size="32" color="#3b82f6"><Monitor /></el-icon>
        </div>
        <h2>企业级 IT 运维管理平台</h2>
        <p class="subtitle">Enterprise IT Operations & Maintenance System</p>
      </div>

      <el-form :model="loginForm" :rules="rules" ref="formRef" class="login-form" size="large">
        <el-form-item prop="username">
          <el-input v-model="loginForm.username" placeholder="请输入用户名" prefix-icon="User" />
        </el-form-item>

        <el-form-item prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="请输入密码"
            prefix-icon="Lock"
            show-password
            @keyup.enter="handleLogin"
          />
        </el-form-item>

        <el-form-item>
          <el-button type="primary" class="login-btn" :loading="loading" @click="handleLogin">
            安全登录入口
          </el-button>
        </el-form-item>
      </el-form>

      <div class="quick-login">
        <span class="tip-label">快捷体验测试账号：</span>
        <div class="account-tags">
          <el-tag class="account-tag" @click="fillAccount('admin', 'admin123')">超级管理员 (admin)</el-tag>
          <el-tag type="success" class="account-tag" @click="fillAccount('ops', 'ops123')">运维工程师 (ops)</el-tag>
          <el-tag type="info" class="account-tag" @click="fillAccount('viewer', 'viewer123')">只读访客 (viewer)</el-tag>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '../../store/user'
import { ElMessage } from 'element-plus'

const router = useRouter()
const userStore = useUserStore()

const formRef = ref(null)
const loading = ref(false)

const loginForm = reactive({
  username: 'admin',
  password: 'admin123'
})

const rules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const fillAccount = (u, p) => {
  loginForm.username = u
  loginForm.password = p
}

const handleLogin = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    loading.value = true
    try {
      const success = await userStore.login(loginForm)
      if (success) {
        ElMessage.success('登录成功，正在进入运维平台控制台...')
        router.push('/')
      }
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped>
.login-container {
  position: relative;
  width: 100vw;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: radial-gradient(circle at 50% 30%, #1e293b 0%, #0f172a 70%);
}

.bg-glow {
  position: absolute;
  width: 600px;
  height: 600px;
  background: radial-gradient(circle, rgba(59, 130, 246, 0.15) 0%, rgba(6, 182, 212, 0) 70%);
  top: 20%;
  left: 30%;
  filter: blur(50px);
  pointer-events: none;
}

.login-box {
  width: 440px;
  padding: 40px;
  z-index: 10;
}

.login-header {
  text-align: center;
  margin-bottom: 30px;
}

.logo-icon {
  width: 64px;
  height: 64px;
  margin: 0 auto 16px;
  border-radius: 16px;
  background: rgba(59, 130, 246, 0.1);
  border: 1px solid rgba(59, 130, 246, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-header h2 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #f8fafc;
  letter-spacing: 0.5px;
}

.subtitle {
  font-size: 0.85rem;
  color: #64748b;
  margin-top: 6px;
}

.login-form {
  margin-top: 20px;
}

.login-btn {
  width: 100%;
  height: 44px;
  font-size: 1rem;
  font-weight: 600;
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
  border: none;
  border-radius: 8px;
  margin-top: 10px;
}

.login-btn:hover {
  background: linear-gradient(135deg, #60a5fa 0%, #3b82f6 100%);
}

.quick-login {
  margin-top: 24px;
  padding-top: 20px;
  border-top: 1px solid #334155;
}

.tip-label {
  font-size: 0.8rem;
  color: #94a3b8;
  display: block;
  margin-bottom: 10px;
}

.account-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.account-tag {
  cursor: pointer;
  transition: all 0.2s;
}

.account-tag:hover {
  transform: translateY(-2px);
}
</style>
