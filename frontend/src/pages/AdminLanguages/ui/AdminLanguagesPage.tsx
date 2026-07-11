import { useState } from 'react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Spinner,
} from '@/shared/ui'
import { useIsMobile } from '@/shared/lib'
import {
  useLanguages,
  useAddLanguageMutation,
  useToggleLanguageMutation,
  type Language,
} from '@/entities/admin'

const COL_WIDTHS = ['70px', '1fr', '1fr', '60px', '80px', '90px']

/**
 * Mobile card for one language — the desktop grid's 6 fixed/flexible columns squeeze into
 * unreadable slivers on a phone (name/native/status all clipped). Stack the fields instead.
 */
function LanguageCard({
  lang,
  onToggle,
  isToggling,
}: {
  lang: Language
  onToggle: () => void
  isToggling: boolean
}) {
  return (
    <div className="flex items-center justify-between gap-3 border-b border-b-[var(--line-1)] p-3 last:border-b-0">
      <div style={{ minWidth: 0 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ color: 'var(--accent-color)', fontSize: 13, fontWeight: 700 }}>
            {lang.code}
          </span>
          <span style={{ color: 'var(--fg-1)', fontSize: 14 }}>{lang.name}</span>
          <span
            style={{
              fontFamily: 'var(--font-body)',
              fontSize: 10,
              padding: '3px 8px',
              borderRadius: 999,
              background: lang.isActive ? 'rgba(63,214,139,0.1)' : 'var(--bg-3)',
              border: `1px solid ${lang.isActive ? 'rgba(63,214,139,0.3)' : 'var(--line-2)'}`,
              color: lang.isActive ? 'var(--success)' : 'var(--fg-4)',
              flexShrink: 0,
            }}
          >
            {lang.isActive ? 'active' : 'inactive'}
          </span>
        </div>
        <div style={{ color: 'var(--fg-3)', fontSize: 12, marginTop: 2 }}>
          {lang.nativeName} · sort {lang.sortOrder}
        </div>
      </div>
      <button
        className="lx-btn-secondary"
        style={{ padding: '4px 12px', fontSize: 11, flexShrink: 0 }}
        onClick={onToggle}
        disabled={isToggling}
      >
        {lang.isActive ? 'Disable' : 'Enable'}
      </button>
    </div>
  )
}

export function AdminLanguagesPage() {
  const { data: languages, isLoading } = useLanguages()
  const addLanguage = useAddLanguageMutation()
  const toggleLanguage = useToggleLanguageMutation()
  const isMobile = useIsMobile()

  const [showAdd, setShowAdd] = useState(false)
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [nativeName, setNativeName] = useState('')
  const [sortOrder, setSortOrder] = useState('0')

  const handleAdd = async () => {
    if (!code.trim() || !name.trim() || !nativeName.trim()) return
    await addLanguage.mutateAsync({
      code: code.trim(),
      name: name.trim(),
      nativeName: nativeName.trim(),
      sortOrder: Number(sortOrder) || 0,
    })
    setShowAdd(false)
    setCode('')
    setName('')
    setNativeName('')
    setSortOrder('0')
  }

  return (
    <div>
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: 20,
        }}
      >
        <h1 className="ds-h2" style={{ margin: 0 }}>
          Languages
        </h1>
        <button className="lx-btn-primary" onClick={() => setShowAdd(true)}>
          + Add language
        </button>
      </div>

      {isLoading ? (
        <div style={{ display: 'flex', justifyContent: 'center', padding: '60px 0' }}>
          <Spinner size="lg" />
        </div>
      ) : isMobile ? (
        <div
          style={{
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-md)',
          }}
        >
          {(languages ?? []).map((lang) => (
            <LanguageCard
              key={lang.id}
              lang={lang}
              onToggle={() => void toggleLanguage.mutateAsync(lang.code)}
              isToggling={toggleLanguage.isPending}
            />
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
          <div
            style={{
              display: 'grid',
              minWidth: 520,
              gridTemplateColumns: COL_WIDTHS.join(' '),
              gap: '0 16px',
              padding: '10px 16px',
              background: 'var(--bg-3)',
              borderBottom: '1px solid var(--line-2)',
            }}
          >
            {['Code', 'Name', 'Native', 'Sort', 'Status', ''].map((h) => (
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

          {(languages ?? []).map((lang, i) => (
            <div
              key={lang.id}
              style={{
                display: 'grid',
                minWidth: 520,
                gridTemplateColumns: COL_WIDTHS.join(' '),
                gap: '0 16px',
                alignItems: 'center',
                padding: '12px 16px',
                borderBottom: i < (languages?.length ?? 0) - 1 ? '1px solid var(--line-1)' : 'none',
              }}
            >
              <span style={{ color: 'var(--accent-color)', fontSize: 13, fontWeight: 700 }}>
                {lang.code}
              </span>
              <span style={{ color: 'var(--fg-1)', fontSize: 14 }}>{lang.name}</span>
              <span style={{ color: 'var(--fg-3)', fontSize: 13 }}>{lang.nativeName}</span>
              <span style={{ color: 'var(--fg-4)', fontSize: 12 }}>{lang.sortOrder}</span>
              <span
                style={{
                  fontFamily: 'var(--font-body)',
                  fontSize: 10,
                  padding: '3px 8px',
                  borderRadius: 999,
                  background: lang.isActive ? 'rgba(63,214,139,0.1)' : 'var(--bg-3)',
                  border: `1px solid ${lang.isActive ? 'rgba(63,214,139,0.3)' : 'var(--line-2)'}`,
                  color: lang.isActive ? 'var(--success)' : 'var(--fg-4)',
                }}
              >
                {lang.isActive ? 'active' : 'inactive'}
              </span>
              <button
                className="lx-btn-secondary"
                style={{ padding: '4px 12px', fontSize: 11 }}
                onClick={() => void toggleLanguage.mutateAsync(lang.code)}
                disabled={toggleLanguage.isPending}
              >
                {lang.isActive ? 'Disable' : 'Enable'}
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Add language dialog */}
      <Dialog open={showAdd} onOpenChange={setShowAdd}>
        <DialogContent style={{ background: 'var(--bg-2)', border: '1px solid var(--line-2)' }}>
          <DialogHeader>
            <DialogTitle className="ds-h4">Add language</DialogTitle>
          </DialogHeader>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 14, padding: '8px 0' }}>
            <div>
              <label className="lx-label" style={{ display: 'block', marginBottom: 6 }}>
                Code (e.g. "fr")
              </label>
              <input
                className="lx-input"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                maxLength={10}
                style={{ width: '100%' }}
              />
            </div>
            <div>
              <label className="lx-label" style={{ display: 'block', marginBottom: 6 }}>
                Name (English)
              </label>
              <input
                className="lx-input"
                value={name}
                onChange={(e) => setName(e.target.value)}
                style={{ width: '100%' }}
              />
            </div>
            <div>
              <label className="lx-label" style={{ display: 'block', marginBottom: 6 }}>
                Native name
              </label>
              <input
                className="lx-input"
                value={nativeName}
                onChange={(e) => setNativeName(e.target.value)}
                style={{ width: '100%' }}
              />
            </div>
            <div>
              <label className="lx-label" style={{ display: 'block', marginBottom: 6 }}>
                Sort order
              </label>
              <input
                className="lx-input"
                type="number"
                value={sortOrder}
                onChange={(e) => setSortOrder(e.target.value)}
                style={{ width: 100 }}
              />
            </div>
          </div>
          <DialogFooter>
            <button className="lx-btn-secondary" onClick={() => setShowAdd(false)}>
              Cancel
            </button>
            <button
              className="lx-btn-primary"
              onClick={() => void handleAdd()}
              disabled={addLanguage.isPending || !code || !name || !nativeName}
            >
              Add
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
