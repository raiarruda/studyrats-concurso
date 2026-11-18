import axios from 'axios'

// Em dev o Vite faz proxy de /api e /uploads; em produção, defina VITE_API_URL se a API ficar em outro domínio.
export const apiBase: string = import.meta.env.VITE_API_URL ?? ''

export const http = axios.create({
  baseURL: apiBase,
  timeout: 20000,
})

http.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

http.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    // Token expirado/inválido em qualquer chamada que não seja o próprio login: volta para a tela de entrada.
    if (axios.isAxiosError(error) && error.response?.status === 401 && !error.config?.url?.includes('/auth/')) {
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      if (location.pathname !== '/login') location.assign('/login')
    }
    return Promise.reject(error)
  },
)

/** Fotos vêm como caminho relativo (/uploads/...). */
export const assetUrl = (path: string) => apiBase + path
