import { useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { ROUTES, LANGUAGES } from '@/shared/config'
import { Spinner } from '@/shared/ui'
import { useBlock, useDeleteBlockMutation, useExportBlock } from '@/entities/block'
import { useCreateWordMutation } from '@/entities/word'
import { WordRow } from '@/entities/word'
import { TagInput } from '@/features/manage-tags'

const WORD_TYPES = ['word', 'phrase', 'idiom', 'expression']

const addWordSchema = z.object({
  term: z.string().min(1, 'Term is required').max(200),
  translation: z.string().min(1, 'Translation is required').max(200),
  wordType: z.string().min(1),
  notes: z.string().max(500).optional(),
})

type AddWordForm = z.infer<typeof addWordSchema>

export function BlockDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [wordsPage, setWordsPage] = useState(1)
  const [confidenceOnly, setConfidenceOnly] = useState(false)
  const [showAddWord, setShowAddWord] = useState(false)

  const { data, isLoading, isError } = useBlock(id ?? '', wordsPage)
  const deleteBlock = useDeleteBlockMutation()
  const exportBlock = useExportBlock()
  const createWord = useCreateWordMutation(id ?? '')

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AddWordForm>({
    resolver: zodResolver(addWordSchema),
    defaultValues: { term: '', translation: '', wordType: 'word', notes: '' },
  })

  const wordType = watch('wordType')

  const handleDeleteBlock = async () => {
    if (!data || !confirm(`Delete block "${data.block.title}"?`)) return
    await deleteBlock.mutateAsync(data.block.id)
    navigate(ROUTES.BLOCKS)
  }

  const onAddWord = async (values: AddWordForm) => {
    const currentCount = data?.words.totalCount ?? 0
    await createWord.mutateAsync({
      term: values.term,
      translation: values.translation,
      wordType: values.wordType,
      notes: values.notes || undefined,
      sortOrder: currentCount + 1,
    })
    reset()
    setShowAddWord(false)
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
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 16,
          padding: '80px 0',
        }}
      >
        <p className="ds-sm" style={{ color: 'var(--fg-3)' }}>
          Block not found or failed to load.
        </p>
        <Link
          to={ROUTES.BLOCKS}
          className="ds-code"
          style={{ color: 'var(--accent-color)', textDecoration: 'none' }}
        >
          ← Back to blocks
        </Link>
      </div>
    )
  }

  const { block, words } = data
  const langCode = LANGUAGES[block.languageId]?.code ?? String(block.languageId)
  const displayedWords = confidenceOnly ? words.items.filter((w) => w.confidenceFlag) : words.items

  return (
    <div>
      {/* Back */}
      <Link
        to={ROUTES.BLOCKS}
        style={{
          color: 'var(--accent-color)',
          textDecoration: 'none',
          display: 'inline-block',
          marginBottom: 16,
          fontSize: 14,
          fontWeight: 700,
        }}
      >
        ← Back to blocks
      </Link>

      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'start',
          justifyContent: 'space-between',
          gap: 16,
          flexWrap: 'wrap',
          marginBottom: 20,
        }}
      >
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 6 }}>
            <h1 className="ds-h2" style={{ margin: 0 }}>
              {block.title}
            </h1>
            <span
              style={{
                fontFamily: 'var(--font-body)',
                fontSize: 11,
                fontWeight: 700,
                padding: '4px 12px',
                borderRadius: 'var(--r-pill)',
                background: 'var(--accent-ghost)',
                border: '1px solid var(--accent-line)',
                color: 'var(--accent-dim)',
              }}
            >
              {langCode.toUpperCase()}
            </span>
          </div>
          <p className="ds-body" style={{ margin: 0, color: 'var(--fg-3)' }}>
            {block.wordCount} words
            {words.items.filter((w) => w.confidenceFlag).length > 0 && (
              <> · {words.items.filter((w) => w.confidenceFlag).length} flagged</>
            )}
          </p>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <button
            className="lx-btn-secondary"
            onClick={() => exportBlock.mutate(block.id)}
            disabled={exportBlock.isPending}
          >
            Export CSV
          </button>
          <button
            onClick={handleDeleteBlock}
            disabled={deleteBlock.isPending}
            style={{
              fontFamily: 'var(--font-body)',
              fontSize: 14,
              fontWeight: 600,
              borderRadius: 'var(--r-md)',
              padding: '10px 20px',
              cursor: 'pointer',
              border: '1px solid rgba(255,92,108,0.3)',
              background: 'transparent',
              color: 'var(--danger)',
              transition: 'all 0.12s',
            }}
          >
            Delete
          </button>
        </div>
      </div>

      {/* Tags */}
      <div style={{ marginBottom: 18 }}>
        <TagInput blockId={block.id} currentTags={block.tags} />
      </div>

      {/* Toolbar */}
      <div
        style={{
          display: 'flex',
          gap: 10,
          flexWrap: 'wrap',
          alignItems: 'center',
          marginBottom: 18,
        }}
      >
        <label
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 8,
            fontSize: 13,
            color: 'var(--fg-3)',
            cursor: 'pointer',
          }}
        >
          <span
            style={{
              width: 16,
              height: 16,
              borderRadius: 4,
              border: `1px solid ${confidenceOnly ? 'var(--accent-color)' : 'var(--line-3)'}`,
              background: confidenceOnly ? 'var(--accent-ghost)' : 'transparent',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              cursor: 'pointer',
              fontSize: 10,
              color: 'var(--accent-color)',
            }}
            onClick={() => setConfidenceOnly((v) => !v)}
          >
            {confidenceOnly ? '✓' : ''}
          </span>
          Confidence flagged only
        </label>
        <div style={{ flex: 1 }} />
        <Link to={ROUTES.WORD_IMPORT(block.id)} style={{ textDecoration: 'none' }}>
          <button className="lx-btn-secondary" style={{ padding: '8px 14px' }}>
            AI import
          </button>
        </Link>
        <button
          className="lx-btn-primary"
          style={{ padding: '8px 14px' }}
          onClick={() => setShowAddWord((v) => !v)}
        >
          {showAddWord ? 'Cancel' : '+ Add word'}
        </button>
      </div>

      {/* Add word form */}
      {showAddWord && (
        <form
          onSubmit={handleSubmit(onAddWord)}
          style={{
            marginBottom: 18,
            padding: 20,
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-lg)',
          }}
        >
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
              gap: 12,
            }}
          >
            <div>
              <input className="lx-input" placeholder="Term" {...register('term')} />
              {errors.term && (
                <p style={{ color: 'var(--danger)', fontSize: 12, marginTop: 4 }}>
                  {errors.term.message}
                </p>
              )}
            </div>
            <div>
              <input className="lx-input" placeholder="Translation" {...register('translation')} />
              {errors.translation && (
                <p style={{ color: 'var(--danger)', fontSize: 12, marginTop: 4 }}>
                  {errors.translation.message}
                </p>
              )}
            </div>
            <div>
              <select
                value={wordType}
                onChange={(e) => setValue('wordType', e.target.value)}
                className="lx-input"
                style={{ cursor: 'pointer' }}
              >
                {WORD_TYPES.map((t) => (
                  <option key={t} value={t} style={{ background: 'var(--bg-2)' }}>
                    {t.charAt(0).toUpperCase() + t.slice(1)}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <input className="lx-input" placeholder="Notes (optional)" {...register('notes')} />
            </div>
          </div>
          <div style={{ marginTop: 12, display: 'flex', justifyContent: 'flex-end' }}>
            <button
              type="submit"
              className="lx-btn-primary"
              disabled={isSubmitting || createWord.isPending}
              style={{ padding: '8px 20px' }}
            >
              Add
            </button>
          </div>
        </form>
      )}

      {/* Words table */}
      <div
        style={{
          background: 'var(--bg-2)',
          border: '1px solid var(--line-2)',
          borderRadius: 'var(--r-lg)',
          overflowX: 'auto',
        }}
      >
        {/* Table header */}
        <div
          style={{
            display: 'grid',
            minWidth: 560,
            gridTemplateColumns: '1.4fr 1.4fr 0.9fr 1.6fr 60px',
            gap: 12,
            padding: '12px 18px',
            background: 'var(--bg-1)',
            borderBottom: '1px solid var(--line-2)',
            fontFamily: 'var(--font-body)',
            fontSize: 11,
            letterSpacing: '0.1em',
            textTransform: 'uppercase',
            color: 'var(--fg-3)',
          }}
        >
          <div>Term</div>
          <div>Translation</div>
          <div>Type</div>
          <div>Notes</div>
          <div />
        </div>

        {displayedWords.length === 0 ? (
          <div
            style={{
              padding: '48px 18px',
              textAlign: 'center',
              color: 'var(--fg-3)',
              fontFamily: 'var(--font-body)',
              fontSize: 13,
            }}
          >
            {confidenceOnly ? 'No flagged words on this page' : 'No words yet — add some above'}
          </div>
        ) : (
          displayedWords.map((word) => <WordRow key={word.id} word={word} blockId={block.id} />)
        )}
      </div>

      {/* Pagination */}
      {words.totalPages > 1 && (
        <div
          style={{
            marginTop: 16,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 12,
          }}
        >
          <button
            className="lx-btn-secondary"
            disabled={!words.hasPreviousPage}
            onClick={() => setWordsPage((p) => p - 1)}
            style={{ padding: '8px 16px' }}
          >
            Previous
          </button>
          <span className="ds-code" style={{ color: 'var(--fg-3)' }}>
            {wordsPage} / {words.totalPages}
          </span>
          <button
            className="lx-btn-secondary"
            disabled={!words.hasNextPage}
            onClick={() => setWordsPage((p) => p + 1)}
            style={{ padding: '8px 16px' }}
          >
            Next
          </button>
        </div>
      )}
    </div>
  )
}
