import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
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
  Textarea,
} from '@/shared/ui'
import { useCreateBlockMutation } from '@/entities/block'

const schema = z.object({
  title: z.string().min(1, 'Title is required').max(100),
  languageId: z.number().int().min(1).max(9),
  description: z.string().max(500).optional(),
})

type FormValues = z.infer<typeof schema>

interface CreateBlockModalProps {
  open: boolean
  onClose: () => void
}

export function CreateBlockModal({ open, onClose }: CreateBlockModalProps) {
  const createBlock = useCreateBlockMutation()
  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { title: '', languageId: 1, description: '' },
  })

  const languageId = watch('languageId')

  const handleClose = () => {
    reset()
    onClose()
  }

  const onSubmit = async (values: FormValues) => {
    await createBlock.mutateAsync({
      title: values.title,
      languageId: values.languageId,
      description: values.description || undefined,
    })
    handleClose()
  }

  return (
    <Dialog open={open} onOpenChange={(v) => !v && handleClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>New word block</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-1">
            <Input placeholder="Title" {...register('title')} />
            {errors.title && <p className="text-xs text-destructive">{errors.title.message}</p>}
          </div>

          <div className="space-y-1">
            <Select
              value={String(languageId)}
              onValueChange={(v) => setValue('languageId', Number(v))}
            >
              <SelectTrigger>
                <SelectValue placeholder="Language" />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(LANGUAGES).map(([id, lang]) => (
                  <SelectItem key={id} value={id}>
                    {lang.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.languageId && (
              <p className="text-xs text-destructive">{errors.languageId.message}</p>
            )}
          </div>

          <div className="space-y-1">
            <Textarea placeholder="Description (optional)" rows={3} {...register('description')} />
            {errors.description && (
              <p className="text-xs text-destructive">{errors.description.message}</p>
            )}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || createBlock.isPending}>
              Create
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
