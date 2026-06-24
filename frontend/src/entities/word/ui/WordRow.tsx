import { useState } from 'react'
import { Button, Input, TableCell, TableRow } from '@/shared/ui'
import { ConfidenceBadge } from '@/shared/ui'
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
    <TableRow>
      <TableCell className="font-medium">
        {word.term}
        {word.confidenceFlag && <ConfidenceBadge flag className="ml-2" />}
      </TableCell>

      <TableCell>
        {editField === 'translation' ? (
          <div className="flex items-center gap-1">
            <Input
              value={draftTranslation}
              onChange={(e) => setDraftTranslation(e.target.value)}
              className="h-7 text-sm"
              autoFocus
            />
            <Button
              size="sm"
              className="h-7 px-2"
              onClick={handleSave}
              disabled={updateWord.isPending}
            >
              ✓
            </Button>
            <Button size="sm" variant="ghost" className="h-7 px-2" onClick={handleCancel}>
              ✕
            </Button>
          </div>
        ) : (
          <button
            className="cursor-pointer text-left hover:underline"
            onClick={() => setEditField('translation')}
          >
            {word.translation}
          </button>
        )}
      </TableCell>

      <TableCell>
        <WordTypeBadge type={word.wordType} />
      </TableCell>

      <TableCell>
        {editField === 'notes' ? (
          <div className="flex items-center gap-1">
            <Input
              value={draftNotes}
              onChange={(e) => setDraftNotes(e.target.value)}
              className="h-7 text-sm"
              autoFocus
            />
            <Button
              size="sm"
              className="h-7 px-2"
              onClick={handleSave}
              disabled={updateWord.isPending}
            >
              ✓
            </Button>
            <Button size="sm" variant="ghost" className="h-7 px-2" onClick={handleCancel}>
              ✕
            </Button>
          </div>
        ) : (
          <button
            className="cursor-pointer text-left text-sm text-muted-foreground hover:underline"
            onClick={() => setEditField('notes')}
          >
            {word.notes ?? <span className="italic">add note</span>}
          </button>
        )}
      </TableCell>

      <TableCell className="text-right">
        <Button
          size="sm"
          variant="ghost"
          onClick={handleDelete}
          disabled={deleteWord.isPending}
          className="h-7 px-2 text-destructive hover:text-destructive"
        >
          Delete
        </Button>
      </TableCell>
    </TableRow>
  )
}
