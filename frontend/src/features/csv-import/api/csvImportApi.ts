import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import { blockKeys } from '@/entities/block'

export function useCsvImportMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (formData: FormData) =>
      apiClient
        .post<string>('/api/blocks/import', formData, {
          headers: { 'Content-Type': 'multipart/form-data' },
        })
        .then((r) => r.data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: blockKeys.all })
    },
  })
}
