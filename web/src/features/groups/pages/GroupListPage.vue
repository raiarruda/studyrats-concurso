<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { groupService } from '../services/groupService'
import type { GroupSummary } from '../types'

const router = useRouter()

const groups = ref<GroupSummary[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const code = ref('')
const joining = ref(false)
const joinError = ref<string | null>(null)

async function load() {
  loading.value = true
  error.value = null
  try {
    groups.value = await groupService.list()
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível carregar seus grupos.')
  } finally {
    loading.value = false
  }
}

async function join() {
  joining.value = true
  joinError.value = null
  try {
    const group = await groupService.join(code.value)
    await router.push(`/groups/${group.id}`)
  } catch (e) {
    joinError.value = apiErrorMessage(e, 'Não foi possível entrar no grupo.')
  } finally {
    joining.value = false
  }
}

onMounted(load)
</script>

<template>
  <main class="stack">
    <div class="row between">
      <h1>Meus grupos</h1>
      <RouterLink to="/groups/new" class="button">+ Novo</RouterLink>
    </div>

    <p v-if="loading" class="muted">Carregando...</p>

    <div v-else-if="error" class="card stack">
      <p class="error" role="alert">{{ error }}</p>
      <button @click="load">Tentar novamente</button>
    </div>

    <p v-else-if="groups.length === 0" class="muted">
      Você ainda não está em nenhum grupo. Crie um ou entre com um código de convite.
    </p>

    <RouterLink
      v-for="g in groups"
      v-else
      :key="g.id"
      :to="`/groups/${g.id}`"
      class="card stack"
      style="text-decoration: none; color: inherit"
    >
      <div class="row between">
        <h2 style="margin: 0">{{ g.name }}</h2>
        <span class="muted">{{ g.memberCount }} {{ g.memberCount === 1 ? 'pessoa' : 'pessoas' }}</span>
      </div>
      <span class="muted">Semana ativa: {{ g.minDaysPerWeek }} {{ g.minDaysPerWeek === 1 ? 'dia' : 'dias' }} de estudo</span>
      <div class="badges">
        <span v-if="g.trackQuestions" class="badge">Questões</span>
        <span v-if="g.trackCorrect" class="badge">Acertos</span>
        <span v-if="g.trackTime" class="badge">Tempo</span>
      </div>
    </RouterLink>

    <form class="card stack" @submit.prevent="join">
      <h2>Entrar com código</h2>
      <div class="row">
        <input
          v-model="code"
          class="grow"
          placeholder="Ex.: K7M2QX"
          maxlength="12"
          autocapitalize="characters"
          autocomplete="off"
          required
        />
        <button type="submit" class="primary" :disabled="joining">{{ joining ? '...' : 'Entrar' }}</button>
      </div>
      <p v-if="joinError" class="error" role="alert">{{ joinError }}</p>
    </form>
  </main>
</template>
