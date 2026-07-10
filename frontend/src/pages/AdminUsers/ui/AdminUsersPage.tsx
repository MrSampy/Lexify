import { useState } from 'react'
import { toast } from 'sonner'
import { debounce } from '@/shared/lib'
import { useConfirm } from '@/shared/ui'
import { useAuthStore } from '@/entities/user'
import {
  useAdminUsers,
  useSuspendUserMutation,
  useRestoreUserMutation,
  useDeleteUserMutation,
  useChangeRoleMutation,
} from '@/entities/admin'
import type { AdminUser, AdminUsersParams } from '@/entities/admin'
import { UsersTable, UserDetailModal } from '@/features/admin-users'

const PAGE_SIZE = 20

export function AdminUsersPage() {
  const currentUserRole = useAuthStore((s) => s.user?.role)
  const canManageUsers = currentUserRole === 'admin'
  const [page, setPage] = useState(1)
  const [role, setRole] = useState<string>('')
  const [status, setStatus] = useState<string>('')
  const [email, setEmail] = useState('')
  const [emailInput, setEmailInput] = useState('')
  const [selectedUser, setSelectedUser] = useState<AdminUser | null>(null)

  const debouncedSetEmail = debounce((v: string) => {
    setEmail(v)
    setPage(1)
  }, 400)

  const params: AdminUsersParams = {
    page,
    pageSize: PAGE_SIZE,
    role: role || undefined,
    status: status || undefined,
    email: email || undefined,
  }

  const { data, isLoading } = useAdminUsers(params)
  const suspend = useSuspendUserMutation()
  const restore = useRestoreUserMutation()
  const deleteUser = useDeleteUserMutation()
  const changeRole = useChangeRoleMutation()
  const { confirm, confirmDialog } = useConfirm()

  const handleDelete = async (id: string, userEmail: string) => {
    if (
      !(await confirm({
        title: `Delete user "${userEmail}"?`,
        description: 'This cannot be undone.',
      }))
    )
      return
    try {
      await deleteUser.mutateAsync(id)
      toast.success('User deleted')
    } catch {
      toast.error('Failed to delete user')
    }
  }

  return (
    <div>
      <h1 className="ds-h2" style={{ margin: '0 0 20px' }}>
        Users
      </h1>

      {/* Filters */}
      <div style={{ display: 'flex', gap: 10, marginBottom: 16, flexWrap: 'wrap' }}>
        <input
          className="lx-input"
          placeholder="search by email…"
          value={emailInput}
          onChange={(e) => {
            setEmailInput(e.target.value)
            debouncedSetEmail(e.target.value)
          }}
          style={{ width: 220, height: 36, fontSize: 13 }}
        />
        <select
          className="lx-input"
          value={role}
          onChange={(e) => {
            setRole(e.target.value === 'all' ? '' : e.target.value)
            setPage(1)
          }}
          style={{ width: 140, height: 36, fontSize: 13, cursor: 'pointer' }}
        >
          <option value="all">All roles</option>
          <option value="user">User</option>
          <option value="moderator">Moderator</option>
          <option value="admin">Admin</option>
        </select>
        <select
          className="lx-input"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value === 'all' ? '' : e.target.value)
            setPage(1)
          }}
          style={{ width: 150, height: 36, fontSize: 13, cursor: 'pointer' }}
        >
          <option value="all">All statuses</option>
          <option value="active">Active</option>
          <option value="suspended">Suspended</option>
          <option value="deleted">Deleted</option>
        </select>
      </div>

      <UsersTable
        users={data?.items ?? []}
        isLoading={isLoading}
        canManageUsers={canManageUsers}
        onSuspend={(id) => void suspend.mutateAsync(id)}
        onRestore={(id) => void restore.mutateAsync(id)}
        onDelete={(id, email) => void handleDelete(id, email)}
        onRoleChange={(id, r) => void changeRole.mutateAsync({ id, role: r })}
        onView={(user) => setSelectedUser(user)}
      />

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 12,
            marginTop: 16,
          }}
        >
          <button
            className="lx-btn-secondary"
            style={{ padding: '6px 16px', fontSize: 12 }}
            disabled={!data.hasPreviousPage}
            onClick={() => setPage((p) => p - 1)}
          >
            ← Previous
          </button>
          <span style={{ color: 'var(--fg-3)', fontSize: 12, fontWeight: 600 }}>
            {page} / {data.totalPages}
          </span>
          <button
            className="lx-btn-secondary"
            style={{ padding: '6px 16px', fontSize: 12 }}
            disabled={!data.hasNextPage}
            onClick={() => setPage((p) => p + 1)}
          >
            Next →
          </button>
        </div>
      )}

      <UserDetailModal
        user={selectedUser}
        open={!!selectedUser}
        canManageUsers={canManageUsers}
        onClose={() => setSelectedUser(null)}
        onSuspend={(id) => void suspend.mutateAsync(id)}
        onRestore={(id) => void restore.mutateAsync(id)}
        onDelete={(id, email) => void handleDelete(id, email)}
      />
      {confirmDialog}
    </div>
  )
}
