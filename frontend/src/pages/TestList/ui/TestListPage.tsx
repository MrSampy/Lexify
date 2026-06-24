import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import {
  Button,
  Spinner,
  Badge,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/ui'
import { useTests, useDeleteTestMutation } from '@/entities/test'
import type { TestStatus } from '@/entities/test'
import { formatDate } from '@/shared/lib'

const STATUS_LABELS: Record<TestStatus, string> = {
  generating: 'Generating',
  ready: 'Ready',
  archived: 'Archived',
}

const STATUS_VARIANTS: Record<TestStatus, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  generating: 'secondary',
  ready: 'default',
  archived: 'outline',
}

export function TestListPage() {
  const navigate = useNavigate()
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [page, setPage] = useState(1)

  const status = statusFilter === 'all' ? undefined : statusFilter
  const { data, isLoading, isError } = useTests(status, page)
  const deleteTest = useDeleteTestMutation()

  const handleDelete = async (id: string, title: string) => {
    if (!confirm(`Archive test "${title}"?`)) return
    await deleteTest.mutateAsync(id)
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-4xl px-4 py-8">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <h1 className="text-2xl font-bold">Tests</h1>
          <Button onClick={() => navigate(ROUTES.TEST_CREATE)}>+ New test</Button>
        </div>

        {/* Filter */}
        <div className="mb-4 w-48">
          <Select
            value={statusFilter}
            onValueChange={(v) => {
              if (v) setStatusFilter(v)
              setPage(1)
            }}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All statuses</SelectItem>
              <SelectItem value="generating">Generating</SelectItem>
              <SelectItem value="ready">Ready</SelectItem>
              <SelectItem value="archived">Archived</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Content */}
        {isLoading && (
          <div className="flex justify-center py-16">
            <Spinner size="lg" />
          </div>
        )}

        {isError && (
          <p className="py-8 text-center text-sm text-muted-foreground">Failed to load tests.</p>
        )}

        {data && data.items.length === 0 && (
          <div className="py-16 text-center">
            <p className="mb-4 text-muted-foreground">No tests yet.</p>
            <Button onClick={() => navigate(ROUTES.TEST_CREATE)}>Create your first test</Button>
          </div>
        )}

        {data && data.items.length > 0 && (
          <div className="space-y-3">
            {data.items.map((test) => (
              <div
                key={test.id}
                className="flex items-center justify-between rounded-lg border bg-card p-4 shadow-sm"
              >
                <div className="min-w-0 flex-1">
                  <div className="mb-1 flex items-center gap-2">
                    <span className="truncate font-medium">{test.title}</span>
                    <Badge variant={STATUS_VARIANTS[test.status]}>
                      {STATUS_LABELS[test.status]}
                    </Badge>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    {test.questionCount != null ? `${test.questionCount} questions · ` : ''}
                    {formatDate(test.createdAt)}
                  </p>
                </div>
                <div className="ml-4 flex shrink-0 gap-2">
                  {test.status === 'ready' && (
                    <Link to={ROUTES.TEST_RUNNER(test.id)}>
                      <Button size="sm">Run</Button>
                    </Link>
                  )}
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => void handleDelete(test.id, test.title)}
                    disabled={deleteTest.isPending}
                  >
                    Delete
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className="mt-6 flex items-center justify-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasPreviousPage}
              onClick={() => setPage((p) => p - 1)}
            >
              Previous
            </Button>
            <span className="text-sm text-muted-foreground">
              {page} / {data.totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </Button>
          </div>
        )}
      </div>
    </div>
  )
}
