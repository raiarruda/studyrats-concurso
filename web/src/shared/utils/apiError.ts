import axios from 'axios'

interface ProblemDetails {
  detail?: string
  errors?: Record<string, string[]>
}

/** Traduz um erro de HTTP em uma frase para o usuário. */
export function apiErrorMessage(e: unknown, fallback: string): string {
  if (!axios.isAxiosError(e)) return fallback
  if (!e.response) return 'Sem conexão com o servidor. Verifique sua internet.'

  if (e.response.status === 429) return 'Muitas tentativas. Aguarde um minuto e tente de novo.'

  const data = e.response.data as ProblemDetails | undefined
  if (data?.detail) return data.detail
  const firstValidation = data?.errors ? Object.values(data.errors)[0]?.[0] : undefined
  return firstValidation ?? fallback
}
