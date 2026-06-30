import { useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { ROUTES, LANGUAGES } from '@/shared/config'
import {
  Button,
  Spinner,
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
  Checkbox,
  LanguageBadge,
} from '@/shared/ui'
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
      <div className="flex min-h-screen items-center justify-center">
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError || !data) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4">
        <p className="text-muted-foreground">Block not found or failed to load.</p>
        <Link to={ROUTES.BLOCKS} className="text-primary hover:underline">
          Back to blocks
        </Link>
      </div>
    )
  }

  const { block, words } = data
  const langCode = LANGUAGES[block.languageId]?.code ?? String(block.languageId)

  const displayedWords = confidenceOnly ? words.items.filter((w) => w.confidenceFlag) : words.items

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-5xl px-4 py-8">
        {/* Header */}
        <div className="mb-6">
          <Link
            to={ROUTES.BLOCKS}
            className="mb-2 inline-block text-sm text-muted-foreground hover:underline"
          >
            ← Back to blocks
          </Link>
          <div className="flex items-start justify-between gap-4">
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <h1 className="text-2xl font-bold">{block.title}</h1>
                <LanguageBadge code={langCode} />
              </div>
              {block.description && (
                <p className="text-sm text-muted-foreground">{block.description}</p>
              )}
              <p className="text-xs text-muted-foreground">
                {block.wordCount} {block.wordCount === 1 ? 'word' : 'words'}
              </p>
            </div>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => exportBlock.mutate(block.id)}
                disabled={exportBlock.isPending}
              >
                Export CSV
              </Button>
              <Button
                variant="destructive"
                size="sm"
                onClick={handleDeleteBlock}
                disabled={deleteBlock.isPending}
              >
                Delete block
              </Button>
            </div>
          </div>
        </div>

        {/* Tags */}
        <div className="mb-4">
          <TagInput blockId={block.id} currentTags={block.tags} />
        </div>

        {/* Toolbar */}
        <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <Checkbox
              id="confidence"
              checked={confidenceOnly}
              onCheckedChange={(v) => setConfidenceOnly(Boolean(v))}
            />
            <label htmlFor="confidence" className="cursor-pointer text-sm">
              Confidence flagged only
            </label>
          </div>
          <div className="flex gap-2">
            <Link to={ROUTES.WORD_IMPORT(block.id)}>
              <Button variant="outline" size="sm">
                AI Import
              </Button>
            </Link>
            <Button size="sm" onClick={() => setShowAddWord((v) => !v)}>
              {showAddWord ? 'Cancel' : '+ Add word'}
            </Button>
          </div>
        </div>

        {/* Add word form */}
        {showAddWord && (
          <form
            onSubmit={handleSubmit(onAddWord)}
            className="mb-4 rounded-lg border bg-card p-4 shadow-sm"
          >
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div className="space-y-1">
                <Input placeholder="Term" {...register('term')} />
                {errors.term && <p className="text-xs text-destructive">{errors.term.message}</p>}
              </div>
              <div className="space-y-1">
                <Input placeholder="Translation" {...register('translation')} />
                {errors.translation && (
                  <p className="text-xs text-destructive">{errors.translation.message}</p>
                )}
              </div>
              <div className="space-y-1">
                <Select value={wordType} onValueChange={(v) => v && setValue('wordType', v)}>
                  <SelectTrigger>
                    <SelectValue placeholder="Word type">
                      {wordType ? wordType.charAt(0).toUpperCase() + wordType.slice(1) : undefined}
                    </SelectValue>
                  </SelectTrigger>
                  <SelectContent>
                    {WORD_TYPES.map((t) => (
                      <SelectItem key={t} value={t} className="capitalize">
                        {t}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Input placeholder="Notes (optional)" {...register('notes')} />
              </div>
            </div>
            <div className="mt-3 flex justify-end">
              <Button type="submit" size="sm" disabled={isSubmitting || createWord.isPending}>
                Add
              </Button>
            </div>
          </form>
        )}

        {/* Words table */}
        <div className="rounded-lg border bg-card shadow-sm">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Term</TableHead>
                <TableHead>Translation</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Notes</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {displayedWords.length === 0 ? (
                <TableRow>
                  <td colSpan={5} className="py-10 text-center text-sm text-muted-foreground">
                    {confidenceOnly ? 'No flagged words on this page.' : 'No words yet.'}
                  </td>
                </TableRow>
              ) : (
                displayedWords.map((word) => (
                  <WordRow key={word.id} word={word} blockId={block.id} />
                ))
              )}
            </TableBody>
          </Table>
        </div>

        {/* Pagination */}
        {words.totalPages > 1 && (
          <div className="mt-4 flex items-center justify-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!words.hasPreviousPage}
              onClick={() => setWordsPage((p) => p - 1)}
            >
              Previous
            </Button>
            <span className="text-sm text-muted-foreground">
              {wordsPage} / {words.totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={!words.hasNextPage}
              onClick={() => setWordsPage((p) => p + 1)}
            >
              Next
            </Button>
          </div>
        )}
      </div>
    </div>
  )
}
