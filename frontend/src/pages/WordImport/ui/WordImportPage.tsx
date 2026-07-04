import { useEffect, useRef, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { ROUTES, LANGUAGES } from '@/shared/config'
import { Spinner } from '@/shared/ui'
import { useBlock, useUpdateBlockMutation } from '@/entities/block'
import { useImportWordsMutation } from '@/entities/word'
import {
  useImportWordsStore,
  streamFormatWords,
  RawTextInput,
  FormatProgress,
  WordPreviewTable,
  BlockTitleInput,
  ImportErrorBanner,
} from '@/features/import-words'
import type { EditableWord } from '@/features/import-words'

export function WordImportPage() {
  const { t } = useTranslation()
  const { id: blockId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const abortRef = useRef<AbortController | null>(null)

  const { data: blockData } = useBlock(blockId ?? '', 1)

  const step = useImportWordsStore((s) => s.step)
  const rawText = useImportWordsStore((s) => s.rawText)
  const targetLanguageId = useImportWordsStore((s) => s.targetLanguageId)
  const nativeLanguageId = useImportWordsStore((s) => s.nativeLanguageId)
  const formattedWords = useImportWordsStore((s) => s.formattedWords)
  const suggestedTitle = useImportWordsStore((s) => s.suggestedTitle)
  const error = useImportWordsStore((s) => s.error)
  const storedBlockId = useImportWordsStore((s) => s.blockId)

  const setBlockId = useImportWordsStore((s) => s.setBlockId)
  const startFormatting = useImportWordsStore((s) => s.startFormatting)
  const appendChunk = useImportWordsStore((s) => s.appendChunk)
  const setPreview = useImportWordsStore((s) => s.setPreview)
  const setError = useImportWordsStore((s) => s.setError)
  const setSaving = useImportWordsStore((s) => s.setSaving)
  const restoreDraft = useImportWordsStore((s) => s.restoreDraft)
  const resetToInput = useImportWordsStore((s) => s.resetToInput)
  const resetAll = useImportWordsStore((s) => s.resetAll)

  const importWords = useImportWordsMutation(blockId ?? '')
  const updateBlock = useUpdateBlockMutation()

  // Sync URL blockId into store so it survives page reloads
  useEffect(() => {
    if (blockId) setBlockId(blockId)
  }, [blockId, setBlockId])

  const handleFormat = useCallback(async () => {
    const targetCode = LANGUAGES[targetLanguageId]?.code ?? 'en'
    const nativeCode = LANGUAGES[nativeLanguageId]?.code ?? 'uk'

    startFormatting()
    abortRef.current = new AbortController()

    try {
      await streamFormatWords({
        rawText,
        targetLanguage: targetCode,
        nativeLanguage: nativeCode,
        signal: abortRef.current.signal,
        onEvent: (evt) => {
          if (evt.type === 'streaming' && evt.chunk) {
            appendChunk(evt.chunk)
          } else if (evt.type === 'done' && evt.result) {
            const words: EditableWord[] = evt.result.words.map((w) => ({
              ...w,
              _id: crypto.randomUUID(),
            }))
            setPreview(words, evt.result.suggestedTitle ?? '')
          } else if (evt.type === 'error') {
            setError(evt.message ?? t('wordImport.errFormat'))
          }
        },
      })
    } catch (err) {
      if (err instanceof Error && err.name !== 'AbortError') {
        setError(t('wordImport.errConnection'))
      } else if (!(err instanceof Error)) {
        setError(t('wordImport.errUnexpected'))
      }
    }
  }, [
    rawText,
    targetLanguageId,
    nativeLanguageId,
    startFormatting,
    appendChunk,
    setPreview,
    setError,
    t,
  ])

  const handleAbort = () => {
    abortRef.current?.abort()
    resetToInput()
  }

  const handleSave = async () => {
    if (!blockId || formattedWords.length === 0) return
    setSaving()

    try {
      const currentWordCount = blockData?.words.totalCount ?? 0

      const words = formattedWords.map((w, i) => ({
        term: w.term.trim(),
        translation: w.translation.trim(),
        alternativeTranslations: w.alternativeTranslations?.filter((t) => t.trim().length > 0),
        wordType: w.wordType,
        notes: w.notes ?? undefined,
        exampleSentence: w.exampleSentence ?? undefined,
        confidenceFlag: w.confidenceFlag,
        confidenceNote: w.confidenceNote ?? undefined,
        sortOrder: currentWordCount + i + 1,
      }))

      // Rename block if the user changed the suggested title
      const currentTitle = blockData?.block.title
      if (suggestedTitle && suggestedTitle !== currentTitle) {
        await updateBlock.mutateAsync({ id: blockId, input: { title: suggestedTitle } })
      }

      await importWords.mutateAsync(words)
      resetAll()
      navigate(ROUTES.BLOCK_DETAIL(blockId))
    } catch {
      resetToInput()
    }
  }

  // Show draft restore banner when user is on 'input' but has saved words from a previous preview
  const hasDraft = step === 'input' && storedBlockId === blockId && formattedWords.length > 0

  const STEPS = [
    { idx: '1', label: t('wordImport.stepInput') },
    { idx: '2', label: t('wordImport.stepFormatting') },
    { idx: '3', label: t('wordImport.stepPreview') },
    { idx: '4', label: t('wordImport.stepSave') },
  ]
  const STEP_INDEX: Record<string, number> = { input: 0, formatting: 1, preview: 2, saving: 3 }
  const stepIndex = STEP_INDEX[step] ?? 0

  return (
    <div style={{ maxWidth: 800, margin: '0 auto' }}>
      <Link
        to={ROUTES.BLOCK_DETAIL(blockId ?? '')}
        style={{
          color: 'var(--accent-color)',
          textDecoration: 'none',
          fontSize: 14,
          fontWeight: 700,
          marginBottom: 16,
          display: 'inline-block',
        }}
      >
        {t('wordImport.backToBlock')}
      </Link>

      {/* Page title */}
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 14, marginBottom: 24 }}>
        <h1 className="ds-h2" style={{ margin: 0 }}>
          {t('wordImport.title')}
        </h1>
        {blockData && (
          <span className="ds-sm" style={{ color: 'var(--fg-3)' }}>
            {t('wordImport.into', { title: blockData.block.title })}
          </span>
        )}
      </div>

      {/* Stepper */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 0,
          marginBottom: 24,
          background: 'var(--bg-2)',
          border: '1px solid var(--line-2)',
          borderRadius: 'var(--r-md)',
          overflow: 'hidden',
        }}
      >
        {STEPS.map((s, i) => (
          <div
            key={s.idx}
            style={{
              flex: 1,
              display: 'flex',
              alignItems: 'center',
              gap: 8,
              padding: '10px 16px',
              borderRight: i < STEPS.length - 1 ? '1px solid var(--line-2)' : 'none',
              background: i === stepIndex ? 'var(--accent-ghost)' : 'transparent',
              borderBottom:
                i === stepIndex ? '2px solid var(--accent-color)' : '2px solid transparent',
            }}
          >
            <span
              style={{
                fontFamily: 'var(--font-body)',
                fontWeight: 700,
                fontSize: 11,
                color:
                  i === stepIndex
                    ? 'var(--accent-color)'
                    : i < stepIndex
                      ? 'var(--success)'
                      : 'var(--fg-4)',
              }}
            >
              {i < stepIndex ? '✓' : s.idx}
            </span>
            <span
              style={{
                fontSize: 12,
                color:
                  i === stepIndex ? 'var(--fg-1)' : i < stepIndex ? 'var(--fg-3)' : 'var(--fg-4)',
              }}
            >
              {s.label}
            </span>
          </div>
        ))}
      </div>

      {/* Draft restore banner */}
      {hasDraft && (
        <div
          style={{
            marginBottom: 16,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '12px 16px',
            background: 'var(--accent-ghost)',
            border: '1px solid var(--accent-line)',
            borderRadius: 'var(--r-md)',
          }}
        >
          <span style={{ color: 'var(--fg-2)', fontSize: 13, fontWeight: 600 }}>
            {t('wordImport.draftSaved', { count: formattedWords.length })}
          </span>
          <div style={{ display: 'flex', gap: 8 }}>
            <button
              className="lx-btn-secondary"
              style={{ padding: '6px 14px', fontSize: 12 }}
              onClick={resetAll}
            >
              {t('wordImport.discard')}
            </button>
            <button
              className="lx-btn-primary"
              style={{ padding: '6px 14px', fontSize: 12 }}
              onClick={restoreDraft}
            >
              {t('wordImport.restore')}
            </button>
          </div>
        </div>
      )}

      {/* Step content */}
      <div
        style={{
          background: 'var(--bg-2)',
          border: '1px solid var(--line-2)',
          borderRadius: 'var(--r-lg)',
          overflow: 'hidden',
        }}
      >
        {/* Step 1 — input */}
        {step === 'input' && (
          <div style={{ padding: '24px 28px' }}>
            {error && <ImportErrorBanner message={error} onRetry={() => void handleFormat()} />}
            <RawTextInput onSubmit={() => void handleFormat()} />
          </div>
        )}

        {/* Step 2 — formatting (SSE streaming) */}
        {step === 'formatting' && (
          <div>
            {/* Terminal header */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                padding: '10px 16px',
                background: 'var(--bg-3)',
                borderBottom: '1px solid var(--line-2)',
              }}
            >
              <div style={{ display: 'flex', gap: 6 }}>
                {['#FF5F57', '#FEBC2E', '#28C840'].map((c) => (
                  <div
                    key={c}
                    style={{ width: 10, height: 10, borderRadius: '50%', background: c }}
                  />
                ))}
              </div>
              <span style={{ color: 'var(--fg-4)', fontSize: 11, flex: 1, fontWeight: 600 }}>
                {t('wordImport.streamingHeader')}
              </span>
              <button
                className="lx-btn-secondary"
                style={{ padding: '4px 12px', fontSize: 11 }}
                onClick={handleAbort}
              >
                {t('wordImport.cancelStream')}
              </button>
            </div>
            <div style={{ padding: '20px 24px' }}>
              <FormatProgress />
            </div>
          </div>
        )}

        {/* Step 3 — preview & save */}
        {step === 'preview' && (
          <div style={{ padding: '24px 28px' }}>
            <BlockTitleInput />
            <div style={{ marginTop: 20 }}>
              <WordPreviewTable />
            </div>
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: 12,
                marginTop: 20,
                paddingTop: 16,
                borderTop: '1px solid var(--line-2)',
              }}
            >
              <span style={{ color: 'var(--fg-3)', fontSize: 12, fontWeight: 600 }}>
                {formattedWords.length} {formattedWords.length === 1 ? 'word' : 'words'} ready
              </span>
              <div style={{ display: 'flex', gap: 10 }}>
                <button className="lx-btn-secondary" onClick={resetToInput}>
                  ← Back
                </button>
                <button
                  className="lx-btn-primary"
                  onClick={() => void handleSave()}
                  disabled={formattedWords.length === 0 || importWords.isPending}
                >
                  {t('wordImport.saveToBlock')}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Step 4 — saving */}
        {step === 'saving' && (
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 12,
              padding: '60px 28px',
            }}
          >
            <Spinner size="sm" />
            <span style={{ color: 'var(--fg-3)', fontWeight: 600, fontSize: 14 }}>
              {t('wordImport.saving')}
            </span>
          </div>
        )}
      </div>
    </div>
  )
}
