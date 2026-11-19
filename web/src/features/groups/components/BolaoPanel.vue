<script setup lang="ts">
import { formatDate, formatMoney } from '@/shared/utils/format'
import type { Bolao, WeekStatus } from '../types'

defineProps<{
  bolao: Bolao
  currentUserId: string
  minDays: number
  busy?: boolean
}>()

const emit = defineEmits<{
  join: []
  leave: []
}>()

const WEEK_ICON: Record<WeekStatus, string> = {
  ok: '✅',
  failed: '❌',
  open: '⏳',
  future: '▫️',
  na: '·',
}
const WEEK_LABEL: Record<WeekStatus, string> = {
  ok: 'cumpriu a meta',
  failed: 'falhou',
  open: 'em andamento',
  future: 'ainda não começou',
  na: 'antes de entrar no bolão',
}

const medal = (position: number) => ['🥇', '🥈', '🥉'][position - 1] ?? `${position}º`
</script>

<template>
  <section class="stack" aria-label="Bolão simulado">
    <p class="muted" style="margin: 0">
      💡 Valores simulados: o app só calcula. O acerto de verdade fica por conta de vocês.
    </p>

    <div class="card stack">
      <div class="row between">
        <strong>{{ bolao.isFinal ? 'Resultado final' : 'Parcial (desafio em andamento)' }}</strong>
        <span class="badge">{{ bolao.isFinal ? 'Encerrado' : 'Em aberto' }}</span>
      </div>

      <div class="profile-stats">
        <div>
          <b>{{ formatMoney(bolao.potCents) }}</b>
          <span>Arrecadado</span>
        </div>
        <div v-if="bolao.caixaCents > 0">
          <b>{{ formatMoney(bolao.caixaCents) }}</b>
          <span>Caixa do grupo</span>
        </div>
        <div>
          <b>{{ bolao.members.length }}</b>
          <span>{{ bolao.members.length === 1 ? 'Participante' : 'Participantes' }}</span>
        </div>
      </div>

      <small class="muted">
        Multa de {{ formatMoney(bolao.penaltyCents) }} por semana com menos de {{ minDays }}
        {{ minDays === 1 ? 'dia' : 'dias' }} de estudo. Prêmios:
        {{ bolao.prizePercents.map((p, i) => `${i + 1}º ${p}%`).join(' · ') }} do arrecadado, pelo ranking de dias
        ativos do desafio (empate divide o prêmio das posições).
      </small>

      <button v-if="bolao.canJoin" class="primary" :disabled="busy" @click="emit('join')">
        {{ busy ? 'Entrando...' : 'Participar do bolão' }}
      </button>
      <button v-else-if="bolao.canLeave" :disabled="busy" @click="emit('leave')">
        {{ busy ? 'Saindo...' : 'Sair do bolão' }}
      </button>
      <small v-else-if="bolao.iAmIn" class="muted">
        Você está no bolão. A entrada é definitiva depois que uma semana sua é contabilizada.
      </small>
    </div>

    <p v-if="bolao.weeks.length === 0" class="muted">Este desafio não tem semanas inteiras.</p>

    <p v-if="bolao.members.length === 0" class="muted">
      Ninguém entrou no bolão ainda. Cada pessoa escolhe se quer participar.
    </p>

    <ol class="rank">
      <li v-for="m in bolao.members" :key="m.userId" class="card" :class="{ me: m.userId === currentUserId }">
        <span class="pos">{{ medal(m.position) }}</span>
        <div class="grow stack" style="gap: 6px">
          <div class="row between">
            <strong>{{ m.name }}<span v-if="m.userId === currentUserId" class="muted"> (você)</span></strong>
            <b :class="m.netCents > 0 ? 'ok' : m.netCents < 0 ? 'error' : 'muted'">
              {{ formatMoney(m.netCents, true) }}
            </b>
          </div>

          <div class="week-row" role="list" :aria-label="`Semanas de ${m.name}`">
            <span
              v-for="(status, i) in m.weeks"
              :key="i"
              role="listitem"
              :title="`Semana de ${formatDate(bolao.weeks[i]!)}: ${WEEK_LABEL[status]}`"
            >
              {{ WEEK_ICON[status] }}
            </span>
          </div>

          <div class="stats">
            <span><b>{{ m.failedWeeks }}</b> {{ m.failedWeeks === 1 ? 'semana falhada' : 'semanas falhadas' }}</span>
            <span>multas <b>{{ formatMoney(m.penaltyCents) }}</b></span>
            <span>prêmio <b>{{ formatMoney(m.prizeCents) }}</b></span>
          </div>
        </div>
      </li>
    </ol>

    <small class="muted">✅ cumpriu · ❌ falhou · ⏳ em andamento · ▫️ ainda não começou · · antes de entrar</small>
  </section>
</template>
