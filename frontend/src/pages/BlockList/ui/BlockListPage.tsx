import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { BlockList } from '@/widgets/BlockList'
import { BlockFilters } from '@/widgets/BlockList'
import { CreateBlockModal } from '@/features/create-block'
import { CsvImportModal } from '@/features/csv-import'

export function BlockListPage() {
  const { t } = useTranslation()
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

  const filter = { languageId, tag: tag || undefined, page, pageSize: 18 }

  return (
    <div>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'end',
          justifyContent: 'space-between',
          gap: 16,
          flexWrap: 'wrap',
          marginBottom: 22,
        }}
      >
        <div>
          <h1 className="ds-h2" style={{ margin: '0 0 4px' }}>
            {t('blocks.title')}
          </h1>
          <p className="ds-body" style={{ margin: 0, color: 'var(--fg-3)' }}>
            {t('blocks.subtitle')}
          </p>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <button className="lx-btn-secondary" onClick={() => setShowImport(true)}>
            {t('blocks.importCsv')}
          </button>
          <button className="lx-btn-primary" onClick={() => setShowCreate(true)}>
            {t('blocks.newBlock')}
          </button>
        </div>
      </div>

      {/* Filters */}
      <div style={{ marginBottom: 22 }}>
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
  )
}
