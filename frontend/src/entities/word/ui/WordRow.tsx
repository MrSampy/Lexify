import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button, Checkbox, SpeakButton, useConfirm, ChipListInput } from '@/shared/ui'
import { masteryInfo } from '@/shared/lib'
import { useUpdateWordMutation, useDeleteWordMutation } from '../api/wordApi'
import type { Word } from '../model/types'
import { WordTypeBadge } from './WordTypeBadge'
import { WordHistoryPanel } from './WordHistoryPanel'

interface WordRowProps {
  word: Word
  blockId: string
  /** Language of the block — enables the pronunciation button when a voice exists. */
  languageId?: number
  /** When provided, the row renders a selection checkbox (bulk actions). */
  selected?: boolean
  onSelectedChange?: (selected: boolean) => void
}

export function WordRow({ word, blockId, languageId, selected, onSelectedChange }: WordRowProps) {
  const { t } = useTranslation()
  const [editField, setEditField] = useState<
    'translation' | 'notes' | 'synonyms' | 'alternatives' | 'example' | null
  >(null)
  const [draftTranslation, setDraftTranslation] = useState(word.translation)
  const [historyOpen, setHistoryOpen] = useState(false)
  const [draftNotes, setDraftNotes] = useState(word.notes ?? '')
  const [draftExample, setDraftExample] = useState(word.exampleSentence ?? '')
  // Synonyms and alternative translations auto-save on every add/remove (like tags) rather than via
  // an explicit ✓/✕ — so there is no "cancel" that could discard an addition and no
  // save-reads-stale-draft race.
  const [synonyms, setSynonyms] = useState<string[]>(word.synonyms ?? [])
  const [alternatives, setAlternatives] = useState<string[]>(word.alternativeTranslations ?? [])
  const updateWord = useUpdateWordMutation(blockId)
  const deleteWord = useDeleteWordMutation(blockId)
  const { confirm, confirmDialog } = useConfirm()

  // Re-sync local chip lists when the word refetches (e.g. after our own mutation invalidates it).
  // Render-time adjustment instead of an effect — see react.dev/learn/you-might-not-need-an-effect.
  const [prevWordSynonyms, setPrevWordSynonyms] = useState(word.synonyms)
  if (prevWordSynonyms !== word.synonyms) {
    setPrevWordSynonyms(word.synonyms)
    setSynonyms(word.synonyms ?? [])
  }

  const [prevWordAlternatives, setPrevWordAlternatives] = useState(word.alternativeTranslations)
  if (prevWordAlternatives !== word.alternativeTranslations) {
    setPrevWordAlternatives(word.alternativeTranslations)
    setAlternatives(word.alternativeTranslations ?? [])
  }

  const handleSave = async () => {
    await updateWord.mutateAsync({
      wordId: word.id,
      input: {
        translation: draftTranslation,
        notes: draftNotes || undefined,
        exampleSentence: draftExample || undefined,
        confidenceFlag: word.confidenceFlag,
        confidenceNote: word.confidenceNote ?? undefined,
        alternativeTranslations: alternatives,
        synonyms,
      },
    })
    setEditField(null)
  }

  const handleCancel = () => {
    setDraftTranslation(word.translation)
    setDraftNotes(word.notes ?? '')
    setDraftExample(word.exampleSentence ?? '')
    setEditField(null)
  }

  // The confidence flag marks a word as needing extra attention; toggle it in place, preserving
  // every other field (the update command replaces the whole word).
  const toggleConfidence = () => {
    updateWord.mutate({
      wordId: word.id,
      input: {
        translation: word.translation,
        notes: word.notes ?? undefined,
        exampleSentence: word.exampleSentence ?? undefined,
        confidenceFlag: !word.confidenceFlag,
        confidenceNote: word.confidenceNote ?? undefined,
        alternativeTranslations: alternatives,
        synonyms,
      },
    })
  }

  const mastery = masteryInfo(word.repetitions, word.intervalDays)

  const persistSynonyms = (next: string[]) => {
    setSynonyms(next)
    updateWord.mutate({
      wordId: word.id,
      input: {
        translation: word.translation,
        notes: word.notes ?? undefined,
        exampleSentence: word.exampleSentence ?? undefined,
        confidenceFlag: word.confidenceFlag,
        confidenceNote: word.confidenceNote ?? undefined,
        alternativeTranslations: alternatives,
        synonyms: next,
      },
    })
  }

  const persistAlternatives = (next: string[]) => {
    setAlternatives(next)
    updateWord.mutate({
      wordId: word.id,
      input: {
        translation: word.translation,
        notes: word.notes ?? undefined,
        exampleSentence: word.exampleSentence ?? undefined,
        confidenceFlag: word.confidenceFlag,
        confidenceNote: word.confidenceNote ?? undefined,
        alternativeTranslations: next,
        synonyms,
      },
    })
  }

  const handleDelete = async () => {
    if (!(await confirm({ title: t('words.deleteConfirm', { term: word.term }) }))) return
    deleteWord.mutate(word.id, {
      onSuccess: () => toast.success(t('words.deleted')),
      onError: () => toast.error(t('words.deleteFailed')),
    })
  }

  const selectable = onSelectedChange !== undefined

  return (
    // Mobile: stacked card (1 column); desktop (md+): the original table row
    // (+ a leading checkbox column when bulk selection is enabled).
    <div
      className={`relative grid grid-cols-1 gap-1.5 border-b border-b-[var(--line-1)] border-l-2 py-3.5 pr-[18px] md:min-w-[560px] md:items-center md:gap-3 ${
        selectable
          ? 'pl-8 md:grid-cols-[28px_1.4fr_1.4fr_0.9fr_1.6fr_60px] md:pl-[18px]'
          : 'pl-[18px] md:grid-cols-[1.4fr_1.4fr_0.9fr_1.6fr_60px]'
      } ${word.confidenceFlag ? 'border-l-[var(--warning)]' : 'border-l-transparent'}`}
    >
      {/* Selection checkbox — inline with the term on mobile, own column on desktop */}
      {selectable && (
        <div className="absolute top-4 left-1 md:static">
          <Checkbox
            checked={selected ?? false}
            onCheckedChange={(v) => onSelectedChange(v === true)}
            aria-label={word.term}
          />
        </div>
      )}

      {/* Term */}
      <div
        style={{
          color: 'var(--fg-1)',
          fontWeight: 500,
          fontSize: 14,
          display: 'flex',
          alignItems: 'center',
          gap: 8,
        }}
      >
        <span>{word.term}</span>
        <SpeakButton text={word.term} wordId={word.id} languageId={languageId} />
        {/* Mastery dot doubles as the review-history trigger */}
        <button
          onClick={() => setHistoryOpen(true)}
          title={`${t(mastery.labelKey)} — ${t('words.showHistory')}`}
          aria-label={t('words.showHistory')}
          className="cursor-pointer border-none bg-transparent p-0 leading-none"
          style={{ display: 'flex', alignItems: 'center', flexShrink: 0 }}
        >
          <span
            style={{
              width: 8,
              height: 8,
              borderRadius: '50%',
              background: mastery.color,
            }}
          />
        </button>
        <button
          onClick={toggleConfidence}
          disabled={updateWord.isPending}
          title={t('words.toggleConfidence')}
          aria-pressed={word.confidenceFlag}
          className="cursor-pointer border-none bg-transparent p-0 text-[13px] leading-none transition-opacity"
          style={{ opacity: word.confidenceFlag ? 1 : 0.35 }}
        >
          🚩
        </button>
      </div>

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
        {/* Alternative translations — same auto-saving chip editor as synonyms below */}
        {editField === 'alternatives' ? (
          <div style={{ marginTop: 6 }}>
            <ChipListInput
              value={alternatives}
              onChange={persistAlternatives}
              placeholder={t('words.addAlternative')}
            />
            <button
              onClick={() => setEditField(null)}
              className="lx-btn-secondary"
              style={{ marginTop: 6, padding: '4px 12px', fontSize: 12 }}
            >
              {t('common.done')}
            </button>
          </div>
        ) : (
          <button
            onClick={() => setEditField('alternatives')}
            className="cursor-pointer border-none bg-transparent p-0 text-left text-xs transition-colors duration-100 [font-family:var(--font-body)]"
            style={{
              display: 'block',
              width: 'fit-content',
              marginTop: 2,
              color: 'var(--fg-4)',
            }}
          >
            {alternatives.length > 0
              ? `${t('words.also')} ${alternatives.join(', ')}`
              : t('words.addAlternative')}
          </button>
        )}
        {/* Synonyms — chips auto-save on add/remove; "Done" only closes the editor */}
        {editField === 'synonyms' ? (
          <div style={{ marginTop: 6 }}>
            <ChipListInput
              value={synonyms}
              onChange={persistSynonyms}
              placeholder={t('words.addSynonym')}
            />
            <button
              onClick={() => setEditField(null)}
              className="lx-btn-secondary"
              style={{ marginTop: 6, padding: '4px 12px', fontSize: 12 }}
            >
              {t('common.done')}
            </button>
          </div>
        ) : (
          <button
            onClick={() => setEditField('synonyms')}
            className="cursor-pointer border-none bg-transparent p-0 text-left text-xs transition-colors duration-100 [font-family:var(--font-body)]"
            style={{
              display: 'block',
              width: 'fit-content',
              marginTop: 3,
              color: synonyms.length ? 'var(--accent-dim)' : 'var(--fg-4)',
            }}
          >
            {synonyms.length > 0
              ? `${t('words.synonyms')} ${synonyms.join(', ')}`
              : t('words.addSynonym')}
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
          <Button
            variant="ghost"
            onClick={() => setEditField('notes')}
            className={`h-auto cursor-pointer justify-start rounded-none border-none p-0 text-left text-[13px] font-normal whitespace-normal hover:bg-transparent ${
              word.notes ? 'text-[var(--fg-3)]' : 'italic text-[var(--fg-4)]'
            }`}
          >
            {word.notes ?? t('words.addNote')}
          </Button>
        )}

        {/* Example sentence — same inline-edit pattern as notes */}
        {editField === 'example' ? (
          <div style={{ display: 'flex', gap: 6, alignItems: 'center', marginTop: 6 }}>
            <input
              className="lx-input"
              style={{ padding: '5px 10px', fontSize: 13 }}
              value={draftExample}
              onChange={(e) => setDraftExample(e.target.value)}
              placeholder={t('words.addExample')}
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
            onClick={() => setEditField('example')}
            className="mt-1 block w-fit cursor-pointer border-none bg-transparent p-0 text-left text-xs italic transition-colors duration-100 [font-family:var(--font-body)]"
            style={{ color: word.exampleSentence ? 'var(--fg-4)' : 'var(--fg-4)' }}
          >
            {word.exampleSentence ? `“${word.exampleSentence}”` : t('words.addExample')}
          </button>
        )}
      </div>

      {/* Delete — pinned to the card's top-right corner on mobile, last column on desktop */}
      <div className="absolute top-3 right-3 md:static md:text-right">
        <button
          onClick={() => void handleDelete()}
          disabled={deleteWord.isPending}
          className="cursor-pointer border-none bg-transparent text-[13px] text-[var(--fg-4)] transition-colors duration-100 hover:text-[var(--danger)] [font-family:var(--font-body)]"
        >
          ✕
        </button>
      </div>
      {confirmDialog}
      {historyOpen && (
        <WordHistoryPanel word={word} open={historyOpen} onOpenChange={setHistoryOpen} />
      )}
    </div>
  )
}
