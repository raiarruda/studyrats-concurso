<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useAuthStore } from '@/features/auth/stores/authStore'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { formatDate } from '@/shared/utils/format'
import LogCard from '../components/LogCard.vue'
import RankingList from '../components/RankingList.vue'
import { groupService } from '../services/groupService'
import type { GroupDetail, RankingEntry, StudyLog } from '../types'

const props = defineProps<{ id: string }>()

const PAGE_SIZE = 20
const auth = useAuthStore()

const detail = ref<GroupDetail | null>(null)
const ranking = ref<RankingEntry[]>([])
const logs = ref<StudyLog[]>([])
const hasMore = ref(false)

const tab = ref<'ranking' | 'feed'>('ranking')
const loading = ref(true)
const loadingMore = ref(false)
const error = ref<string | null>(null)
const deletingId = ref<string | null>(null)
const copied = ref(false)

async function load() {
  loading.value = true
  error.value = null
  try {
    const [d, r, l] = await Promise.all([
      groupService.get(props.id),
      groupService.ranking(props.id),
      groupService.logs(props.id, 0, PAGE_SIZE),
    ])
    detail.value = d
    ranking.value = r
    logs.value = l
    hasMore.value = l.length === PAGE_SIZE
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível carregar o grupo.')
  } finally {
    loading.value = false
  }
}

async function loadMore() {
  loadingMore.value = true
  try {
    const next = await groupService.logs(props.id, logs.value.length, PAGE_SIZE)
    logs.value.push(...next)
    hasMore.value = next.length === PAGE_SIZE
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível carregar mais registros.')
  } finally {
    loadingMore.value = false
  }
}

async function removeLog(logId: string) {
  if (!confirm('Apagar este registro? Os pontos dele saem do ranking.')) return
  deletingId.value = logId
  try {
    await groupService.deleteLog(props.id, logId)
    logs.value = logs.value.filter((l) => l.id !== logId)
    ranking.value = await groupService.ranking(props.id)
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível apagar o registro.')
  } finally {
    deletingId.value = null
  }
}

async function copyCode() {
  if (!detail.value) return
  try {
    await navigator.clipboard.writeText(detail.value.group.code)
    copied.value = true
    setTimeout(() => (copied.value = false), 2000)
  } catch {
    // Sem permissão de área de transferência: o código continua visível na tela.
  }
}

onMounted(load)
</script>

<template>
  <main>
    <RouterLink to="/">← Meus grupos</RouterLink>

    <p v-if="loading" class="muted">Carregando...</p>

    <div v-else-if="error && !detail" class="card stack" style="margin-top: 12px">
      <p class="error" role="alert">{{ error }}</p>
      <button @click="load">Tentar novamente</button>
    </div>

    <template v-else-if="detail">
      <header class="stack" style="margin-top: 8px">
        <h1>{{ detail.group.name }}</h1>
        <span class="muted">
          {{ formatDate(detail.group.startDate) }} → {{ formatDate(detail.group.endDate) }} ·
          {{ detail.group.memberCount }} {{ detail.group.memberCount === 1 ? 'pessoa' : 'pessoas' }}
        </span>
        <div class="badges">
          <span v-if="detail.group.trackQuestions" class="badge">Questões</span>
          <span v-if="detail.group.trackCorrect" class="badge">Acertos</span>
          <span v-if="detail.group.trackTime" class="badge">Tempo</span>
        </div>
        <div class="row">
          <span class="muted">Código de convite:</span>
          <span class="code">{{ detail.group.code }}</span>
          <button class="link" @click="copyCode">{{ copied ? 'Copiado ✓' : 'Copiar' }}</button>
        </div>
      </header>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <div class="tabs" role="tablist">
        <button role="tab" :class="{ active: tab === 'ranking' }" @click="tab = 'ranking'">Ranking</button>
        <button role="tab" :class="{ active: tab === 'feed' }" @click="tab = 'feed'">Feed</button>
      </div>

      <RankingList
        v-if="tab === 'ranking'"
        :group="detail.group"
        :entries="ranking"
        :current-user-id="auth.user?.id ?? ''"
      />

      <div v-else class="stack">
        <p v-if="logs.length === 0" class="muted">Ninguém registrou estudo ainda. Seja o primeiro! 💪</p>
        <LogCard
          v-for="log in logs"
          :key="log.id"
          :log="log"
          :can-delete="log.userId === auth.user?.id"
          :deleting="deletingId === log.id"
          @delete="removeLog"
        />
        <button v-if="hasMore" :disabled="loadingMore" @click="loadMore">
          {{ loadingMore ? 'Carregando...' : 'Carregar mais' }}
        </button>
      </div>
    </template>
  </main>

  <div v-if="detail" class="fab-bar">
    <RouterLink :to="`/groups/${id}/log`" class="button">📷 Registrar estudo</RouterLink>
  </div>
</template>
