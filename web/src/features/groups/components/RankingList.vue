<script setup lang="ts">
import { computed, ref } from 'vue'
import { formatMinutes } from '@/shared/utils/format'
import type { GroupSummary, RankingEntry } from '../types'

const props = defineProps<{
  group: GroupSummary
  entries: RankingEntry[]
  currentUserId: string
}>()

type SortKey = 'questions' | 'accuracy' | 'time' | 'streak'

// Só oferece ordenação pelas métricas que o grupo realmente registra.
const sorters = computed(() => {
  const list: { key: SortKey; label: string }[] = []
  if (props.group.trackQuestions) list.push({ key: 'questions', label: 'Questões' })
  if (props.group.trackCorrect) list.push({ key: 'accuracy', label: '% acerto' })
  if (props.group.trackTime) list.push({ key: 'time', label: 'Tempo' })
  list.push({ key: 'streak', label: 'Sequência' })
  return list
})

const sortKey = ref<SortKey>(sorters.value[0]!.key)

const value = (e: RankingEntry, key: SortKey): number => {
  switch (key) {
    case 'questions': return e.totalQuestions ?? 0
    case 'accuracy': return e.accuracyPercent ?? -1
    case 'time': return e.totalMinutes ?? 0
    case 'streak': return e.currentStreak
  }
}

const sorted = computed(() =>
  [...props.entries].sort((a, b) => value(b, sortKey.value) - value(a, sortKey.value) || a.name.localeCompare(b.name)),
)

const medal = (i: number) => ['🥇', '🥈', '🥉'][i] ?? `${i + 1}º`
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
            <span v-if="e.totalQuestions !== null"><b>{{ e.totalQuestions }}</b> questões</span>
            <span v-if="e.accuracyPercent !== null"><b>{{ e.accuracyPercent }}%</b> acerto</span>
            <span v-if="e.totalMinutes !== null"><b>{{ formatMinutes(e.totalMinutes) }}</b></span>
            <span>🔥 <b>{{ e.currentStreak }}</b> {{ e.currentStreak === 1 ? 'dia' : 'dias' }}</span>
          </div>
        </div>
      </li>
    </ol>
  </div>
</template>
