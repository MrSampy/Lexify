import { useState } from 'react'
import { Pencil, Check, X } from 'lucide-react'
import { Spinner } from '@/shared/ui'
import { formatDate } from '@/shared/lib'
import { useSettings, useUpdateSettingMutation } from '@/entities/admin'

const COL_WIDTHS = ['220px', '1fr', '80px', '1fr', '120px', '40px']

export function AdminSettingsPage() {
  const { data: settings, isLoading } = useSettings()
  const updateSetting = useUpdateSettingMutation()
  const [editingKey, setEditingKey] = useState<string | null>(null)
  const [editValue, setEditValue] = useState('')

  const handleEdit = (key: string, currentValue: string) => {
    setEditingKey(key)
    setEditValue(currentValue)
  }

  const handleSave = async (key: string) => {
    await updateSetting.mutateAsync({ key, value: editValue })
    setEditingKey(null)
  }

  const handleCancel = () => {
    setEditingKey(null)
    setEditValue('')
  }

  return (
    <div>
      <h1 className="ds-h2" style={{ margin: '0 0 20px' }}>
        System Settings
      </h1>

      {isLoading ? (
        <div style={{ display: 'flex', justifyContent: 'center', padding: '60px 0' }}>
          <Spinner size="lg" />
        </div>
      ) : (
        <div
          style={{
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-md)',
            overflow: 'hidden',
          }}
        >
          {/* Header row */}
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: COL_WIDTHS.join(' '),
              gap: '0 16px',
              padding: '10px 16px',
              background: 'var(--bg-3)',
              borderBottom: '1px solid var(--line-2)',
            }}
          >
            {['Key', 'Value', 'Type', 'Description', 'Updated', ''].map((h) => (
              <span
                key={h}
                style={{
                  fontFamily: 'var(--font-body)',
                  fontWeight: 700,
                  color: 'var(--fg-4)',
                  fontSize: 10,
                  textTransform: 'uppercase',
                  letterSpacing: '0.1em',
                }}
              >
                {h}
              </span>
            ))}
          </div>

          {(settings ?? []).map((s, i) => (
            <div
              key={s.key}
              style={{
                display: 'grid',
                gridTemplateColumns: COL_WIDTHS.join(' '),
                gap: '0 16px',
                alignItems: 'center',
                padding: '12px 16px',
                borderBottom: i < (settings?.length ?? 0) - 1 ? '1px solid var(--line-1)' : 'none',
              }}
            >
              <span style={{ color: 'var(--fg-2)', fontSize: 12, fontWeight: 600 }}>{s.key}</span>
              <span>
                {editingKey === s.key ? (
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <input
                      className="lx-input"
                      value={editValue}
                      onChange={(e) => setEditValue(e.target.value)}
                      autoFocus
                      style={{ height: 30, fontSize: 13, flex: 1 }}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') void handleSave(s.key)
                        if (e.key === 'Escape') handleCancel()
                      }}
                    />
                    <button
                      onClick={() => void handleSave(s.key)}
                      disabled={updateSetting.isPending}
                      style={{
                        background: 'none',
                        border: 'none',
                        cursor: 'pointer',
                        color: 'var(--success)',
                        padding: 4,
                      }}
                    >
                      <Check size={14} />
                    </button>
                    <button
                      onClick={handleCancel}
                      style={{
                        background: 'none',
                        border: 'none',
                        cursor: 'pointer',
                        color: 'var(--fg-4)',
                        padding: 4,
                      }}
                    >
                      <X size={14} />
                    </button>
                  </div>
                ) : (
                  <span style={{ color: 'var(--fg-1)', fontSize: 13 }}>{s.value}</span>
                )}
              </span>
              <span
                style={{
                  fontFamily: 'var(--font-body)',
                  fontSize: 10,
                  padding: '2px 7px',
                  borderRadius: 'var(--r-sm)',
                  background: 'var(--bg-3)',
                  border: '1px solid var(--line-2)',
                  color: 'var(--fg-4)',
                }}
              >
                {s.valueType}
              </span>
              <span style={{ fontSize: 12, color: 'var(--fg-3)' }}>{s.description ?? '—'}</span>
              <span style={{ color: 'var(--fg-4)', fontSize: 11 }}>{formatDate(s.updatedAt)}</span>
              <div>
                {editingKey !== s.key && (
                  <button
                    onClick={() => handleEdit(s.key, s.value)}
                    style={{
                      background: 'none',
                      border: 'none',
                      cursor: 'pointer',
                      color: 'var(--fg-4)',
                      padding: 4,
                      transition: 'color 0.12s',
                    }}
                    onMouseEnter={(e) => {
                      ;(e.currentTarget as HTMLButtonElement).style.color = 'var(--accent-color)'
                    }}
                    onMouseLeave={(e) => {
                      ;(e.currentTarget as HTMLButtonElement).style.color = 'var(--fg-4)'
                    }}
                  >
                    <Pencil size={13} />
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
