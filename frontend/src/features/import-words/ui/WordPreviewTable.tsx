import { Trash2, Plus } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import {
  Button,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Table,
  TableBody,
  TableHead,
  TableHeader,
  TableRow,
  ConfidenceBadge,
  ChipListInput,
} from '@/shared/ui'
import { useIsMobile } from '@/shared/lib'
import { useImportWordsStore } from '../model/store'
import type { EditableWord } from '../model/types'

const WORD_TYPES = ['word', 'phrase', 'idiom', 'expression']

interface RowProps {
  word: EditableWord
  onUpdate: (updates: Partial<EditableWord>) => void
  onRemove: () => void
}

function WordPreviewRow({ word, onUpdate, onRemove }: RowProps) {
  const { t } = useTranslation()
  return (
    <TableRow className={word.confidenceFlag ? 'bg-amber-500/10' : undefined}>
      <td className="p-0">
        <Input
          value={word.term}
          onChange={(e) => onUpdate({ term: e.target.value })}
          placeholder={t('words.term')}
          className="h-9 rounded-none border-0 bg-transparent px-3 focus-visible:ring-inset"
        />
      </td>
      <td className="p-0">
        <Input
          value={word.translation}
          onChange={(e) => onUpdate({ translation: e.target.value })}
          placeholder={t('words.translation')}
          className="h-9 rounded-none border-0 bg-transparent px-3 focus-visible:ring-inset"
        />
        {word.alternativeTranslations && word.alternativeTranslations.length > 0 && (
          <div className="px-3 pb-1 text-xs text-muted-foreground">
            {t('words.also')} {word.alternativeTranslations.join(', ')}
          </div>
        )}
        <div className="px-3 pb-2 pt-1">
          <div className="mb-1 text-xs text-muted-foreground">{t('words.synonyms')}</div>
          <ChipListInput
            value={word.synonyms ?? []}
            onChange={(synonyms) => onUpdate({ synonyms })}
            placeholder={t('words.addSynonym')}
          />
        </div>
      </td>
      <td className="w-36 p-1">
        <Select value={word.wordType} onValueChange={(v) => v && onUpdate({ wordType: v })}>
          <SelectTrigger className="h-7 text-xs capitalize">
            <SelectValue>{word.wordType}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            {WORD_TYPES.map((t) => (
              <SelectItem key={t} value={t} className="capitalize text-xs">
                {t}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </td>
      <td className="w-8 p-1 text-center">
        <ConfidenceBadge flag={word.confidenceFlag} />
      </td>
      <td className="w-8 p-1">
        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7 text-muted-foreground hover:text-destructive"
          onClick={onRemove}
        >
          <Trash2 className="h-3.5 w-3.5" />
        </Button>
      </td>
    </TableRow>
  )
}

/**
 * Mobile equivalent of a WordPreviewRow — a real <table> squeezes every column into a sliver of a
 * phone-width viewport (term/translation inputs clip mid-word). Stack the same fields vertically
 * in a card instead, mirroring how WordRow does it in the block-detail list.
 */
function WordPreviewCard({ word, onUpdate, onRemove }: RowProps) {
  const { t } = useTranslation()
  return (
    <div
      className={`relative space-y-2 rounded-lg border bg-card p-3 ${
        word.confidenceFlag ? 'border-amber-500/40 bg-amber-500/10' : ''
      }`}
    >
      <Button
        variant="ghost"
        size="icon"
        className="absolute right-2 top-2 h-7 w-7 text-muted-foreground hover:text-destructive"
        onClick={onRemove}
      >
        <Trash2 className="h-3.5 w-3.5" />
      </Button>

      <div className="flex items-center gap-2 pr-8">
        <Input
          value={word.term}
          onChange={(e) => onUpdate({ term: e.target.value })}
          placeholder={t('words.term')}
          className="h-9 flex-1 font-medium"
        />
        <ConfidenceBadge flag={word.confidenceFlag} />
      </div>

      <div>
        <Input
          value={word.translation}
          onChange={(e) => onUpdate({ translation: e.target.value })}
          placeholder={t('words.translation')}
          className="h-9"
        />
        {word.alternativeTranslations && word.alternativeTranslations.length > 0 && (
          <div className="pt-1 text-xs text-muted-foreground">
            {t('words.also')} {word.alternativeTranslations.join(', ')}
          </div>
        )}
      </div>

      <div>
        <div className="mb-1 text-xs text-muted-foreground">{t('words.synonyms')}</div>
        <ChipListInput
          value={word.synonyms ?? []}
          onChange={(synonyms) => onUpdate({ synonyms })}
          placeholder={t('words.addSynonym')}
        />
      </div>

      <Select value={word.wordType} onValueChange={(v) => v && onUpdate({ wordType: v })}>
        <SelectTrigger className="h-8 w-full text-xs capitalize">
          <SelectValue>{word.wordType}</SelectValue>
        </SelectTrigger>
        <SelectContent>
          {WORD_TYPES.map((t) => (
            <SelectItem key={t} value={t} className="capitalize text-xs">
              {t}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  )
}

export function WordPreviewTable() {
  const { t } = useTranslation()
  const isMobile = useIsMobile()
  const formattedWords = useImportWordsStore((s) => s.formattedWords)
  const updateWord = useImportWordsStore((s) => s.updateWord)
  const removeWord = useImportWordsStore((s) => s.removeWord)
  const addWord = useImportWordsStore((s) => s.addWord)

  return (
    <div className="space-y-2">
      {formattedWords.length === 0 ? (
        <div className="rounded-lg border bg-card py-10 text-center text-sm text-muted-foreground">
          {t('import.noRows')}
        </div>
      ) : isMobile ? (
        <div className="space-y-2">
          {formattedWords.map((word) => (
            <WordPreviewCard
              key={word._id}
              word={word}
              onUpdate={(updates) => updateWord(word._id, updates)}
              onRemove={() => removeWord(word._id)}
            />
          ))}
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('words.term')}</TableHead>
                <TableHead>{t('words.translation')}</TableHead>
                <TableHead className="w-36">{t('words.type')}</TableHead>
                <TableHead className="w-8" />
                <TableHead className="w-8" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {formattedWords.map((word) => (
                <WordPreviewRow
                  key={word._id}
                  word={word}
                  onUpdate={(updates) => updateWord(word._id, updates)}
                  onRemove={() => removeWord(word._id)}
                />
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <Button variant="outline" size="sm" onClick={addWord} className="flex items-center gap-1.5">
        <Plus className="h-3.5 w-3.5" />
        {t('import.addRow')}
      </Button>
    </div>
  )
}
