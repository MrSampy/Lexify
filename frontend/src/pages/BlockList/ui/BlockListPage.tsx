import { useState } from 'react'
import { Button } from '@/shared/ui'
import { BlockList } from '@/widgets/BlockList'
import { BlockFilters } from '@/widgets/BlockList'
import { CreateBlockModal } from '@/features/create-block'

export function BlockListPage() {
  const [page, setPage] = useState(1)
  const [languageId, setLanguageId] = useState<number | undefined>()
  const [search, setSearch] = useState('')
  const [showCreate, setShowCreate] = useState(false)

  const handleLanguageChange = (id: number | undefined) => {
    setLanguageId(id)
    setPage(1)
  }

  const handleSearchChange = (v: string) => {
    setSearch(v)
    setPage(1)
  }

  const filter = {
    languageId,
    tag: search || undefined,
    page,
    pageSize: 18,
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-6xl px-4 py-8">
        <div className="mb-6 flex items-center justify-between">
          <h1 className="text-2xl font-bold">My Blocks</h1>
          <Button onClick={() => setShowCreate(true)}>+ New Block</Button>
        </div>

        <div className="mb-4">
          <BlockFilters
            languageId={languageId}
            onLanguageChange={handleLanguageChange}
            search={search}
            onSearchChange={handleSearchChange}
          />
        </div>

        <BlockList filter={filter} onPageChange={setPage} />

        <CreateBlockModal open={showCreate} onClose={() => setShowCreate(false)} />
      </div>
    </div>
  )
}
