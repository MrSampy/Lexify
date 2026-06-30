export function AdminAuditPage() {
  return (
    <div>
      <div className="eyebrow" style={{ marginBottom: 14 }}>
        ~/admin/audit
      </div>
      <h1 className="ds-h2" style={{ margin: '0 0 20px' }}>
        Audit Log
      </h1>
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '40vh',
          gap: 16,
          textAlign: 'center',
        }}
      >
        <div
          style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 32,
            color: 'var(--fg-4)',
          }}
        >
          [ ]
        </div>
        <div className="ds-h3" style={{ color: 'var(--fg-2)' }}>
          Audit logs
        </div>
        <p className="ds-body" style={{ color: 'var(--fg-3)', maxWidth: 380 }}>
          Audit log viewing will be available in a future version. Audit events (impersonation,
          settings changes) are currently recorded in the database.
        </p>
      </div>
    </div>
  )
}
