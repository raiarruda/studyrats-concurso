export interface CalendarDay {
  date: string
  logsCount: number
  photoUrl: string
  questions: number | null
  minutes: number | null
}

export interface Profile {
  userId: string
  name: string
  isMe: boolean
  totalLogs: number
  activeDays: number
  totalMinutes: number | null
  totalQuestions: number | null
  currentStreak: number
  firstActiveDate: string | null
  days: CalendarDay[]
}
