import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Spinner } from '@/shared/ui'
import { useTests, useDeleteTestMutation } from '@/entities/test'
import type { TestStatus } from '@/entities/test'
import { formatDate } from '@/shared/lib'

const STATUS_STYLES: Record<
  TestStatus,
  { bg: string; color: string; border: string; label: string }
> = {
  generating: {
    bg: 'var(--warning-ghost)',
    color: 'var(--warning)',
    border: 'rgba(245,181,61,0.3)',
    label: 'Generating',
  },
  ready: {
    bg: 'var(--success-ghost)',
    color: 'var(--success)',
    border: 'rgba(63,214,139,0.3)',
    label: 'Ready',
  },
  archived: { bg: 'var(--bg-3)', color: 'var(--fg-3)', border: 'var(--line-2)', label: 'Archived' },
}

export function TestListPage() {
  const navigate = useNavigate()
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [page, setPage] = useState(1)

  const status = statusFilter === 'all' ? undefined : statusFilter
  const { data, isLoading, isError } = useTests(status, page)
  const deleteTest = useDeleteTestMutation()

  const handleDelete = async (id: string, title: string) => {
    if (!confirm(`Delete test "${title}"?`)) return
    await deleteTest.mutateAsync(id)
  }

  return (
    <div>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'end',
          justifyContent: 'space-between',
          gap: 16,
          flexWrap: 'wrap',
          marginBottom: 22,
        }}
      >
        <div>
          <h1 className="ds-h2" style={{ margin: '0 0 4px' }}>
            Tests
          </h1>
          <p className="ds-body" style={{ margin: 0, color: 'var(--fg-3)' }}>
            AI-generated quizzes across your blocks.
          </p>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          {/* Status filter */}
          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value)
              setPage(1)
            }}
            style={{
              padding: '9px 14px',
              background: 'var(--bg-2)',
              border: '1px solid var(--line-2)',
              borderRadius: 'var(--r-md)',
              color: 'var(--fg-1)',
              fontSize: 13,
              fontFamily: 'var(--font-body)',
              cursor: 'pointer',
              outline: 'none',
            }}
          >
            <option value="all">all ▾</option>
            <option value="generating">generating</option>
            <option value="ready">ready</option>
            <option value="archived">archived</option>
          </select>
          <button className="lx-btn-primary" onClick={() => navigate(ROUTES.TEST_CREATE)}>
            + New test
          </button>
        </div>
      </div>

      {/* Content */}
      {isLoading && (
        <div style={{ display: 'flex', justifyContent: 'center', padding: '64px 0' }}>
          <Spinner size="lg" />
        </div>
      )}

      {isError && (
        <p
          className="ds-sm"
          style={{ textAlign: 'center', padding: '32px 0', color: 'var(--fg-3)' }}
        >
          Failed to load tests.
        </p>
      )}

      {data && data.items.length === 0 && (
        <div style={{ textAlign: 'center', padding: '64px 0' }}>
          <p className="ds-body" style={{ color: 'var(--fg-3)', marginBottom: 16 }}>
            No tests yet.
          </p>
          <button className="lx-btn-primary" onClick={() => navigate(ROUTES.TEST_CREATE)}>
            Create your first test
          </button>
        </div>
      )}

      {data && data.items.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {data.items.map((test) => {
            const s = STATUS_STYLES[test.status] ?? STATUS_STYLES.archived
            return (
              <div
                key={test.id}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 18,
                  padding: '18px 20px',
                  background: 'var(--bg-2)',
                  border: '1px solid var(--line-2)',
                  borderRadius: 'var(--r-lg)',
                  flexWrap: 'wrap',
                }}
              >
                <div style={{ flex: 1, minWidth: 180 }}>
                  <div
                    className="ds-h4"
                    style={{ color: 'var(--fg-1)', fontSize: 16, marginBottom: 4 }}
                  >
                    {test.title}
                  </div>
                  <div className="ds-sm" style={{ color: 'var(--fg-3)' }}>
                    {test.questionCount != null ? `${test.questionCount} questions · ` : ''}
                    {formatDate(test.createdAt)}
                  </div>
                </div>

                <span
                  style={{
                    fontFamily: 'var(--font-body)',
                    fontSize: 11,
                    fontWeight: 700,
                    padding: '4px 12px',
                    borderRadius: 'var(--r-pill)',
                    background: s.bg,
                    color: s.color,
                    border: `1px solid ${s.border}`,
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: 6,
                  }}
                >
                  {test.status === 'generating' && (
                    <span
                      style={{
                        display: 'inline-block',
                        animation: 'spin 1s linear infinite',
                        fontSize: 10,
                      }}
                    >
                      ⟳
                    </span>
                  )}
                  {s.label}
                </span>

                <div style={{ display: 'flex', gap: 8 }}>
                  {test.status === 'ready' && (
                    <Link to={ROUTES.TEST_RUNNER(test.id)} style={{ textDecoration: 'none' }}>
                      <button className="lx-btn-primary" style={{ padding: '9px 18px' }}>
                        Run →
                      </button>
                    </Link>
                  )}
                  <button
                    className="lx-btn-secondary"
                    style={{ padding: '9px 14px', color: 'var(--fg-3)' }}
                    onClick={() => void handleDelete(test.id, test.title)}
                    disabled={deleteTest.isPending}
                  >
                    Delete
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div
          style={{
            marginTop: 24,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 12,
          }}
        >
          <button
            className="lx-btn-secondary"
            disabled={!data.hasPreviousPage}
            onClick={() => setPage((p) => p - 1)}
            style={{ padding: '8px 16px' }}
          >
            Previous
          </button>
          <span className="ds-code" style={{ color: 'var(--fg-3)' }}>
            {page} / {data.totalPages}
          </span>
          <button
            className="lx-btn-secondary"
            disabled={!data.hasNextPage}
            onClick={() => setPage((p) => p + 1)}
            style={{ padding: '8px 16px' }}
          >
            Next
          </button>
        </div>
      )}
    </div>
  )
}
