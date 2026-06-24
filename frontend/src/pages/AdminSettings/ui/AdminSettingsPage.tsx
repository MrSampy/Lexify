import { useState } from 'react'
import { Pencil, Check, X } from 'lucide-react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Button,
  Input,
  Badge,
  Spinner,
} from '@/shared/ui'
import { formatDate } from '@/shared/lib'
import { useSettings, useUpdateSettingMutation } from '@/entities/admin'

export function AdminSettingsPage() {
  const { data: settings, isLoading } = useSettings()
  const updateSetting = useUpdateSettingMutation()
  const [editingKey, setEditingKey] = useState<string | null>(null)
  const [editValue, setEditValue] = useState('')

  const handleEdit = (key: string, currentValue: string) => {
    setEditingKey(key)
    setEditValue(currentValue)
  }

  const handleSave = async (key: string) => {
    await updateSetting.mutateAsync({ key, value: editValue })
    setEditingKey(null)
  }

  const handleCancel = () => {
    setEditingKey(null)
    setEditValue('')
  }

  return (
    <div className="p-8">
      <h1 className="mb-6 text-2xl font-bold">System Settings</h1>

      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner size="lg" />
        </div>
      ) : (
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-56">Key</TableHead>
                <TableHead>Value</TableHead>
                <TableHead className="w-24">Type</TableHead>
                <TableHead>Description</TableHead>
                <TableHead className="w-32">Updated</TableHead>
                <TableHead className="w-20" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {(settings ?? []).map((s) => (
                <TableRow key={s.key}>
                  <TableCell className="font-mono text-sm font-medium">{s.key}</TableCell>
                  <TableCell>
                    {editingKey === s.key ? (
                      <div className="flex items-center gap-2">
                        <Input
                          value={editValue}
                          onChange={(e) => setEditValue(e.target.value)}
                          className="h-7 w-48 text-sm"
                          autoFocus
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') void handleSave(s.key)
                            if (e.key === 'Escape') handleCancel()
                          }}
                        />
                        <Button
                          size="sm"
                          className="h-7 w-7 p-0"
                          onClick={() => void handleSave(s.key)}
                          disabled={updateSetting.isPending}
                        >
                          <Check size={14} />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-7 w-7 p-0"
                          onClick={handleCancel}
                        >
                          <X size={14} />
                        </Button>
                      </div>
                    ) : (
                      <span className="font-mono text-sm">{s.value}</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className="text-xs">
                      {s.valueType}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {s.description ?? '—'}
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground">
                    {formatDate(s.updatedAt)}
                  </TableCell>
                  <TableCell>
                    {editingKey !== s.key && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 w-7 p-0"
                        onClick={() => handleEdit(s.key, s.value)}
                      >
                        <Pencil size={14} />
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}
