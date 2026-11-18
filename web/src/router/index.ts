import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/features/auth/stores/authStore'
import LoginPage from '@/features/auth/pages/LoginPage.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: LoginPage },
    {
      path: '/',
      component: () => import('@/features/groups/pages/GroupListPage.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/groups/new',
      component: () => import('@/features/groups/pages/GroupCreatePage.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/groups/:id',
      component: () => import('@/features/groups/pages/GroupPage.vue'),
      meta: { requiresAuth: true },
      props: true,
    },
    {
      path: '/groups/:id/log',
      component: () => import('@/features/groups/pages/NewLogPage.vue'),
      meta: { requiresAuth: true },
      props: true,
    },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isAuthenticated) return '/login'
  if (to.path === '/login' && auth.isAuthenticated) return '/'
})
