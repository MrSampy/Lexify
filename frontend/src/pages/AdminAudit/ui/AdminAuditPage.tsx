import { ClipboardList } from 'lucide-react'

export function AdminAuditPage() {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 p-8 text-center">
      <ClipboardList size={48} className="text-muted-foreground/40" />
      <h1 className="text-xl font-semibold">Audit logs</h1>
      <p className="max-w-sm text-sm text-muted-foreground">
        Audit log viewing will be available in a future version. Audit events (impersonation,
        settings changes) are currently recorded in the database.
      </p>
    </div>
  )
}
