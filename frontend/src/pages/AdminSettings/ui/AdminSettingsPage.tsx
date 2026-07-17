import { useState } from 'react'
import { Pencil, Check, X } from 'lucide-react'
import { Spinner } from '@/shared/ui'
import { formatDate, useIsMobile } from '@/shared/lib'
import { useSettings, useUpdateSettingMutation } from '@/entities/admin'
import type { SystemSetting } from '@/entities/admin'

const COL_WIDTHS = ['220px', '1fr', '80px', '1fr', '120px', '40px']

// These rows exist in the DB but the runtime reads provider config from environment variables —
// editing them changes nothing until that changes. Flagged so the admin isn't misled.
const ENV_MANAGED_KEYS = new Set([
  'ai.primary_model',
  'ai.fallback_enabled',
  'ai.rate_limit_per_minute',
])

const GROUP_ORDER = ['features', 'ai', 'test', 'maintenance']

function groupSettings(settings: SystemSetting[]): Array<[string, SystemSetting[]]> {
  const groups = new Map<string, SystemSetting[]>()
  for (const s of settings) {
    const prefix = s.key.split('.')[0]
    const list = groups.get(prefix) ?? []
    list.push(s)
    groups.set(prefix, list)
  }
  return [...groups.entries()].sort(
    ([a], [b]) =>
      (GROUP_ORDER.indexOf(a) + 1 || 99) - (GROUP_ORDER.indexOf(b) + 1 || 99) || a.localeCompare(b),
  )
}

function EnvManagedBadge() {
  return (
    <span
      title="Stored in the database but not read at runtime — provider config comes from environment variables and needs a redeploy."
      style={{
        fontFamily: 'var(--font-body)',
        fontSize: 10,
        padding: '2px 7px',
        borderRadius: 'var(--r-sm)',
        background: 'var(--warning-ghost)',
        border: '1px solid var(--warning)',
        color: 'var(--warning)',
        whiteSpace: 'nowrap',
      }}
    >
      env / redeploy
    </span>
  )
}

interface EditState {
  editingKey: string | null
  editValue: string
  onEdit: (key: string, currentValue: string) => void
  onSave: (key: string) => void
  onCancel: () => void
  onChangeValue: (value: string) => void
  isPending: boolean
}

/**
 * Mobile card for one setting — the desktop grid's fixed 220px/80px/120px columns plus a flexible
 * description column force long descriptions to wrap into many narrow lines on a phone. Stack the
 * fields vertically instead.
 */
function SettingCard({ setting: s, edit }: { setting: SystemSetting; edit: EditState }) {
  const isEditing = edit.editingKey === s.key
  return (
    <div className="space-y-2 border-b border-b-[var(--line-1)] p-3 last:border-b-0">
      <div className="flex items-center justify-between gap-2">
        <span style={{ color: 'var(--fg-2)', fontSize: 13, fontWeight: 600 }}>{s.key}</span>
        <span style={{ display: 'flex', gap: 6, flexShrink: 0 }}>
          {ENV_MANAGED_KEYS.has(s.key) && <EnvManagedBadge />}
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
        </span>
      </div>
      {s.description && <div style={{ fontSize: 12, color: 'var(--fg-3)' }}>{s.description}</div>}
      {isEditing ? (
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <input
            className="lx-input"
            value={edit.editValue}
            onChange={(e) => edit.onChangeValue(e.target.value)}
            autoFocus
            style={{ height: 32, fontSize: 13, flex: 1 }}
            onKeyDown={(e) => {
              if (e.key === 'Enter') edit.onSave(s.key)
              if (e.key === 'Escape') edit.onCancel()
            }}
          />
          <button
            onClick={() => edit.onSave(s.key)}
            disabled={edit.isPending}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: 'var(--success)',
              padding: 4,
            }}
          >
            <Check size={16} />
          </button>
          <button
            onClick={edit.onCancel}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: 'var(--fg-4)',
              padding: 4,
            }}
          >
            <X size={16} />
          </button>
        </div>
      ) : (
        <div className="flex items-center justify-between gap-2">
          <span style={{ color: 'var(--fg-1)', fontSize: 13 }}>{s.value}</span>
          <button
            onClick={() => edit.onEdit(s.key, s.value)}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: 'var(--fg-4)',
              padding: 4,
            }}
          >
            <Pencil size={14} />
          </button>
        </div>
      )}
      <div style={{ color: 'var(--fg-4)', fontSize: 11 }}>{formatDate(s.updatedAt)}</div>
    </div>
  )
}

export function AdminSettingsPage() {
  const { data: settings, isLoading } = useSettings()
  const updateSetting = useUpdateSettingMutation()
  const isMobile = useIsMobile()
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
      ) : isMobile ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {groupSettings(settings ?? []).map(([group, items]) => (
            <div key={group}>
              <div
                style={{
                  fontSize: 11,
                  fontWeight: 800,
                  textTransform: 'uppercase',
                  letterSpacing: '0.1em',
                  color: 'var(--fg-4)',
                  padding: '0 4px 6px',
                }}
              >
                {group}
              </div>
              <div
                style={{
                  background: 'var(--bg-2)',
                  border: '1px solid var(--line-2)',
                  borderRadius: 'var(--r-md)',
                }}
              >
                {items.map((s) => (
                  <SettingCard
                    key={s.key}
                    setting={s}
                    edit={{
                      editingKey,
                      editValue,
                      onEdit: handleEdit,
                      onSave: (key) => void handleSave(key),
                      onCancel: handleCancel,
                      onChangeValue: setEditValue,
                      isPending: updateSetting.isPending,
                    }}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div
          style={{
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-md)',
            overflowX: 'auto',
          }}
        >
          {/* Header row */}
          <div
            style={{
              display: 'grid',
              minWidth: 560,
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

          {groupSettings(settings ?? []).flatMap(([group, items]) => [
            <div
              key={`group-${group}`}
              style={{
                minWidth: 560,
                padding: '8px 16px',
                background: 'var(--bg-3)',
                borderBottom: '1px solid var(--line-1)',
                fontSize: 10,
                fontWeight: 800,
                textTransform: 'uppercase',
                letterSpacing: '0.1em',
                color: 'var(--fg-4)',
              }}
            >
              {group}
            </div>,
            ...items.map((s, i) => (
              <div
                key={s.key}
                style={{
                  display: 'grid',
                  minWidth: 560,
                  gridTemplateColumns: COL_WIDTHS.join(' '),
                  gap: '0 16px',
                  alignItems: 'center',
                  padding: '12px 16px',
                  borderBottom: i < items.length - 1 ? '1px solid var(--line-1)' : 'none',
                }}
              >
                <span
                  style={{
                    color: 'var(--fg-2)',
                    fontSize: 12,
                    fontWeight: 600,
                    display: 'flex',
                    alignItems: 'center',
                    gap: 6,
                    flexWrap: 'wrap',
                  }}
                >
                  {s.key}
                  {ENV_MANAGED_KEYS.has(s.key) && <EnvManagedBadge />}
                </span>
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
                <span style={{ color: 'var(--fg-4)', fontSize: 11 }}>
                  {formatDate(s.updatedAt)}
                </span>
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
            )),
          ])}
        </div>
      )}
    </div>
  )
}
