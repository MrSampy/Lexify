import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Badge,
  Spinner,
} from '@/shared/ui'
import { formatDate } from '@/shared/lib'
import type { AiLog } from '@/entities/admin'

interface AiLogTableProps {
  logs: AiLog[]
  isLoading: boolean
}

export function AiLogTable({ logs, isLoading }: AiLogTableProps) {
  if (isLoading) {
    return (
      <div className="flex justify-center py-8">
        <Spinner size="lg" />
      </div>
    )
  }

  if (logs.length === 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">No logs found.</p>
  }

  return (
    <div className="overflow-auto rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Call Type</TableHead>
            <TableHead>Provider</TableHead>
            <TableHead>Model</TableHead>
            <TableHead className="text-right">Duration</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Error</TableHead>
            <TableHead>Time</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {logs.map((log) => (
            <TableRow key={log.id}>
              <TableCell className="font-mono text-xs">{log.callType}</TableCell>
              <TableCell>
                <Badge variant="outline">{log.provider}</Badge>
              </TableCell>
              <TableCell className="max-w-32 truncate text-xs text-muted-foreground">
                {log.model}
              </TableCell>
              <TableCell className="text-right text-xs">{log.durationMs}ms</TableCell>
              <TableCell>
                {log.success ? (
                  <Badge className="bg-green-100 text-green-700 hover:bg-green-100">✓ OK</Badge>
                ) : (
                  <Badge variant="destructive">✗ Error</Badge>
                )}
              </TableCell>
              <TableCell className="max-w-48 truncate text-xs text-muted-foreground">
                {log.errorMessage ?? '—'}
              </TableCell>
              <TableCell className="text-xs text-muted-foreground">
                {formatDate(log.createdAt)}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}
