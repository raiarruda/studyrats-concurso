<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { assetUrl } from '@/shared/services/http'
import { formatDate, formatMinutes, localDate, monthLabel } from '@/shared/utils/format'
import type { CalendarDay } from '../types'

const props = defineProps<{
  month: string // yyyy-MM
  days: CalendarDay[]
  minMonth: string
  maxMonth: string
  loading?: boolean
}>()

const emit = defineEmits<{
  'update:month': [month: string]
}>()

const WEEKDAYS = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S']
const today = localDate()

const selected = ref<string | null>(null)
watch(
  () => props.month,
  () => (selected.value = null),
)

const byDate = computed(() => new Map(props.days.map((d) => [d.date, d])))
const selectedDay = computed(() => (selected.value ? byDate.value.get(selected.value) : undefined))

const cells = computed(() => {
  const [y, m] = props.month.split('-').map(Number)
  const blanks = new Date(y!, m! - 1, 1).getDay() // 0 = domingo
  const total = new Date(y!, m!, 0).getDate()
  const list: { key: string; day?: number; date?: string }[] = []
  for (let i = 0; i < blanks; i++) list.push({ key: `b${i}` })
  for (let d = 1; d <= total; d++) {
    list.push({ key: `d${d}`, day: d, date: `${props.month}-${String(d).padStart(2, '0')}` })
  }
  return list
})

const canPrev = computed(() => props.month > props.minMonth)
const canNext = computed(() => props.month < props.maxMonth)

function shift(delta: number) {
  const [y, m] = props.month.split('-').map(Number)
  const d = new Date(y!, m! - 1 + delta, 1)
  emit('update:month', `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`)
}
</script>

<template>
  <section class="card stack" aria-label="Calendário de estudo">
    <div class="row between">
      <button class="link" :disabled="!canPrev" aria-label="Mês anterior" @click="shift(-1)">‹</button>
      <strong class="cal-title">{{ monthLabel(month) }}</strong>
      <button class="link" :disabled="!canNext" aria-label="Próximo mês" @click="shift(1)">›</button>
    </div>

    <div class="cal-grid" :class="{ loading }">
      <span v-for="(w, i) in WEEKDAYS" :key="`w${i}`" class="cal-wd">{{ w }}</span>

      <template v-for="c in cells" :key="c.key">
        <span v-if="!c.date" />
        <button
          v-else
          type="button"
          class="cal-day"
          :class="{ has: byDate.has(c.date), today: c.date === today, selected: c.date === selected }"
          :disabled="!byDate.has(c.date)"
          :aria-label="byDate.has(c.date) ? `Dia ${c.day}: estudou` : `Dia ${c.day}`"
          @click="selected = c.date"
        >
          <img v-if="byDate.get(c.date)" :src="assetUrl(byDate.get(c.date)!.photoUrl)" alt="" loading="lazy" />
          <span class="cal-num">{{ c.day }}</span>
        </button>
      </template>
    </div>

    <p class="muted" style="margin: 0">
      {{ days.length }} {{ days.length === 1 ? 'dia ativo' : 'dias ativos' }} neste mês
    </p>

    <div v-if="selectedDay" class="stack cal-detail">
      <strong>{{ formatDate(selectedDay.date) }}</strong>
      <div class="meta">
        <span>{{ selectedDay.logsCount }} {{ selectedDay.logsCount === 1 ? 'registro' : 'registros' }}</span>
        <span v-if="selectedDay.questions !== null">✍️ <b>{{ selectedDay.questions }}</b> questões</span>
        <span v-if="selectedDay.minutes !== null">⏱️ <b>{{ formatMinutes(selectedDay.minutes) }}</b></span>
      </div>
      <img :src="assetUrl(selectedDay.photoUrl)" alt="Foto do estudo neste dia" class="preview" />
    </div>
  </section>
</template>
