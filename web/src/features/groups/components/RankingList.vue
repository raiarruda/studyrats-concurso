<script setup lang="ts">
import { computed, ref } from 'vue'
import { formatMinutes } from '@/shared/utils/format'
import type { GroupSummary, RankingEntry } from '../types'

const props = defineProps<{
  group: GroupSummary
  entries: RankingEntry[]
  currentUserId: string
}>()

type SortKey = 'days' | 'weeks' | 'questions' | 'accuracy' | 'time' | 'streak'

// Constância vem primeiro (é o que mantém o hábito); as demais só aparecem se o grupo registra a métrica.
const sorters = computed(() => {
  const list: { key: SortKey; label: string }[] = [
    { key: 'days', label: 'Dias ativos' },
    { key: 'weeks', label: 'Semanas' },
  ]
  if (props.group.trackQuestions) list.push({ key: 'questions', label: 'Questões' })
  if (props.group.trackCorrect) list.push({ key: 'accuracy', label: '% acerto' })
  if (props.group.trackTime) list.push({ key: 'time', label: 'Tempo' })
  list.push({ key: 'streak', label: 'Sequência' })
  return list
})

const sortKey = ref<SortKey>('days')

const value = (e: RankingEntry, key: SortKey): number => {
  switch (key) {
    case 'days': return e.daysStudied
    case 'weeks': return e.currentWeekStreak
    case 'questions': return e.totalQuestions ?? 0
    case 'accuracy': return e.accuracyPercent ?? -1
    case 'time': return e.totalMinutes ?? 0
    case 'streak': return e.currentStreak
  }
}

// Empate: mais dias ativos, depois mais tempo estudado, depois mais questões, depois ordem alfabética.
const sorted = computed(() =>
  [...props.entries].sort(
    (a, b) =>
      value(b, sortKey.value) - value(a, sortKey.value) ||
      b.daysStudied - a.daysStudied ||
      (b.totalMinutes ?? 0) - (a.totalMinutes ?? 0) ||
      (b.totalQuestions ?? 0) - (a.totalQuestions ?? 0) ||
      a.name.localeCompare(b.name),
  ),
)

const medal = (i: number) => ['🥇', '🥈', '🥉'][i] ?? `${i + 1}º`
const plural = (n: number, one: string, many: string) => `${n} ${n === 1 ? one : many}`
</script>

<template>
  <div>
    <div class="sorters">
      <button
        v-for="s in sorters"
        :key="s.key"
        :class="{ active: sortKey === s.key }"
        @click="sortKey = s.key"
      >
        {{ s.label }}
      </button>
    </div>

    <ol class="rank">
      <li v-for="(e, i) in sorted" :key="e.userId" class="card" :class="{ me: e.userId === currentUserId }">
        <span class="pos">{{ medal(i) }}</span>
        <div class="grow">
          <strong>{{ e.name }}<span v-if="e.userId === currentUserId" class="muted"> (você)</span></strong>
          <div class="stats">
            <span>📅 <b>{{ e.daysStudied }}</b> {{ e.daysStudied === 1 ? 'dia ativo' : 'dias ativos' }}</span>
            <span :title="`Melhor sequência: ${plural(e.bestWeekStreak, 'semana', 'semanas')}`">
              🗓️ <b>{{ e.currentWeekStreak }}</b> {{ e.currentWeekStreak === 1 ? 'semana ativa' : 'semanas ativas' }}
            </span>
            <span v-if="e.totalQuestions !== null"><b>{{ e.totalQuestions }}</b> questões</span>
            <span v-if="e.accuracyPercent !== null"><b>{{ e.accuracyPercent }}%</b> acerto</span>
            <span v-if="e.totalMinutes !== null"><b>{{ formatMinutes(e.totalMinutes) }}</b></span>
            <span>🔥 <b>{{ e.currentStreak }}</b> {{ e.currentStreak === 1 ? 'dia seguido' : 'dias seguidos' }}</span>
          </div>
        </div>
      </li>
    </ol>
  </div>
</template>
