export interface GroupSummary {
  id: string
  name: string
  code: string
  trackQuestions: boolean
  trackCorrect: boolean
  trackTime: boolean
  minDaysPerWeek: number
  createdAt: string
  memberCount: number
  isOwner: boolean
}

export interface Member {
  userId: string
  name: string
  isOwner: boolean
}

export interface GroupDetail {
  group: GroupSummary
  members: Member[]
}

/** Preparação do grupo para um edital, com data de prova. */
export interface Challenge {
  id: string
  groupId: string
  name: string
  startDate: string
  examDate: string
}

export interface StudyLog {
  id: string
  userId: string
  userName: string
  date: string
  photoUrl: string
  questionsAnswered: number | null
  questionsCorrect: number | null
  studyMinutes: number | null
  subject: string | null
  note: string | null
  createdAt: string
}

export interface RankingEntry {
  userId: string
  name: string
  totalQuestions: number | null
  totalCorrect: number | null
  accuracyPercent: number | null
  totalMinutes: number | null
  daysStudied: number
  currentStreak: number
  currentWeekStreak: number
  bestWeekStreak: number
}

export interface CalendarDay {
  date: string
  logsCount: number
  photoUrl: string
  questions: number | null
  minutes: number | null
}

export interface CreateGroupRequest {
  name: string
  trackQuestions: boolean
  trackCorrect: boolean
  trackTime: boolean
  minDaysPerWeek: number
}

export interface CreateChallengeRequest {
  name: string
  startDate: string
  examDate: string
}

export interface CreateLogInput {
  photo: Blob
  fileName: string
  date?: string
  questionsAnswered?: number
  questionsCorrect?: number
  studyMinutes?: number
  subject?: string
  note?: string
}
