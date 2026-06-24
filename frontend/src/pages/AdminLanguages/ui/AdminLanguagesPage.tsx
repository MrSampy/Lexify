import { useState } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Input,
  Spinner,
} from '@/shared/ui'
import { useLanguages, useAddLanguageMutation, useToggleLanguageMutation } from '@/entities/admin'

export function AdminLanguagesPage() {
  const { data: languages, isLoading } = useLanguages()
  const addLanguage = useAddLanguageMutation()
  const toggleLanguage = useToggleLanguageMutation()

  const [showAdd, setShowAdd] = useState(false)
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [nativeName, setNativeName] = useState('')
  const [sortOrder, setSortOrder] = useState('0')

  const handleAdd = async () => {
    if (!code.trim() || !name.trim() || !nativeName.trim()) return
    await addLanguage.mutateAsync({
      code: code.trim(),
      name: name.trim(),
      nativeName: nativeName.trim(),
      sortOrder: Number(sortOrder) || 0,
    })
    setShowAdd(false)
    setCode('')
    setName('')
    setNativeName('')
    setSortOrder('0')
  }

  return (
    <div className="p-8">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-bold">Languages</h1>
        <Button onClick={() => setShowAdd(true)}>+ Add language</Button>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner size="lg" />
        </div>
      ) : (
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-16">Code</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Native name</TableHead>
                <TableHead className="w-16 text-right">Sort</TableHead>
                <TableHead className="w-24">Status</TableHead>
                <TableHead className="w-24" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {(languages ?? []).map((lang) => (
                <TableRow key={lang.id}>
                  <TableCell className="font-mono font-medium">{lang.code}</TableCell>
                  <TableCell>{lang.name}</TableCell>
                  <TableCell className="text-muted-foreground">{lang.nativeName}</TableCell>
                  <TableCell className="text-right text-sm">{lang.sortOrder}</TableCell>
                  <TableCell>
                    <Badge variant={lang.isActive ? 'default' : 'outline'}>
                      {lang.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-7 text-xs"
                      onClick={() => void toggleLanguage.mutateAsync(lang.code)}
                      disabled={toggleLanguage.isPending}
                    >
                      {lang.isActive ? 'Disable' : 'Enable'}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Add language dialog */}
      <Dialog open={showAdd} onOpenChange={setShowAdd}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Add language</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <div>
              <label className="mb-1 block text-sm font-medium">Code (e.g. "fr")</label>
              <Input value={code} onChange={(e) => setCode(e.target.value)} maxLength={10} />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Name (English)</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Native name</label>
              <Input value={nativeName} onChange={(e) => setNativeName(e.target.value)} />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Sort order</label>
              <Input
                type="number"
                value={sortOrder}
                onChange={(e) => setSortOrder(e.target.value)}
                className="w-24"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAdd(false)}>
              Cancel
            </Button>
            <Button
              onClick={() => void handleAdd()}
              disabled={addLanguage.isPending || !code || !name || !nativeName}
            >
              Add
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
