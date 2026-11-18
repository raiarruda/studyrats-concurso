import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { AuthResponse, User } from '../types'

function readUser(): User | null {
  try {
    const raw = localStorage.getItem('user')
    return raw ? (JSON.parse(raw) as User) : null
  } catch {
    return null
  }
}

// Único store do app: o usuário logado é lido por várias telas e pelo guard do router.
export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const user = ref<User | null>(readUser())

  const isAuthenticated = computed(() => !!token.value)

  function signIn(auth: AuthResponse) {
    token.value = auth.token
    user.value = auth.user
    localStorage.setItem('token', auth.token)
    localStorage.setItem('user', JSON.stringify(auth.user))
  }

  function signOut() {
    token.value = null
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  return { token, user, isAuthenticated, signIn, signOut }
})
