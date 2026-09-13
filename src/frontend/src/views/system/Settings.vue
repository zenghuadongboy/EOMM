<template>
  <div class="settings-page ops-glass-card">
    <div class="ops-card-title">
      <el-icon color="#3b82f6"><Setting /></el-icon>
      <span>系统管理与基础配置中心</span>
    </div>

    <el-row :gutter="20" class="mt-4">
      <!-- Form Column -->
      <el-col :xs="24" :md="14">
        <div class="setting-card ops-glass-card">
          <div class="card-subtitle">系统基础参数配置</div>
          <el-form :model="configForm" label-width="140px" class="mt-3">
            <el-form-item label="系统平台名称">
              <el-input v-model="configForm.systemName" placeholder="基于.NET的企业运维管理平台" />
            </el-form-item>
            <el-form-item label="所属企业/单位">
              <el-input v-model="configForm.companyName" placeholder="某某大型企业IT基础设施中心" />
            </el-form-item>
            <el-form-item label="监控采样轮询频率">
              <el-select v-model="configForm.sampleInterval" style="width: 100%">
                <el-option label="极速刷新 (3 秒)" :value="3" />
                <el-option label="标准刷新 (5 秒)" :value="5" />
                <el-option label="节能模式 (15 秒)" :value="15" />
              </el-select>
            </el-form-item>
            <el-form-item label="告警自动一键派单">
              <el-switch v-model="configForm.autoWorkOrder" active-text="开启 Critical 级告警自动联动派单" />
            </el-form-item>
            <el-form-item label="审计日志保留时长">
              <el-radio-group v-model="configForm.logKeepDays">
                <el-radio :label="30">30 天</el-radio>
                <el-radio :label="90">90 天</el-radio>
                <el-radio :label="180">半 年</el-radio>
              </el-radio-group>
            </el-form-item>
            <el-form-item label="运维防维护开关">
              <el-switch v-model="configForm.maintenanceMode" active-text="开启系统维护锁定模式" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="saving" @click="saveSettings">保存基础设置</el-button>
              <el-button @click="resetForm">重 置</el-button>
            </el-form-item>
          </el-form>
        </div>
      </el-col>

      <!-- Status Column -->
      <el-col :xs="24" :md="10">
        <div class="setting-card ops-glass-card">
          <div class="card-subtitle">系统环境与服务运行状态</div>
          <div class="env-status-list mt-3">
            <div class="env-item">
              <span class="e-name">后台核心框架</span>
              <span class="e-val text-cyan">ASP.NET Core 8.0 (C# 12)</span>
            </div>
            <div class="env-item">
              <span class="e-name">关系型数据库引擎</span>
              <span class="e-val text-green">MySQL 8.0 (EF Core ORM)</span>
            </div>
            <div class="env-item">
              <span class="e-name">分布式缓存/任务队列</span>
              <span class="e-val text-green">Redis 7.0 (StackExchange)</span>
            </div>
            <div class="env-item">
              <span class="e-name">前端响应式架构</span>
              <span class="e-val text-blue">Vue 3.4 + Element Plus</span>
            </div>
            <div class="env-item">
              <span class="e-name">系统运行模式</span>
              <span class="e-val text-green">Production 生产集群态</span>
            </div>
            <div class="env-item">
              <span class="e-name">数据库存储连接</span>
              <el-tag type="success" size="small">Normal 正常</el-tag>
            </div>
          </div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { Setting } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'

const saving = ref(false)

const configForm = reactive({
  systemName: '基于.NET的企业运维管理平台',
  companyName: 'IT基础设施运维管理中心',
  sampleInterval: 3,
  autoWorkOrder: true,
  logKeepDays: 90,
  maintenanceMode: false
})

const saveSettings = () => {
  saving.value = true
  setTimeout(() => {
    saving.value = false
    ElMessage.success('系统基础配置与参数修改成功！')
  }, 600)
}

const resetForm = () => {
  configForm.systemName = '基于.NET的企业运维管理平台'
  configForm.companyName = 'IT基础设施运维管理中心'
  configForm.sampleInterval = 3
  configForm.autoWorkOrder = true
  configForm.logKeepDays = 90
  configForm.maintenanceMode = false
}
</script>

<style scoped>
.settings-page {
  padding: 20px;
}

.setting-card {
  padding: 20px;
  border-radius: 10px;
  min-height: 420px;
}

.card-subtitle {
  font-size: 1.05rem;
  font-weight: 700;
  color: #f1f5f9;
  border-bottom: 1px solid #334155;
  padding-bottom: 10px;
}

.env-status-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.env-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 12px;
  background: rgba(15, 23, 42, 0.5);
  border: 1px solid #334155;
  border-radius: 6px;
}

.e-name {
  font-size: 0.85rem;
  color: #94a3b8;
}

.e-val {
  font-size: 0.88rem;
  font-weight: 600;
}

.text-cyan { color: #38bdf8; }
.text-green { color: #10b981; }
.text-blue { color: #60a5fa; }

.mt-3 { margin-top: 12px; }
.mt-4 { margin-top: 16px; }
</style>
