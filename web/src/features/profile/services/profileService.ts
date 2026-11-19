import { http } from '@/shared/services/http'
import type { Profile } from '../types'

export const profileService = {
  /** Sem userId, é o perfil de quem está logado. `month` no formato AAAA-MM. */
  async get(month: string, userId?: string): Promise<Profile> {
    const { data } = await http.get<Profile>(userId ? `/api/profile/${userId}` : '/api/profile', {
      params: { month },
    })
    return data
  },
}
