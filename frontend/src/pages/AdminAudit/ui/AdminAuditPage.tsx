export function AdminAuditPage() {
  return (
    <div>
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
        <div style={{ fontSize: 48 }}>📋</div>
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
