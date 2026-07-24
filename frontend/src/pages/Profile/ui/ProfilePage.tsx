import { useRef } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ChevronDown } from 'lucide-react'
import { useProfile } from '@/entities/user'
import { Spinner } from '@/shared/ui'
import { useIsMobile } from '@/shared/lib'
import { AccountTab } from './AccountTab'
import { LearningTab } from './LearningTab'
import { AppearanceTab } from './AppearanceTab'

const TABS = [
  { id: 'account', labelKey: 'profile.tabAccount' },
  { id: 'learning', labelKey: 'profile.tabLearning' },
  { id: 'appearance', labelKey: 'profile.tabAppearance' },
] as const

type TabId = (typeof TABS)[number]['id']

export function ProfilePage() {
  const { t } = useTranslation()
  const { data: profile, isLoading } = useProfile()
  const isMobile = useIsMobile()

  // The active section lives in the URL so "the notification setting" is a linkable place.
  const [searchParams, setSearchParams] = useSearchParams()
  const requested = searchParams.get('tab')
  const active: TabId = TABS.some((tab) => tab.id === requested) ? (requested as TabId) : 'account'
  const selectTab = (id: TabId) => setSearchParams({ tab: id }, { replace: true })

  const tabRefs = useRef<Record<string, HTMLButtonElement | null>>({})

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', padding: 80 }}>
        <Spinner size="lg" />
      </div>
    )
  }

  const renderTab = (id: TabId) => {
    if (!profile) return null
    if (id === 'account') return <AccountTab profile={profile} />
    if (id === 'learning') return <LearningTab profile={profile} />
    return <AppearanceTab />
  }

  // Roving focus: a tablist is one tab stop, arrows move between the tabs inside it.
  const handleTabKeyDown = (e: React.KeyboardEvent, index: number) => {
    const delta = e.key === 'ArrowRight' ? 1 : e.key === 'ArrowLeft' ? -1 : 0
    if (delta === 0) return
    e.preventDefault()
    const next = TABS[(index + delta + TABS.length) % TABS.length]
    selectTab(next.id)
    tabRefs.current[next.id]?.focus()
  }

  return (
    <div>
      <h1 className="ds-h2" style={{ margin: '0 0 6px' }}>
        {t('profile.title')}
      </h1>
      <p className="ds-body" style={{ margin: '0 0 24px', color: 'var(--fg-3)' }}>
        {profile?.email}
      </p>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 560 }}>
        {isMobile ? (
          // Narrow screens: an exclusive accordion. A tab strip here would either wrap onto two rows
          // or hide sections behind a horizontal scroll nobody notices.
          TABS.map(({ id, labelKey }) => {
            const open = active === id
            return (
              <div key={id} style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
                <button
                  aria-expanded={open}
                  aria-controls={`profile-panel-${id}`}
                  onClick={() => selectTab(id)}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    gap: 10,
                    width: '100%',
                    padding: '14px 18px',
                    cursor: 'pointer',
                    fontFamily: 'var(--font-body)',
                    fontSize: 15,
                    fontWeight: 800,
                    textAlign: 'left',
                    color: open ? 'var(--fg-1)' : 'var(--fg-2)',
                    background: open ? 'var(--accent-ghost)' : 'var(--bg-1)',
                    border: `1.5px solid ${open ? 'var(--accent-line)' : 'var(--line-2)'}`,
                    borderRadius: 'var(--r-md)',
                  }}
                >
                  {t(labelKey)}
                  <ChevronDown
                    size={18}
                    style={{
                      flexShrink: 0,
                      transition: 'transform 0.15s',
                      transform: open ? 'rotate(180deg)' : 'none',
                    }}
                  />
                </button>
                {open && (
                  <div
                    id={`profile-panel-${id}`}
                    style={{ display: 'flex', flexDirection: 'column', gap: 20 }}
                  >
                    {renderTab(id)}
                  </div>
                )}
              </div>
            )
          })
        ) : (
          <>
            <div
              role="tablist"
              aria-label={t('profile.title')}
              style={{
                display: 'flex',
                gap: 4,
                padding: 4,
                background: 'var(--bg-1)',
                border: '1.5px solid var(--line-2)',
                borderRadius: 'var(--r-pill)',
                alignSelf: 'flex-start',
              }}
            >
              {TABS.map(({ id, labelKey }, index) => {
                const selected = active === id
                return (
                  <button
                    key={id}
                    ref={(el) => {
                      tabRefs.current[id] = el
                    }}
                    role="tab"
                    id={`profile-tab-${id}`}
                    aria-selected={selected}
                    aria-controls={`profile-panel-${id}`}
                    tabIndex={selected ? 0 : -1}
                    onClick={() => selectTab(id)}
                    onKeyDown={(e) => handleTabKeyDown(e, index)}
                    style={{
                      border: 'none',
                      cursor: 'pointer',
                      fontFamily: 'var(--font-body)',
                      fontSize: 13,
                      fontWeight: 800,
                      letterSpacing: '0.02em',
                      padding: '8px 18px',
                      borderRadius: 'var(--r-pill)',
                      background: selected ? 'var(--accent-color)' : 'transparent',
                      color: selected ? '#fff' : 'var(--fg-3)',
                      transition: 'all 0.12s',
                    }}
                  >
                    {t(labelKey)}
                  </button>
                )
              })}
            </div>

            <div
              role="tabpanel"
              id={`profile-panel-${active}`}
              aria-labelledby={`profile-tab-${active}`}
              style={{ display: 'flex', flexDirection: 'column', gap: 20 }}
            >
              {renderTab(active)}
            </div>
          </>
        )}
      </div>
    </div>
  )
}
