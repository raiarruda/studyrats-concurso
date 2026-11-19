<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { formatMoney, localDate } from '@/shared/utils/format'
import { groupService } from '../services/groupService'
import { countFullWeeks } from '../utils/weeks'

const props = defineProps<{ id: string }>()
const router = useRouter()

const DEFAULT_PERCENTS: Record<number, number[]> = { 2: [70, 30], 3: [60, 30, 10] }

const form = reactive({
  name: '',
  startDate: localDate(),
  examDate: '',
})

// Bolão (simulado): multa por semana falhada e divisão do valor arrecadado.
const bet = reactive({
  enabled: false,
  penalty: 10, // em reais
  places: 2,
  percents: [...DEFAULT_PERCENTS[2]!],
})

const minDays = ref<number | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

const fullWeeks = computed(() => countFullWeeks(form.startDate, form.examDate))
const percentSum = computed(() => bet.percents.reduce((sum, p) => sum + (Number(p) || 0), 0))
const caixa = computed(() => 100 - percentSum.value)
const penaltyCents = computed(() => Math.round((Number(bet.penalty) || 0) * 100))

watch(
  () => bet.places,
  (places) => (bet.percents = [...DEFAULT_PERCENTS[places]!]),
)

async function submit() {
  error.value = null

  if (bet.enabled) {
    if (penaltyCents.value < 1) {
      error.value = 'Informe um valor de multa maior que zero.'
      return
    }
    if (bet.percents.some((p) => !(Number(p) >= 1))) {
      error.value = 'Cada colocado precisa receber pelo menos 1% do prêmio.'
      return
    }
    if (percentSum.value > 100) {
      error.value = 'A soma das porcentagens do prêmio não pode passar de 100%.'
      return
    }
    if (form.examDate && fullWeeks.value === 0) {
      error.value = 'Para valer multa, o desafio precisa ter ao menos uma semana inteira (segunda a domingo).'
      return
    }
  }

  loading.value = true
  try {
    const challenge = await groupService.createChallenge(props.id, {
      name: form.name,
      startDate: form.startDate,
      examDate: form.examDate,
      ...(bet.enabled ? { penaltyCents: penaltyCents.value, prizePercents: bet.percents.map(Number) } : {}),
    })
    await router.push(`/groups/${props.id}/challenges/${challenge.id}`)
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível criar o desafio.')
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  try {
    minDays.value = (await groupService.get(props.id)).group.minDaysPerWeek
  } catch {
    // Só ilustra o texto de ajuda; se falhar, o formulário segue funcionando.
  }
})
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

      <hr class="divider" />

      <label class="check">
        <input v-model="bet.enabled" type="checkbox" />
        <span>
          💰 Bolão simulado
          <small class="muted">
            Quem fica abaixo da meta semanal “paga” uma multa, e o valor arrecadado é dividido entre os primeiros
            colocados no fim. O app só calcula e mostra: <b>não movimenta dinheiro</b>.
          </small>
        </span>
      </label>

      <template v-if="bet.enabled">
        <label>
          Multa por semana falhada (R$)
          <input v-model.number="bet.penalty" type="number" inputmode="decimal" min="0.5" max="1000" step="0.5" required />
          <small class="muted">
            Semana falhada = menos de {{ minDays ?? 'N' }} dias de estudo (o mínimo do grupo). Só contam semanas
            inteiras, de segunda a domingo, dentro do desafio.
          </small>
        </label>

        <p v-if="form.examDate" class="muted" style="margin: 0">
          Este desafio tem <b>{{ fullWeeks }}</b> {{ fullWeeks === 1 ? 'semana inteira' : 'semanas inteiras' }}
          {{ fullWeeks === 0 ? '— aumente o período para valer multa.' : `(até ${formatMoney(fullWeeks * penaltyCents)} de multa por pessoa).` }}
        </p>

        <label>
          Quantos colocados recebem?
          <select v-model.number="bet.places">
            <option :value="2">2 colocados (1º e 2º)</option>
            <option :value="3">3 colocados (1º, 2º e 3º)</option>
          </select>
        </label>

        <fieldset class="stack" style="border: 0; padding: 0; margin: 0">
          <legend style="font-weight: 600; font-size: 0.9rem; padding: 0; margin-bottom: 8px">
            Divisão do valor arrecadado (%)
          </legend>
          <div class="row">
            <label v-for="(_, i) in bet.percents" :key="i" class="grow">
              {{ i + 1 }}º lugar
              <input v-model.number="bet.percents[i]" type="number" inputmode="numeric" min="1" max="100" required />
            </label>
          </div>
          <small :class="percentSum > 100 ? 'error' : 'muted'">
            Soma: {{ percentSum }}%.
            <template v-if="percentSum > 100">Passou de 100%.</template>
            <template v-else-if="caixa > 0">Os {{ caixa }}% restantes ficam no caixa do grupo.</template>
          </small>
        </fieldset>

        <small class="muted">
          Cada pessoa escolhe se quer participar do bolão, e só paga as semanas inteiras depois de entrar.
          As regras não podem ser alteradas depois que o desafio é criado.
        </small>
      </template>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <button type="submit" class="primary" :disabled="loading">
        {{ loading ? 'Criando...' : 'Criar desafio' }}
      </button>
    </form>
  </main>
</template>
