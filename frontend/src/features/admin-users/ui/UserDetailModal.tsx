import { Dialog, DialogContent, DialogHeader, DialogTitle, Badge, Button } from '@/shared/ui'
import { formatDate } from '@/shared/lib'
import type { AdminUser } from '@/entities/admin'

interface UserDetailModalProps {
  user: AdminUser | null
  open: boolean
  canManageUsers: boolean
  onClose: () => void
  onSuspend: (id: string) => void
  onRestore: (id: string) => void
  onDelete: (id: string, email: string) => void
  /** Provided only for superadmins — renders the "Sign in as user" button. */
  onImpersonate?: (id: string) => void
  /** Manual override for a user who cannot receive the confirmation link. */
  onVerifyEmail?: (id: string) => void
}

export function UserDetailModal({
  user,
  open,
  canManageUsers,
  onClose,
  onSuspend,
  onRestore,
  onDelete,
  onImpersonate,
  onVerifyEmail,
}: UserDetailModalProps) {
  if (!user) return null

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>User details</DialogTitle>
        </DialogHeader>

        <div className="space-y-3 text-sm">
          <Row label="Email" value={user.email} />
          {user.displayName && <Row label="Display name" value={user.displayName} />}
          <Row label="Role" value={<Badge variant="outline">{user.role}</Badge>} />
          <Row
            label="Status"
            value={
              <Badge
                variant={
                  user.status === 'active'
                    ? 'default'
                    : user.status === 'suspended'
                      ? 'secondary'
                      : 'destructive'
                }
              >
                {user.status}
              </Badge>
            }
          />
          <Row
            label="Email confirmed"
            value={
              user.emailVerifiedAt ? (
                formatDate(user.emailVerifiedAt)
              ) : (
                <Badge variant="outline" style={{ color: 'var(--warning)' }}>
                  not confirmed
                </Badge>
              )
            }
          />
          <Row label="Blocks" value={String(user.blockCount)} />
          <Row label="Words" value={String(user.wordCount)} />
          <Row label="Tests" value={String(user.testCount)} />
          <Row label="Created" value={formatDate(user.createdAt)} />
          <Row
            label="Last active"
            value={user.lastActiveAt ? formatDate(user.lastActiveAt) : '—'}
          />
        </div>

        <div className="mt-4 flex flex-wrap gap-2">
          {user.status === 'active' && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                onSuspend(user.id)
                onClose()
              }}
            >
              Suspend
            </Button>
          )}
          {user.status === 'suspended' && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                onRestore(user.id)
                onClose()
              }}
            >
              Restore
            </Button>
          )}
          {canManageUsers && (
            <Button
              variant="outline"
              size="sm"
              className="text-destructive hover:text-destructive"
              onClick={() => {
                onDelete(user.id, user.email)
                onClose()
              }}
            >
              Delete
            </Button>
          )}
          {canManageUsers && !user.emailVerifiedAt && onVerifyEmail && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                onVerifyEmail(user.id)
                onClose()
              }}
            >
              Confirm email
            </Button>
          )}
          {onImpersonate && user.status === 'active' && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                onImpersonate(user.id)
                onClose()
              }}
            >
              Sign in as user
            </Button>
          )}
          <Button variant="ghost" size="sm" onClick={onClose} className="ml-auto">
            Close
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{value}</span>
    </div>
  )
}
