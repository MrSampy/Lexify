import { useState } from 'react'
import {
  Button,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/ui'
import { debounce } from '@/shared/lib'
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

  const handleDelete = (id: string, userEmail: string) => {
    if (!confirm(`Delete user "${userEmail}"? This cannot be undone.`)) return
    void deleteUser.mutateAsync(id)
  }

  return (
    <div className="p-8">
      <h1 className="mb-6 text-2xl font-bold">Users</h1>

      {/* Filters */}
      <div className="mb-4 flex flex-wrap gap-3">
        <Input
          placeholder="Search by email…"
          value={emailInput}
          onChange={(e) => {
            setEmailInput(e.target.value)
            debouncedSetEmail(e.target.value)
          }}
          className="w-56"
        />
        <Select
          value={role}
          onValueChange={(v) => {
            setRole(!v || v === 'all' ? '' : v)
            setPage(1)
          }}
        >
          <SelectTrigger className="w-36">
            <SelectValue placeholder="All roles" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All roles</SelectItem>
            <SelectItem value="user">User</SelectItem>
            <SelectItem value="moderator">Moderator</SelectItem>
            <SelectItem value="admin">Admin</SelectItem>
          </SelectContent>
        </Select>
        <Select
          value={status}
          onValueChange={(v) => {
            setStatus(!v || v === 'all' ? '' : v)
            setPage(1)
          }}
        >
          <SelectTrigger className="w-36">
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            <SelectItem value="active">Active</SelectItem>
            <SelectItem value="suspended">Suspended</SelectItem>
            <SelectItem value="deleted">Deleted</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <UsersTable
        users={data?.items ?? []}
        isLoading={isLoading}
        onSuspend={(id) => void suspend.mutateAsync(id)}
        onRestore={(id) => void restore.mutateAsync(id)}
        onDelete={handleDelete}
        onRoleChange={(id, r) => void changeRole.mutateAsync({ id, role: r })}
        onView={(user) => setSelectedUser(user)}
      />

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="mt-4 flex items-center justify-center gap-2">
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

      <UserDetailModal
        user={selectedUser}
        open={!!selectedUser}
        onClose={() => setSelectedUser(null)}
        onSuspend={(id) => void suspend.mutateAsync(id)}
        onRestore={(id) => void restore.mutateAsync(id)}
        onDelete={handleDelete}
      />
    </div>
  )
}
