<script setup lang="ts">
import { assetUrl } from '@/shared/services/http'
import { formatDate, formatMinutes } from '@/shared/utils/format'
import type { StudyLog } from '../types'

defineProps<{
  log: StudyLog
  canDelete: boolean
  deleting?: boolean
}>()

const emit = defineEmits<{
  delete: [id: string]
}>()
</script>

<template>
  <article class="card log">
    <div class="row between">
      <strong>{{ log.userName }}</strong>
      <span class="muted">{{ formatDate(log.date) }}</span>
    </div>

    <p v-if="log.subject" class="muted" style="margin: 2px 0 0">{{ log.subject }}</p>

    <div class="meta">
      <span v-if="log.questionsAnswered !== null">✍️ <b>{{ log.questionsAnswered }}</b> questões</span>
      <span v-if="log.questionsCorrect !== null">✅ <b>{{ log.questionsCorrect }}</b> acertos</span>
      <span v-if="log.studyMinutes !== null">⏱️ <b>{{ formatMinutes(log.studyMinutes) }}</b></span>
    </div>

    <p v-if="log.note" style="margin: 8px 0 0">{{ log.note }}</p>

    <img :src="assetUrl(log.photoUrl)" :alt="`Foto do estudo de ${log.userName}`" loading="lazy" />

    <div v-if="canDelete" class="row" style="margin-top: 8px; justify-content: flex-end">
      <button class="link danger" :disabled="deleting" @click="emit('delete', log.id)">
        {{ deleting ? 'Apagando...' : 'Apagar' }}
      </button>
    </div>
  </article>
</template>
