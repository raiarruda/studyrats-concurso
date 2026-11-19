<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useAuthStore } from '@/features/auth/stores/authStore'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { formatDate, localDate } from '@/shared/utils/format'
import ChallengeProgress from '../components/ChallengeProgress.vue'
import LogCard from '../components/LogCard.vue'
import RankingList from '../components/RankingList.vue'
import { groupService } from '../services/groupService'
import type { Challenge, GroupDetail, RankingEntry, StudyLog } from '../types'

const props = defineProps<{ id: string }>()

const PAGE_SIZE = 20
const auth = useAuthStore()

const detail = ref<GroupDetail | null>(null)
const ranking = ref<RankingEntry[]>([])
const logs = ref<StudyLog[]>([])
const challenges = ref<Challenge[]>([])
const hasMore = ref(false)

const tab = ref<'ranking' | 'challenges' | 'feed'>('ranking')
const loading = ref(true)
const loadingMore = ref(false)
const error = ref<string | null>(null)
const deletingId = ref<string | null>(null)
const copied = ref(false)

// Desafio em andamento (se houver mais de um, o que tem a prova mais próxima).
const activeChallenge = computed(() => {
  const today = localDate()
  return challenges.value
    .filter((c) => c.startDate <= today && today <= c.examDate)
    .sort((a, b) => a.examDate.localeCompare(b.examDate))[0]
})

async function load() {
  loading.value = true
  error.value = null
  try {
    const [d, r, l, c] = await Promise.all([
      groupService.get(props.id),
      groupService.ranking(props.id),
      groupService.logs(props.id, 0, PAGE_SIZE),
      groupService.challenges(props.id),
    ])
    detail.value = d
    ranking.value = r
    logs.value = l
    challenges.value = c
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
          {{ detail.group.memberCount }} {{ detail.group.memberCount === 1 ? 'pessoa' : 'pessoas' }} ·
          semana ativa = {{ detail.group.minDaysPerWeek }}
          {{ detail.group.minDaysPerWeek === 1 ? 'dia' : 'dias' }} de estudo
        </span>

        <RouterLink v-if="activeChallenge" :to="`/groups/${id}/challenges/${activeChallenge.id}`" class="card stack challenge-banner">
          <strong>🎯 {{ activeChallenge.name }}</strong>
          <ChallengeProgress :start-date="activeChallenge.startDate" :exam-date="activeChallenge.examDate" />
          <span class="muted">Ver ranking do desafio →</span>
        </RouterLink>

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
        <button role="tab" :class="{ active: tab === 'challenges' }" @click="tab = 'challenges'">Desafios</button>
        <button role="tab" :class="{ active: tab === 'feed' }" @click="tab = 'feed'">Feed</button>
      </div>

      <RankingList
        v-if="tab === 'ranking'"
        :group="detail.group"
        :entries="ranking"
        :current-user-id="auth.user?.id ?? ''"
      />

      <div v-else-if="tab === 'challenges'" class="stack">
        <p class="muted" style="margin: 0">
          Um desafio é a preparação do grupo para um edital, com data de prova e ranking próprio.
        </p>

        <RouterLink v-if="detail.group.isOwner" :to="`/groups/${id}/challenges/new`" class="button">
          + Novo desafio
        </RouterLink>

        <p v-if="challenges.length === 0" class="muted">
          Nenhum desafio ainda.{{ detail.group.isOwner ? '' : ' Quem criou o grupo pode criar o primeiro.' }}
        </p>

        <RouterLink
          v-for="c in challenges"
          :key="c.id"
          :to="`/groups/${id}/challenges/${c.id}`"
          class="card stack challenge-banner"
        >
          <strong>{{ c.name }}</strong>
          <span class="muted">{{ formatDate(c.startDate) }} → prova em {{ formatDate(c.examDate) }}</span>
          <ChallengeProgress :start-date="c.startDate" :exam-date="c.examDate" />
        </RouterLink>
      </div>

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
