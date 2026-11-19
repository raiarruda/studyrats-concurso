<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { localDate } from '@/shared/utils/format'
import { groupService } from '../services/groupService'

const props = defineProps<{ id: string }>()
const router = useRouter()

const form = reactive({
  name: '',
  startDate: localDate(),
  examDate: '',
})

const loading = ref(false)
const error = ref<string | null>(null)

async function submit() {
  loading.value = true
  error.value = null
  try {
    const challenge = await groupService.createChallenge(props.id, { ...form })
    await router.push(`/groups/${props.id}/challenges/${challenge.id}`)
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível criar o desafio.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="stack">
    <RouterLink :to="`/groups/${id}`">← Voltar ao grupo</RouterLink>
    <h1>Novo desafio</h1>
    <p class="muted" style="margin: 0">
      O desafio é a preparação do grupo para um edital específico. O ranking dele conta os registros de estudo
      entre o início e a data da prova.
    </p>

    <form class="card stack" @submit.prevent="submit">
      <label>
        Edital ou concurso
        <input v-model="form.name" maxlength="80" minlength="2" placeholder="Ex.: Edital da Prefeitura 2027" required />
      </label>

      <div class="row">
        <label class="grow">
          Início
          <input v-model="form.startDate" type="date" required />
        </label>
        <label class="grow">
          Data da prova
          <input v-model="form.examDate" type="date" :min="form.startDate" required />
        </label>
      </div>

      <small class="muted">
        Se o início for no passado, os registros que o grupo já fez desde essa data entram no ranking do desafio.
      </small>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <button type="submit" class="primary" :disabled="loading">
        {{ loading ? 'Criando...' : 'Criar desafio' }}
      </button>
    </form>
  </main>
</template>
