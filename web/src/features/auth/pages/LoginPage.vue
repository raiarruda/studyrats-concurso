<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { apiErrorMessage } from '@/shared/utils/apiError'
import { authService } from '../services/authService'
import { useAuthStore } from '../stores/authStore'

const router = useRouter()
const auth = useAuthStore()

const mode = ref<'login' | 'register'>('login')
const form = reactive({ name: '', email: '', password: '' })
const loading = ref(false)
const error = ref<string | null>(null)

async function submit() {
  loading.value = true
  error.value = null
  try {
    const result =
      mode.value === 'login'
        ? await authService.login(form.email, form.password)
        : await authService.register(form.name, form.email, form.password)
    auth.signIn(result)
    await router.push('/')
  } catch (e) {
    error.value = apiErrorMessage(e, 'Não foi possível entrar. Tente novamente.')
  } finally {
    loading.value = false
  }
}

function toggleMode() {
  mode.value = mode.value === 'login' ? 'register' : 'login'
  error.value = null
}
</script>

<template>
  <main class="auth">
    <h1>📚 StudyRats</h1>
    <p class="muted">Desafios de estudo para concurseiros, entre amigos.</p>

    <form class="card stack" @submit.prevent="submit">
      <h2>{{ mode === 'login' ? 'Entrar' : 'Criar conta' }}</h2>

      <label v-if="mode === 'register'">
        Nome
        <input v-model="form.name" autocomplete="name" minlength="2" maxlength="80" required />
      </label>

      <label>
        E-mail
        <input v-model="form.email" type="email" autocomplete="email" required />
      </label>

      <label>
        Senha
        <input
          v-model="form.password"
          type="password"
          :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
          :minlength="mode === 'register' ? 8 : undefined"
          required
        />
        <small v-if="mode === 'register'" class="muted">Mínimo de 8 caracteres.</small>
      </label>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <button type="submit" class="primary" :disabled="loading">
        {{ loading ? 'Aguarde...' : mode === 'login' ? 'Entrar' : 'Criar conta' }}
      </button>

      <button type="button" class="link" @click="toggleMode">
        {{ mode === 'login' ? 'Ainda não tenho conta' : 'Já tenho conta' }}
      </button>
    </form>
  </main>
</template>
