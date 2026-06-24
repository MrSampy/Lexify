import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Badge,
  Button,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Spinner,
} from '@/shared/ui'
import { formatDate } from '@/shared/lib'
import type { AdminUser } from '@/entities/admin'

interface UsersTableProps {
  users: AdminUser[]
  isLoading: boolean
  onSuspend: (id: string) => void
  onRestore: (id: string) => void
  onDelete: (id: string, email: string) => void
  onRoleChange: (id: string, role: string) => void
  onView: (user: AdminUser) => void
}

const STATUS_VARIANTS: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  active: 'default',
  suspended: 'secondary',
  deleted: 'destructive',
}

const ROLES = ['user', 'moderator', 'admin']

export function UsersTable({
  users,
  isLoading,
  onSuspend,
  onRestore,
  onDelete,
  onRoleChange,
  onView,
}: UsersTableProps) {
  if (isLoading) {
    return (
      <div className="flex justify-center py-8">
        <Spinner size="lg" />
      </div>
    )
  }

  if (users.length === 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">No users found.</p>
  }

  return (
    <div className="overflow-auto rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Email</TableHead>
            <TableHead>Role</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="text-right">Blocks</TableHead>
            <TableHead className="text-right">Words</TableHead>
            <TableHead className="text-right">Tests</TableHead>
            <TableHead>Last Active</TableHead>
            <TableHead>Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => (
            <TableRow key={user.id}>
              <TableCell>
                <button
                  className="text-sm font-medium hover:underline"
                  onClick={() => onView(user)}
                >
                  {user.email}
                </button>
                {user.displayName && (
                  <p className="text-xs text-muted-foreground">{user.displayName}</p>
                )}
              </TableCell>
              <TableCell>
                <Select
                  value={user.role}
                  onValueChange={(role) => role && onRoleChange(user.id, role)}
                >
                  <SelectTrigger className="h-7 w-28 text-xs">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {ROLES.map((r) => (
                      <SelectItem key={r} value={r}>
                        {r}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </TableCell>
              <TableCell>
                <Badge variant={STATUS_VARIANTS[user.status] ?? 'outline'}>{user.status}</Badge>
              </TableCell>
              <TableCell className="text-right text-sm">{user.blockCount}</TableCell>
              <TableCell className="text-right text-sm">{user.wordCount}</TableCell>
              <TableCell className="text-right text-sm">{user.testCount}</TableCell>
              <TableCell className="text-xs text-muted-foreground">
                {user.lastActiveAt ? formatDate(user.lastActiveAt) : '—'}
              </TableCell>
              <TableCell>
                <div className="flex gap-1">
                  {user.status === 'active' ? (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-7 text-xs"
                      onClick={() => onSuspend(user.id)}
                    >
                      Suspend
                    </Button>
                  ) : user.status === 'suspended' ? (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-7 text-xs"
                      onClick={() => onRestore(user.id)}
                    >
                      Restore
                    </Button>
                  ) : null}
                  <Button
                    variant="outline"
                    size="sm"
                    className="h-7 text-xs text-destructive hover:text-destructive"
                    onClick={() => onDelete(user.id, user.email)}
                  >
                    Delete
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}
