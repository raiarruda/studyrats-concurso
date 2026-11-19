<script setup lang="ts">
import { computed } from 'vue'
import { daysBetween, formatDate, localDate } from '@/shared/utils/format'

const props = defineProps<{
  startDate: string
  examDate: string
}>()

const progress = computed(() => {
  const today = localDate()
  const total = daysBetween(props.startDate, props.examDate) + 1
  const sinceStart = daysBetween(props.startDate, today)
  const untilExam = daysBetween(today, props.examDate)

  if (sinceStart < 0) {
    const n = -sinceStart
    return { label: `Começa em ${n} ${n === 1 ? 'dia' : 'dias'} · prova em ${formatDate(props.examDate)}`, percent: 0 }
  }
  if (untilExam < 0) return { label: `Prova realizada em ${formatDate(props.examDate)}`, percent: 100 }
  if (untilExam === 0) return { label: 'Hoje é a prova! 🍀', percent: 100 }

  const day = Math.min(sinceStart + 1, total)
  return {
    label: `Dia ${day} de ${total} · faltam ${untilExam} ${untilExam === 1 ? 'dia' : 'dias'} para a prova`,
    percent: Math.round((day / total) * 100),
  }
})
</script>

<template>
  <div class="period">
    <div class="row between">
      <span>{{ progress.label }}</span>
      <span class="muted">{{ progress.percent }}%</span>
    </div>
    <div
      class="bar"
      role="progressbar"
      aria-label="Progresso até a prova"
      :aria-valuenow="progress.percent"
      aria-valuemin="0"
      aria-valuemax="100"
    >
      <div class="fill" :style="{ width: progress.percent + '%' }" />
    </div>
  </div>
</template>
