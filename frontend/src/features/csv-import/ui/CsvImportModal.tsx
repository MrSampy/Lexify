import { useRef, useState } from 'react'
import { toast } from 'sonner'
import { LANGUAGES } from '@/shared/config'
import {
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/shared/ui'
import { useCsvImportMutation } from '../api/csvImportApi'

interface CsvImportModalProps {
  open: boolean
  onClose: () => void
}

export function CsvImportModal({ open, onClose }: CsvImportModalProps) {
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string[][]>([])
  const [title, setTitle] = useState('')
  const [languageId, setLanguageId] = useState<string>('')
  const [isDragging, setIsDragging] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const importMutation = useCsvImportMutation()

  const readPreview = (f: File) => {
    const reader = new FileReader()
    reader.onload = (e) => {
      const text = e.target?.result as string
      const lines = text.split(/\r?\n/).filter((l) => l.trim())
      const rows = lines
        .slice(1, 11)
        .map((line) => line.split(',').map((c) => c.replace(/^"|"$/g, '')))
      setPreview(rows)
    }
    reader.readAsText(f)
  }

  const handleFileSelect = (f: File) => {
    if (!f.name.endsWith('.csv') && f.type !== 'text/csv') {
      toast.error('Please select a CSV file.')
      return
    }
    setFile(f)
    readPreview(f)
    if (!title) setTitle(f.name.replace(/\.csv$/i, '').replace(/_/g, ' '))
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    const f = e.dataTransfer.files[0]
    if (f) handleFileSelect(f)
  }

  const handleSubmit = async () => {
    if (!file || !title.trim() || !languageId) {
      toast.error('Please fill all fields and select a file.')
      return
    }

    const formData = new FormData()
    formData.append('title', title.trim())
    formData.append('languageId', languageId)
    formData.append('file', file)

    await importMutation.mutateAsync(formData)
    toast.success('Block imported successfully!')
    handleClose()
  }

  const handleClose = () => {
    setFile(null)
    setPreview([])
    setTitle('')
    setLanguageId('')
    onClose()
  }

  return (
    <Dialog open={open} onOpenChange={(v) => !v && handleClose()}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Import from CSV</DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          {/* Drop zone */}
          <div
            className={`cursor-pointer rounded-lg border-2 border-dashed p-8 text-center transition-colors ${
              isDragging
                ? 'border-primary bg-primary/5'
                : 'border-muted-foreground/30 hover:border-primary/50'
            }`}
            onDragOver={(e) => {
              e.preventDefault()
              setIsDragging(true)
            }}
            onDragLeave={() => setIsDragging(false)}
            onDrop={handleDrop}
            onClick={() => fileInputRef.current?.click()}
          >
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv,text/csv"
              className="hidden"
              onChange={(e) => {
                const f = e.target.files?.[0]
                if (f) handleFileSelect(f)
              }}
            />
            {file ? (
              <div>
                <p className="font-medium">{file.name}</p>
                <p className="text-sm text-muted-foreground">
                  {(file.size / 1024).toFixed(1)} KB — click to change
                </p>
              </div>
            ) : (
              <div>
                <p className="text-muted-foreground">
                  Drag & drop a CSV file here, or click to browse
                </p>
                <p className="mt-1 text-xs text-muted-foreground">
                  Expected columns: term, translation, wordType, notes, exampleSentence
                </p>
              </div>
            )}
          </div>

          {/* Form fields */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-sm font-medium">Block title</label>
              <Input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="My vocabulary block"
              />
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium">Language</label>
              <Select value={languageId} onValueChange={(v) => setLanguageId(v ?? '')}>
                <SelectTrigger>
                  <SelectValue placeholder="Select language" />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(LANGUAGES).map(([id, lang]) => (
                    <SelectItem key={id} value={id}>
                      {lang.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Preview */}
          {preview.length > 0 && (
            <div>
              <p className="mb-2 text-sm font-medium text-muted-foreground">
                Preview (first {preview.length} rows)
              </p>
              <div className="max-h-48 overflow-auto rounded-md border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="text-xs">Term</TableHead>
                      <TableHead className="text-xs">Translation</TableHead>
                      <TableHead className="text-xs">Type</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {preview.map((row, i) => (
                      <TableRow key={i}>
                        <TableCell className="text-xs">{row[0] ?? ''}</TableCell>
                        <TableCell className="text-xs">{row[1] ?? ''}</TableCell>
                        <TableCell className="text-xs text-muted-foreground">
                          {row[2] ?? 'word'}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleClose}>
            Cancel
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!file || !title.trim() || !languageId || importMutation.isPending}
          >
            {importMutation.isPending ? 'Importing…' : 'Import'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
