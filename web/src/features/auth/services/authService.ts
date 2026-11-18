import { http } from '@/shared/services/http'
import type { AuthResponse } from '../types'

export const authService = {
  async login(email: string, password: string): Promise<AuthResponse> {
    const { data } = await http.post<AuthResponse>('/api/auth/login', { email, password })
    return data
  },

  async register(name: string, email: string, password: string): Promise<AuthResponse> {
    const { data } = await http.post<AuthResponse>('/api/auth/register', { name, email, password })
    return data
  },
}
