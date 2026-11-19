<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { currentMonth, formatMinutes } from '@/shared/utils/format'
import CalendarMonth from '../components/CalendarMonth.vue'
import { profileService } from '../services/profileService'
import type { Profile } from '../types'

// Sem `userId` (rota /profile) é o perfil de quem está logado.
const props = defineProps<{ userId?: string }>()

const profile = ref<Profile | null>(null)
const month = ref(currentMonth())
const loading = ref(true)
const error = ref<string | null>(null)
let request = 0 // descarta respostas antigas ao trocar de mês ou de pessoa rapidamente

const minMonth = computed(() => profile.value?.firstActiveDate?.slice(0, 7) ?? currentMonth())

async function load() {
  const current = ++request
  loading.value = true
  error.value = null
  try {
    const result = await profileService.get(month.value, props.userId)
    if (current === request) profile.value = result
  } catch (e) {
    if (current === request) error.value = apiErrorMessage(e, 'Não foi possível carregar o perfil.')
  } finally {
    if (current === request) loading.value = false
  }
}

watch(month, load)
watch(
  () => props.userId,
  () => {
    profile.value = null
    month.value = currentMonth()
    load()
  },
)
onMounted(load)
</script>

<template>
  <main class="stack">
    <RouterLink to="/">← Meus grupos</RouterLink>

    <p v-if="loading && !profile" class="muted">Carregando...</p>

    <div v-else-if="error && !profile" class="card stack">
      <p class="error" role="alert">{{ error }}</p>
      <button @click="load">Tentar novamente</button>
    </div>

    <template v-else-if="profile">
      <header class="profile-head">
        <div class="avatar" aria-hidden="true">{{ profile.name.charAt(0).toUpperCase() }}</div>
        <h1>{{ profile.name }}<span v-if="profile.isMe" class="muted"> (você)</span></h1>
      </header>

      <div class="profile-stats">
        <div>
          <b>{{ profile.totalLogs }}</b>
          <span>{{ profile.totalLogs === 1 ? 'Registro' : 'Registros' }}</span>
        </div>
        <div>
          <b>{{ profile.activeDays }}</b>
          <span>{{ profile.activeDays === 1 ? 'Dia ativo' : 'Dias ativos' }}</span>
        </div>
        <div>
          <b>🔥 {{ profile.currentStreak }}</b>
          <span>{{ profile.currentStreak === 1 ? 'Dia seguido' : 'Dias seguidos' }}</span>
        </div>
        <div v-if="profile.totalQuestions !== null">
          <b>{{ profile.totalQuestions }}</b>
          <span>Questões</span>
        </div>
        <div v-if="profile.totalMinutes !== null">
          <b>{{ formatMinutes(profile.totalMinutes) }}</b>
          <span>Duração</span>
        </div>
      </div>

      <p v-if="!profile.isMe" class="muted" style="margin: 0">
        Mostra só o que foi registrado nos grupos que vocês têm em comum.
      </p>
      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <CalendarMonth
        v-model:month="month"
        :days="profile.days"
        :min-month="minMonth"
        :max-month="currentMonth()"
        :loading="loading"
      />
    </template>
  </main>
</template>
