const DAY = 86_400_000

const utc = (iso: string) => {
  const [y, m, d] = iso.split('-').map(Number)
  return Date.UTC(y!, m! - 1, d)
}

/**
 * Quantas semanas inteiras (segunda a domingo) cabem entre o início e a prova.
 * Espelha a regra do servidor: só elas valem multa no bolão.
 */
export function countFullWeeks(start: string, exam: string): number {
  if (!start || !exam) return 0
  const end = utc(exam)
  const startDay = new Date(utc(start)).getUTCDay() // 0 = domingo
  let monday = utc(start) + (startDay === 1 ? 0 : (8 - startDay) % 7) * DAY

  let weeks = 0
  for (; monday + 6 * DAY <= end; monday += 7 * DAY) weeks++
  return weeks
}
