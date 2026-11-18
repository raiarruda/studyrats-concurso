<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { localDate } from '@/shared/utils/format'
import { resizeImage } from '@/shared/utils/resizeImage'
import { groupService } from '../services/groupService'
import type { GroupSummary } from '../types'

const props = defineProps<{ id: string }>()
const router = useRouter()

const group = ref<GroupSummary | null>(null)
const loadError = ref<string | null>(null)

const form = reactive({
  dayOffset: 0, // 0 = hoje (o servidor decide a data); 1..3 = dias anteriores
  questionsAnswered: null as number | null,
  questionsCorrect: null as number | null,
  hours: 0,
  minutes: 0,
  subject: '',
  note: '',
})

const photoFile = ref<File | null>(null)
const photoPreview = ref<string | null>(null)

const submitting = ref(false)
const error = ref<string | null>(null)

const dayOptions = computed(() => [
  { value: 0, label: 'Hoje' },
  { value: 1, label: 'Ontem' },
  { value: 2, label: 'Anteontem' },
  { value: 3, label: `${localDate(3).split('-').reverse().slice(0, 2).join('/')}` },
])

async function load() {
  try {
    group.value = (await groupService.get(props.id)).group
  } catch (e) {
    loadError.value = apiErrorMessage(e, 'Não foi possível carregar o grupo.')
  }
}

function onPhoto(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0] ?? null
  if (photoPreview.value) URL.revokeObjectURL(photoPreview.value)
  photoFile.value = file
  photoPreview.value = file ? URL.createObjectURL(file) : null
}

async function submit() {
  if (!group.value) return
  error.value = null

  if (!photoFile.value) {
    error.value = 'A foto é obrigatória. Tire ou escolha uma foto do seu estudo.'
    return
  }

  const g = group.value
  const totalMinutes = form.hours * 60 + form.minutes
  if (g.trackQuestions && !form.questionsAnswered) {
    error.value = 'Informe quantas questões você respondeu.'
    return
  }
  if (g.trackCorrect && form.questionsCorrect === null) {
    error.value = 'Informe quantas questões você acertou.'
    return
  }
  if (g.trackCorrect && (form.questionsCorrect ?? 0) > (form.questionsAnswered ?? 0)) {
    error.value = 'Os acertos não podem passar do total de questões.'
    return
  }
  if (g.trackTime && totalMinutes < 1) {
    error.value = 'Informe quanto tempo você estudou.'
    return
  }

  submitting.value = true
  try {
    const photo = await resizeImage(photoFile.value)
    await groupService.createLog(props.id, {
      photo,
      fileName: photo === photoFile.value ? photoFile.value.name : 'estudo.jpg',
      date: form.dayOffset > 0 ? localDate(form.dayOffset) : undefined,
      questionsAnswered: g.trackQuestions ? (form.questionsAnswered ?? undefined) : undefined,
      questionsCorrect: g.trackCorrect ? (form.questionsCorrect ?? undefined) : undefined,
      studyMinutes: g.trackTime ? totalMinutes : undefined,
      subject: form.subject.trim() || undefined,
      note: form.note.trim() || undefined,
    })
    await router.push(`/groups/${props.id}`)
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível salvar o registro. Tente novamente.')
  } finally {
    submitting.value = false
  }
}

onMounted(load)
onBeforeUnmount(() => {
  if (photoPreview.value) URL.revokeObjectURL(photoPreview.value)
})
</script>

<template>
  <main class="stack">
    <RouterLink :to="`/groups/${id}`">← Voltar ao grupo</RouterLink>
    <h1>Registrar estudo</h1>

    <p v-if="loadError" class="error" role="alert">{{ loadError }}</p>
    <p v-else-if="!group" class="muted">Carregando...</p>

    <form v-else class="card stack" @submit.prevent="submit">
      <label>
        Foto do estudo <span class="muted">(obrigatória)</span>
        <input type="file" accept="image/jpeg,image/png,image/webp,image/*" required @change="onPhoto" />
      </label>
      <img v-if="photoPreview" :src="photoPreview" alt="Pré-visualização da foto" class="preview" />

      <label>
        Dia
        <select v-model.number="form.dayOffset">
          <option v-for="o in dayOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
        </select>
      </label>

      <label v-if="group.trackQuestions">
        Questões respondidas
        <input v-model.number="form.questionsAnswered" type="number" inputmode="numeric" min="1" max="5000" required />
      </label>

      <label v-if="group.trackCorrect">
        Acertos
        <input
          v-model.number="form.questionsCorrect"
          type="number"
          inputmode="numeric"
          min="0"
          :max="form.questionsAnswered ?? undefined"
          required
        />
      </label>

      <div v-if="group.trackTime" class="row">
        <label class="grow">
          Horas
          <input v-model.number="form.hours" type="number" inputmode="numeric" min="0" max="24" />
        </label>
        <label class="grow">
          Minutos
          <input v-model.number="form.minutes" type="number" inputmode="numeric" min="0" max="59" />
        </label>
      </div>

      <label>
        Matéria <span class="muted">(opcional)</span>
        <input v-model="form.subject" maxlength="80" placeholder="Ex.: Direito Constitucional" />
      </label>

      <label>
        Observação <span class="muted">(opcional)</span>
        <textarea v-model="form.note" rows="2" maxlength="500" />
      </label>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <button type="submit" class="primary" :disabled="submitting">
        {{ submitting ? 'Enviando...' : 'Salvar registro' }}
      </button>
    </form>
  </main>
</template>
