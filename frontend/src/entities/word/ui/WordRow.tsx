import { useState } from 'react'
import { Button } from '@/shared/ui'
import { useUpdateWordMutation, useDeleteWordMutation } from '../api/wordApi'
import type { Word } from '../model/types'
import { WordTypeBadge } from './WordTypeBadge'

interface WordRowProps {
  word: Word
  blockId: string
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
      className={`grid min-w-[560px] grid-cols-[1.4fr_1.4fr_0.9fr_1.6fr_60px] items-center gap-3 border-b border-b-[var(--line-1)] border-l-2 px-[18px] py-3.5 ${
        word.confidenceFlag ? 'border-l-[var(--warning)]' : 'border-l-transparent'
      }`}
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
          <Button
            variant="ghost"
            onClick={() => setEditField('translation')}
            className="h-auto cursor-pointer justify-start rounded-none border-none p-0 text-left text-sm font-normal whitespace-normal text-[var(--fg-2)] hover:bg-transparent"
          >
            {word.translation}
          </Button>
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
          <Button
            variant="ghost"
            onClick={() => setEditField('notes')}
            className={`h-auto cursor-pointer justify-start rounded-none border-none p-0 text-left text-[13px] font-normal whitespace-normal hover:bg-transparent ${
              word.notes ? 'text-[var(--fg-3)]' : 'italic text-[var(--fg-4)]'
            }`}
          >
            {word.notes ?? 'add note'}
          </Button>
        )}
      </div>

      {/* Delete */}
      <div className="text-right">
        <button
          onClick={handleDelete}
          disabled={deleteWord.isPending}
          className="cursor-pointer border-none bg-transparent text-[13px] text-[var(--fg-4)] transition-colors duration-100 hover:text-[var(--danger)] [font-family:var(--font-body)]"
        >
          ✕
        </button>
      </div>
    </div>
  )
}
