/** 90 -> "1h30", 45 -> "45min", 120 -> "2h". */
export function formatMinutes(total: number): string {
  const h = Math.floor(total / 60)
  const m = total % 60
  if (h === 0) return `${m}min`
  return m === 0 ? `${h}h` : `${h}h${String(m).padStart(2, '0')}`
}

/** "2026-11-18" -> "18/11/2026" (sem passar por Date, para não deslocar pelo fuso). */
export function formatDate(iso: string): string {
  const [y, m, d] = iso.split('-')
  return `${d}/${m}/${y}`
}

/** Data local do aparelho no formato yyyy-MM-dd, deslocada por N dias. */
export function localDate(offsetDays = 0): string {
  const d = new Date()
  d.setDate(d.getDate() - offsetDays)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${mm}-${dd}`
}

/** Dias entre duas datas yyyy-MM-dd (b - a). Em UTC, para o horário de verão nunca deslocar um dia. */
export function daysBetween(a: string, b: string): number {
  const ms = (iso: string) => {
    const [y, m, d] = iso.split('-').map(Number)
    return Date.UTC(y!, m! - 1, d)
  }
  return Math.round((ms(b) - ms(a)) / 86_400_000)
}

/** yyyy-MM do dia de hoje no aparelho. */
export function currentMonth(): string {
  return localDate().slice(0, 7)
}

/** "2026-11" -> "novembro de 2026". */
export function monthLabel(month: string): string {
  const [y, m] = month.split('-').map(Number)
  return new Date(y!, m! - 1, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
}

export function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}
