import { useTranslation } from 'react-i18next'
import { Button, Spinner } from '@/shared/ui'
import { BlockCard } from '@/entities/block'
import type { WordBlock, BlockFilter } from '@/entities/block'
import { useBlocks } from '@/entities/block'

interface BlockListProps {
  filter: BlockFilter
  onPageChange: (page: number) => void
}

export function BlockList({ filter, onPageChange }: BlockListProps) {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useBlocks(filter)

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError) {
    return <div className="py-16 text-center text-muted-foreground">{t('blocks.loadFailed')}</div>
  }

  if (!data || data.items.length === 0) {
    return <div className="py-16 text-center text-muted-foreground">{t('blocks.noBlocksYet')}</div>
  }

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {data.items.map((block: WordBlock) => (
          <BlockCard key={block.id} block={block} />
        ))}
      </div>

      {data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 pt-2">
          <Button
            variant="outline"
            size="sm"
            disabled={!data.hasPreviousPage}
            onClick={() => onPageChange(filter.page - 1)}
          >
            {t('common.previous')}
          </Button>
          <span className="text-sm text-muted-foreground">
            {filter.page} / {data.totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={!data.hasNextPage}
            onClick={() => onPageChange(filter.page + 1)}
          >
            {t('common.next')}
          </Button>
        </div>
      )}
    </div>
  )
}
