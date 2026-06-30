import { useState } from 'react'
import { useUpdateWordMutation, useDeleteWordMutation } from '../api/wordApi'
import type { Word } from '../model/types'
import { WordTypeBadge } from './WordTypeBadge'

interface WordRowProps {
  word: Word
  blockId: string
}

const rowBase: React.CSSProperties = {
  display: 'grid',
  minWidth: 560,
  gridTemplateColumns: '1.4fr 1.4fr 0.9fr 1.6fr 60px',
  gap: 12,
  padding: '14px 18px',
  borderBottom: '1px solid var(--line-1)',
  alignItems: 'center',
}

export function WordRow({ word, blockId }: WordRowProps) {
  const [editField, setEditField] = useState<'translation' | 'notes' | null>(null)
  const [draftTranslation, setDraftTranslation] = useState(word.translation)
  const [draftNotes, setDraftNotes] = useState(word.notes ?? '')
  const updateWord = useUpdateWordMutation(blockId)
  const deleteWord = useDeleteWordMutation(blockId)

  const handleSave = async () => {
    await updateWord.mutateAsync({
      wordId: word.id,
      input: {
        translation: draftTranslation,
        notes: draftNotes || undefined,
        exampleSentence: word.exampleSentence ?? undefined,
        confidenceFlag: word.confidenceFlag,
        confidenceNote: word.confidenceNote ?? undefined,
      },
    })
    setEditField(null)
  }

  const handleCancel = () => {
    setDraftTranslation(word.translation)
    setDraftNotes(word.notes ?? '')
    setEditField(null)
  }

  const handleDelete = () => {
    if (!confirm(`Delete "${word.term}"?`)) return
    deleteWord.mutate(word.id)
  }

  return (
    <div
      style={{
        ...rowBase,
        borderLeft: word.confidenceFlag ? '2px solid var(--warning)' : '2px solid transparent',
      }}
    >
      {/* Term */}
      <div style={{ color: 'var(--fg-1)', fontWeight: 500, fontSize: 14 }}>{word.term}</div>

      {/* Translation */}
      <div>
        {editField === 'translation' ? (
          <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
            <input
              className="lx-input"
              style={{ padding: '5px 10px', fontSize: 13 }}
              value={draftTranslation}
              onChange={(e) => setDraftTranslation(e.target.value)}
              autoFocus
            />
            <button
              onClick={handleSave}
              disabled={updateWord.isPending}
              className="lx-btn-primary"
              style={{ padding: '5px 10px', fontSize: 12 }}
            >
              ✓
            </button>
            <button
              onClick={handleCancel}
              className="lx-btn-secondary"
              style={{ padding: '5px 10px', fontSize: 12 }}
            >
              ✕
            </button>
          </div>
        ) : (
          <button
            onClick={() => setEditField('translation')}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: 'var(--fg-2)',
              fontSize: 14,
              padding: 0,
              textAlign: 'left',
            }}
          >
            {word.translation}
          </button>
        )}
      </div>

      {/* Type */}
      <div>
        <WordTypeBadge type={word.wordType} />
      </div>

      {/* Notes */}
      <div>
        {editField === 'notes' ? (
          <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
            <input
              className="lx-input"
              style={{ padding: '5px 10px', fontSize: 13 }}
              value={draftNotes}
              onChange={(e) => setDraftNotes(e.target.value)}
              autoFocus
            />
            <button
              onClick={handleSave}
              disabled={updateWord.isPending}
              className="lx-btn-primary"
              style={{ padding: '5px 10px', fontSize: 12 }}
            >
              ✓
            </button>
            <button
              onClick={handleCancel}
              className="lx-btn-secondary"
              style={{ padding: '5px 10px', fontSize: 12 }}
            >
              ✕
            </button>
          </div>
        ) : (
          <button
            onClick={() => setEditField('notes')}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: word.notes ? 'var(--fg-3)' : 'var(--fg-4)',
              fontSize: 13,
              padding: 0,
              textAlign: 'left',
              fontStyle: word.notes ? 'normal' : 'italic',
            }}
          >
            {word.notes ?? 'add note'}
          </button>
        )}
      </div>

      {/* Delete */}
      <div style={{ textAlign: 'right' }}>
        <button
          onClick={handleDelete}
          disabled={deleteWord.isPending}
          style={{
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            color: 'var(--fg-4)',
            fontFamily: 'var(--font-mono)',
            fontSize: 13,
            transition: 'color 0.12s',
          }}
          onMouseEnter={(e) => {
            ;(e.currentTarget as HTMLButtonElement).style.color = 'var(--danger)'
          }}
          onMouseLeave={(e) => {
            ;(e.currentTarget as HTMLButtonElement).style.color = 'var(--fg-4)'
          }}
        >
          ✕
        </button>
      </div>
    </div>
  )
}
