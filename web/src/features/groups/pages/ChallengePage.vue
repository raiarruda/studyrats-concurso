<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/features/auth/stores/authStore'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { formatDate, formatMoney } from '@/shared/utils/format'
import BolaoPanel from '../components/BolaoPanel.vue'
import ChallengeProgress from '../components/ChallengeProgress.vue'
import RankingList from '../components/RankingList.vue'
import { groupService } from '../services/groupService'
import type { Bolao, Challenge, GroupDetail, RankingEntry } from '../types'

// `id` é o do grupo (vem da rota /groups/:id/challenges/:challengeId).
const props = defineProps<{ id: string; challengeId: string }>()

const auth = useAuthStore()
const router = useRouter()

const challenge = ref<Challenge | null>(null)
const detail = ref<GroupDetail | null>(null)
const ranking = ref<RankingEntry[]>([])
const bolao = ref<Bolao | null>(null)

const tab = ref<'ranking' | 'bolao'>('ranking')
const loading = ref(true)
const error = ref<string | null>(null)
const deleting = ref(false)
const bolaoBusy = ref(false)

async function load() {
  loading.value = true
  error.value = null
  try {
    const c = await groupService.challenge(props.id, props.challengeId)
    const [d, r, b] = await Promise.all([
      groupService.get(props.id),
      groupService.challengeRanking(props.id, props.challengeId),
      c.bet ? groupService.bolao(props.id, props.challengeId) : Promise.resolve(null),
    ])
    challenge.value = c
    detail.value = d
    ranking.value = r
    bolao.value = b
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível carregar o desafio.')
  } finally {
    loading.value = false
  }
}

async function joinBolao() {
  if (!challenge.value?.bet) return
  const penalty = formatMoney(challenge.value.bet.penaltyCents)
  const ok = confirm(
    `Entrar no bolão?\n\nCada semana inteira (segunda a domingo) abaixo da meta vale uma multa de ${penalty}, ` +
      'contada só a partir de agora. A entrada vira definitiva quando uma semana sua for contabilizada.\n\n' +
      'Os valores são simulados: o app não movimenta dinheiro.',
  )
  if (!ok) return

  bolaoBusy.value = true
  error.value = null
  try {
    bolao.value = await groupService.joinBolao(props.id, props.challengeId)
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível entrar no bolão.')
  } finally {
    bolaoBusy.value = false
  }
}

async function leaveBolao() {
  bolaoBusy.value = true
  error.value = null
  try {
    await groupService.leaveBolao(props.id, props.challengeId)
    bolao.value = await groupService.bolao(props.id, props.challengeId)
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível sair do bolão.')
  } finally {
    bolaoBusy.value = false
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
      </header>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <div v-if="challenge.bet" class="tabs" role="tablist">
        <button role="tab" :class="{ active: tab === 'ranking' }" @click="tab = 'ranking'">Ranking</button>
        <button role="tab" :class="{ active: tab === 'bolao' }" @click="tab = 'bolao'">💰 Bolão</button>
      </div>

      <template v-if="tab === 'ranking' || !bolao">
        <span class="muted">O ranking conta só os registros feitos entre o início e a data da prova.</span>
        <RankingList :group="detail.group" :entries="ranking" :current-user-id="auth.user?.id ?? ''" />
      </template>

      <BolaoPanel
        v-else
        :bolao="bolao"
        :current-user-id="auth.user?.id ?? ''"
        :min-days="detail.group.minDaysPerWeek"
        :busy="bolaoBusy"
        @join="joinBolao"
        @leave="leaveBolao"
      />

      <button v-if="detail.group.isOwner" class="danger" :disabled="deleting" @click="remove">
        {{ deleting ? 'Apagando...' : 'Apagar desafio' }}
      </button>
    </template>
  </main>
</template>
