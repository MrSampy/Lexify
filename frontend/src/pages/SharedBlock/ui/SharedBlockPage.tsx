import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { ROUTES, LANGUAGES } from '@/shared/config'
import { LanguageBadge, Mascot, Spinner } from '@/shared/ui'
import { useCopySharedBlockMutation, useSharedBlock } from '@/entities/block'
import { WordTypeBadge } from '@/entities/word'

/**
 * Read-only landing page for a share link. Copying is an explicit button press, never automatic: the
 * link is something someone else sent, and opening it should not quietly write to the account.
 */
export function SharedBlockPage() {
  const { t } = useTranslation()
  const { token } = useParams<{ token: string }>()
  const navigate = useNavigate()

  const { data, isLoading, isError } = useSharedBlock(token ?? '')
  const copyBlock = useCopySharedBlockMutation()

  const handleCopy = () => {
    if (!token) return
    copyBlock.mutate(token, {
      onSuccess: (newBlockId) => {
        toast.success(t('sharedBlock.copied'))
        navigate(ROUTES.BLOCK_DETAIL(newBlockId))
      },
      onError: () => toast.error(t('sharedBlock.copyFailed')),
    })
  }

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError || !data) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 text-center">
        <Mascot pose="sleep" size={140} animate />
        <div className="ds-h3">{t('sharedBlock.unavailable')}</div>
        <p className="ds-body text-[var(--fg-3)]">{t('sharedBlock.unavailableHint')}</p>
        <Link to={ROUTES.BLOCKS} className="no-underline">
          <button className="lx-btn-secondary">{t('sharedBlock.goToBlocks')}</button>
        </Link>
      </div>
    )
  }

  const langCode = LANGUAGES[data.languageId]?.code ?? String(data.languageId)

  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      <div
        style={{
          display: 'flex',
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          gap: 16,
          flexWrap: 'wrap',
          marginBottom: 6,
        }}
      >
        <div>
          <div className="eyebrow">{t('sharedBlock.shared')}</div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginTop: 6 }}>
            <h1 className="ds-h2" style={{ margin: 0 }}>
              {data.title}
            </h1>
            <LanguageBadge code={langCode} />
          </div>
          <p className="ds-sm" style={{ margin: '6px 0 0', color: 'var(--fg-3)' }}>
            {t('sharedBlock.wordCount', { count: data.wordCount })}
            {data.ownerDisplayName && (
              <> · {t('sharedBlock.sharedBy', { name: data.ownerDisplayName })}</>
            )}
          </p>
        </div>
        <button className="lx-btn-primary" onClick={handleCopy} disabled={copyBlock.isPending}>
          {copyBlock.isPending ? t('sharedBlock.copying') : t('sharedBlock.copyToMyBlocks')}
        </button>
      </div>

      {data.description && (
        <p className="ds-body" style={{ color: 'var(--fg-3)', marginTop: 4 }}>
          {data.description}
        </p>
      )}

      <p className="ds-sm" style={{ color: 'var(--fg-4)', margin: '10px 0 20px' }}>
        {t('sharedBlock.previewNote')}
      </p>

      {/* Preview table — stacked cards on mobile, columns from md up, mirroring the block page */}
      <div
        style={{
          background: 'var(--bg-2)',
          border: '1px solid var(--line-2)',
          borderRadius: 'var(--r-lg)',
          overflow: 'hidden',
        }}
      >
        {data.words.map((word, i) => (
          <div
            key={`${word.term}-${i}`}
            className="grid grid-cols-1 gap-1.5 border-b border-b-[var(--line-1)] px-[18px] py-3.5 last:border-b-0 md:grid-cols-[1.4fr_1.6fr_0.9fr_1.6fr] md:items-center md:gap-3"
          >
            <div style={{ color: 'var(--fg-1)', fontWeight: 500, fontSize: 14 }}>{word.term}</div>
            <div>
              <div style={{ color: 'var(--fg-2)', fontSize: 14 }}>{word.translation}</div>
              {word.alternativeTranslations.length > 0 && (
                <div style={{ fontSize: 12, color: 'var(--fg-4)', marginTop: 2 }}>
                  {t('words.also')} {word.alternativeTranslations.join(', ')}
                </div>
              )}
              {word.synonyms.length > 0 && (
                <div style={{ fontSize: 12, color: 'var(--accent-dim)', marginTop: 2 }}>
                  {t('words.synonyms')} {word.synonyms.join(', ')}
                </div>
              )}
            </div>
            <div>
              <WordTypeBadge type={word.wordType} />
            </div>
            <div>
              {word.notes && <div style={{ fontSize: 13, color: 'var(--fg-3)' }}>{word.notes}</div>}
              {word.exampleSentence && (
                <div
                  style={{ fontSize: 12, color: 'var(--fg-4)', fontStyle: 'italic', marginTop: 2 }}
                >
                  “{word.exampleSentence}”
                </div>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
