import { useEffect, useRef, useCallback } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { ROUTES, LANGUAGES } from '@/shared/config'
import { Button, Spinner } from '@/shared/ui'
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
            setError(evt.message ?? 'AI formatting failed. Please try again.')
          }
        },
      })
    } catch (err) {
      if (err instanceof Error && err.name !== 'AbortError') {
        setError('Connection failed. Please check your network and try again.')
      } else if (!(err instanceof Error)) {
        setError('An unexpected error occurred.')
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

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-4xl px-4 py-8">
        {/* Header */}
        <div className="mb-6">
          <Link
            to={ROUTES.BLOCK_DETAIL(blockId ?? '')}
            className="mb-2 inline-block text-sm text-muted-foreground hover:underline"
          >
            ← Back to block
          </Link>
          <h1 className="text-2xl font-bold">Import words with AI</h1>
          {blockData && (
            <p className="text-sm text-muted-foreground">
              into &ldquo;{blockData.block.title}&rdquo;
            </p>
          )}
        </div>

        {/* Draft restore banner */}
        {hasDraft && (
          <div className="mb-4 flex items-center justify-between rounded-lg border bg-muted/30 px-4 py-3">
            <p className="text-sm">
              You have a saved draft with {formattedWords.length} words. Restore it?
            </p>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={resetAll}>
                Discard
              </Button>
              <Button size="sm" onClick={restoreDraft}>
                Restore
              </Button>
            </div>
          </div>
        )}

        {/* Step content */}
        <div className="rounded-lg border bg-card p-6 shadow-sm">
          {/* Step 1 — input */}
          {step === 'input' && (
            <>
              {error && <ImportErrorBanner message={error} onRetry={() => void handleFormat()} />}
              <RawTextInput onSubmit={() => void handleFormat()} />
            </>
          )}

          {/* Step 2 — formatting (SSE streaming) */}
          {step === 'formatting' && (
            <div className="space-y-4">
              <FormatProgress />
              <div className="flex justify-end">
                <Button variant="outline" size="sm" onClick={handleAbort}>
                  Cancel
                </Button>
              </div>
            </div>
          )}

          {/* Step 3 — preview & save */}
          {step === 'preview' && (
            <div className="space-y-6">
              <BlockTitleInput />
              <WordPreviewTable />
              <div className="flex items-center justify-between gap-4 border-t pt-4">
                <p className="text-sm text-muted-foreground">
                  {formattedWords.length} {formattedWords.length === 1 ? 'word' : 'words'} ready to
                  import
                </p>
                <div className="flex gap-2">
                  <Button variant="outline" onClick={resetToInput}>
                    ← Back
                  </Button>
                  <Button
                    onClick={() => void handleSave()}
                    disabled={formattedWords.length === 0 || importWords.isPending}
                  >
                    Save to block
                  </Button>
                </div>
              </div>
            </div>
          )}

          {/* Step 4 — saving */}
          {step === 'saving' && (
            <div className="flex items-center justify-center gap-3 py-12">
              <Spinner size="sm" />
              <p className="text-sm text-muted-foreground">Saving words to block…</p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
