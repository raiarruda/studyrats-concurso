<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { groupService } from '../services/groupService'

const router = useRouter()

const form = reactive({
  name: '',
  trackQuestions: true,
  trackCorrect: false,
  trackTime: true,
  minDaysPerWeek: 3,
})

const loading = ref(false)
const error = ref<string | null>(null)

// Acertos só fazem sentido sobre o total de questões: marcar acertos liga questões e a trava.
const correctLocksQuestions = computed(() => form.trackCorrect)

function onCorrectChange() {
  if (form.trackCorrect) form.trackQuestions = true
}

async function submit() {
  error.value = null
  if (!form.trackQuestions && !form.trackCorrect && !form.trackTime) {
    error.value = 'Escolha pelo menos uma métrica para o grupo acompanhar.'
    return
  }

  loading.value = true
  try {
    const group = await groupService.create({ ...form })
    await router.push(`/groups/${group.id}`)
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível criar o grupo.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="stack">
    <RouterLink to="/">← Voltar</RouterLink>
    <h1>Novo grupo</h1>
    <p class="muted" style="margin: 0">
      O grupo é a turma, e não tem data de prova. Quando vocês forem estudar para um edital específico,
      crie um <b>desafio</b> dentro do grupo.
    </p>

    <form class="card stack" @submit.prevent="submit">
      <label>
        Nome do grupo
        <input v-model="form.name" maxlength="80" minlength="2" placeholder="Ex.: Turma da madrugada" required />
      </label>

      <fieldset class="stack" style="border: 0; padding: 0; margin: 0">
        <legend style="font-weight: 600; font-size: 0.9rem; padding: 0; margin-bottom: 8px">
          O que o grupo acompanha?
        </legend>

        <label class="check">
          <input v-model="form.trackQuestions" type="checkbox" :disabled="correctLocksQuestions" />
          <span>
            Questões respondidas
            <small class="muted">Quantas questões cada um resolveu.</small>
          </span>
        </label>

        <label class="check">
          <input v-model="form.trackCorrect" type="checkbox" @change="onCorrectChange" />
          <span>
            Acertos
            <small class="muted">Calcula o % de acerto. Exige contar questões também.</small>
          </span>
        </label>

        <label class="check">
          <input v-model="form.trackTime" type="checkbox" />
          <span>
            Tempo de estudo
            <small class="muted">Minutos estudados por dia.</small>
          </span>
        </label>

        <small class="muted">
          📷 A foto é sempre obrigatória. As métricas não podem ser alteradas depois que o grupo é criado.
        </small>
      </fieldset>

      <label>
        Semana ativa: mínimo de dias estudados
        <select v-model.number="form.minDaysPerWeek">
          <option v-for="n in 7" :key="n" :value="n">
            {{ n }} {{ n === 1 ? 'dia' : 'dias' }} por semana{{ n === 3 ? ' (padrão)' : '' }}
          </option>
        </select>
        <small class="muted">
          Uma semana (segunda a domingo) com esse mínimo conta para a sequência de semanas ativas.
        </small>
      </label>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <button type="submit" class="primary" :disabled="loading">
        {{ loading ? 'Criando...' : 'Criar grupo' }}
      </button>
    </form>
  </main>
</template>
