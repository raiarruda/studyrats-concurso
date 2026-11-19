<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/features/auth/stores/authStore'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { formatDate } from '@/shared/utils/format'
import ChallengeProgress from '../components/ChallengeProgress.vue'
import RankingList from '../components/RankingList.vue'
import { groupService } from '../services/groupService'
import type { Challenge, GroupDetail, RankingEntry } from '../types'

// `id` é o do grupo (vem da rota /groups/:id/challenges/:challengeId).
const props = defineProps<{ id: string; challengeId: string }>()

const auth = useAuthStore()
const router = useRouter()

const challenge = ref<Challenge | null>(null)
const detail = ref<GroupDetail | null>(null)
const ranking = ref<RankingEntry[]>([])

const loading = ref(true)
const error = ref<string | null>(null)
const deleting = ref(false)

async function load() {
  loading.value = true
  error.value = null
  try {
    const [c, d, r] = await Promise.all([
      groupService.challenge(props.id, props.challengeId),
      groupService.get(props.id),
      groupService.challengeRanking(props.id, props.challengeId),
    ])
    challenge.value = c
    detail.value = d
    ranking.value = r
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível carregar o desafio.')
  } finally {
    loading.value = false
  }
}

async function remove() {
  if (!challenge.value) return
  if (!confirm(`Apagar o desafio "${challenge.value.name}"? Os registros de estudo do grupo continuam.`)) return
  deleting.value = true
  error.value = null
  try {
    await groupService.deleteChallenge(props.id, props.challengeId)
    await router.push(`/groups/${props.id}`)
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível apagar o desafio.')
    deleting.value = false
  }
}

onMounted(load)
</script>

<template>
  <main class="stack">
    <RouterLink :to="`/groups/${id}`">← Voltar ao grupo</RouterLink>

    <p v-if="loading" class="muted">Carregando...</p>

    <div v-else-if="error && !challenge" class="card stack">
      <p class="error" role="alert">{{ error }}</p>
      <button @click="load">Tentar novamente</button>
    </div>

    <template v-else-if="challenge && detail">
      <header class="stack">
        <h1>🎯 {{ challenge.name }}</h1>
        <span class="muted">
          {{ formatDate(challenge.startDate) }} → prova em {{ formatDate(challenge.examDate) }}
        </span>
        <ChallengeProgress :start-date="challenge.startDate" :exam-date="challenge.examDate" />
        <span class="muted">
          O ranking conta só os registros feitos entre o início e a data da prova.
        </span>
      </header>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <RankingList :group="detail.group" :entries="ranking" :current-user-id="auth.user?.id ?? ''" />

      <button v-if="detail.group.isOwner" class="danger" :disabled="deleting" @click="remove">
        {{ deleting ? 'Apagando...' : 'Apagar desafio' }}
      </button>
    </template>
  </main>
</template>
