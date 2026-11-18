export interface GroupSummary {
  id: string
  name: string
  code: string
  startDate: string
  endDate: string
  trackQuestions: boolean
  trackCorrect: boolean
  trackTime: boolean
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
}

export interface CreateGroupRequest {
  name: string
  startDate: string
  endDate: string
  trackQuestions: boolean
  trackCorrect: boolean
  trackTime: boolean
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
