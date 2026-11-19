import { http } from '@/shared/services/http'
import type {
  CalendarDay,
  Challenge,
  CreateChallengeRequest,
  CreateGroupRequest,
  CreateLogInput,
  GroupDetail,
  GroupSummary,
  RankingEntry,
  StudyLog,
} from '../types'

export const groupService = {
  async list(): Promise<GroupSummary[]> {
    const { data } = await http.get<GroupSummary[]>('/api/groups')
    return data
  },

  async create(request: CreateGroupRequest): Promise<GroupSummary> {
    const { data } = await http.post<GroupSummary>('/api/groups', request)
    return data
  },

  async join(code: string): Promise<GroupSummary> {
    const { data } = await http.post<GroupSummary>('/api/groups/join', { code })
    return data
  },

  async get(id: string): Promise<GroupDetail> {
    const { data } = await http.get<GroupDetail>(`/api/groups/${id}`)
    return data
  },

  async ranking(id: string): Promise<RankingEntry[]> {
    const { data } = await http.get<RankingEntry[]>(`/api/groups/${id}/ranking`)
    return data
  },

  async logs(id: string, skip = 0, take = 20): Promise<StudyLog[]> {
    const { data } = await http.get<StudyLog[]>(`/api/groups/${id}/logs`, { params: { skip, take } })
    return data
  },

  async calendar(id: string, month: string, userId?: string): Promise<CalendarDay[]> {
    const { data } = await http.get<CalendarDay[]>(`/api/groups/${id}/calendar`, { params: { month, userId } })
    return data
  },

  async createLog(id: string, input: CreateLogInput): Promise<StudyLog> {
    const form = new FormData()
    form.append('photo', input.photo, input.fileName)
    if (input.date) form.append('date', input.date)
    if (input.questionsAnswered !== undefined) form.append('questionsAnswered', String(input.questionsAnswered))
    if (input.questionsCorrect !== undefined) form.append('questionsCorrect', String(input.questionsCorrect))
    if (input.studyMinutes !== undefined) form.append('studyMinutes', String(input.studyMinutes))
    if (input.subject) form.append('subject', input.subject)
    if (input.note) form.append('note', input.note)

    const { data } = await http.post<StudyLog>(`/api/groups/${id}/logs`, form, { timeout: 60000 })
    return data
  },

  async deleteLog(groupId: string, logId: string): Promise<void> {
    await http.delete(`/api/groups/${groupId}/logs/${logId}`)
  },

  // ---------- desafios (edital + data da prova) ----------

  async challenges(groupId: string): Promise<Challenge[]> {
    const { data } = await http.get<Challenge[]>(`/api/groups/${groupId}/challenges`)
    return data
  },

  async challenge(groupId: string, id: string): Promise<Challenge> {
    const { data } = await http.get<Challenge>(`/api/groups/${groupId}/challenges/${id}`)
    return data
  },

  async createChallenge(groupId: string, request: CreateChallengeRequest): Promise<Challenge> {
    const { data } = await http.post<Challenge>(`/api/groups/${groupId}/challenges`, request)
    return data
  },

  async challengeRanking(groupId: string, id: string): Promise<RankingEntry[]> {
    const { data } = await http.get<RankingEntry[]>(`/api/groups/${groupId}/challenges/${id}/ranking`)
    return data
  },

  async deleteChallenge(groupId: string, id: string): Promise<void> {
    await http.delete(`/api/groups/${groupId}/challenges/${id}`)
  },
}
