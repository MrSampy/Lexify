import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import type { PagedResult } from '@/shared/api'
import type { Test, TestListItem, GenerateTestInput } from '../model/types'

export const testKeys = {
  all: ['tests'] as const,
  list: (status?: string, page?: number) => [...testKeys.all, 'list', { status, page }] as const,
  detail: (id: string) => [...testKeys.all, 'detail', id] as const,
}

export const testApi = {
  getTests: (status?: string, page = 1, pageSize = 10) =>
    apiClient
      .get<PagedResult<TestListItem>>('/api/tests', { params: { status, page, pageSize } })
      .then((r) => r.data),

  getTestById: (id: string) => apiClient.get<Test>(`/api/tests/${id}`).then((r) => r.data),

  generateTest: (input: GenerateTestInput) =>
    apiClient
      .post<{ testId: string; status: string }>('/api/tests/generate', input)
      .then((r) => r.data),

  deleteTest: (id: string) => apiClient.delete(`/api/tests/${id}`).then((r) => r.data),
}

export function useTests(status?: string, page = 1) {
  return useQuery({
    queryKey: testKeys.list(status, page),
    queryFn: () => testApi.getTests(status, page),
  })
}

export function useTest(id: string) {
  return useQuery({
    queryKey: testKeys.detail(id),
    queryFn: () => testApi.getTestById(id),
    enabled: !!id,
  })
}

export function useGenerateTestMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: testApi.generateTest,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: testKeys.all })
    },
  })
}

export function useDeleteTestMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: testApi.deleteTest,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: testKeys.all })
    },
  })
}
