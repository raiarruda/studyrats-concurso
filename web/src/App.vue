<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/features/auth/stores/authStore'

const auth = useAuthStore()
const router = useRouter()

async function logout() {
  auth.signOut()
  await router.push('/login')
}
</script>

<template>
  <header v-if="auth.isAuthenticated" class="topbar">
    <RouterLink to="/" class="brand">📚 StudyRats</RouterLink>
    <span class="who">
      {{ auth.user?.name }}
      <button class="link" @click="logout">Sair</button>
    </span>
  </header>
  <RouterView />
</template>
