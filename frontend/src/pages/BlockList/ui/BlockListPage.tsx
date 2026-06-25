import { useState } from 'react'
import { Button } from '@/shared/ui'
import { BlockList } from '@/widgets/BlockList'
import { BlockFilters } from '@/widgets/BlockList'
import { CreateBlockModal } from '@/features/create-block'
import { CsvImportModal } from '@/features/csv-import'

export function BlockListPage() {
  const [page, setPage] = useState(1)
  const [languageId, setLanguageId] = useState<number | undefined>()
  const [tag, setTag] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [showImport, setShowImport] = useState(false)

  const handleLanguageChange = (id: number | undefined) => {
    setLanguageId(id)
    setPage(1)
  }

  const handleTagChange = (v: string) => {
    setTag(v)
    setPage(1)
  }

  const filter = {
    languageId,
    tag: tag || undefined,
    page,
    pageSize: 18,
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-6xl px-4 py-8">
        <div className="mb-6 flex items-center justify-between">
          <h1 className="text-2xl font-bold">My Blocks</h1>
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => setShowImport(true)}>
              Import CSV
            </Button>
            <Button onClick={() => setShowCreate(true)}>+ New Block</Button>
          </div>
        </div>

        <div className="mb-4">
          <BlockFilters
            languageId={languageId}
            onLanguageChange={handleLanguageChange}
            tag={tag}
            onTagChange={handleTagChange}
          />
        </div>

        <BlockList filter={filter} onPageChange={setPage} />

        <CreateBlockModal open={showCreate} onClose={() => setShowCreate(false)} />
        <CsvImportModal open={showImport} onClose={() => setShowImport(false)} />
      </div>
    </div>
  )
}
