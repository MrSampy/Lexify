import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { HelpCircle } from 'lucide-react'
import { Button, Dialog, DialogContent, DialogDescription, DialogTitle } from '@/shared/ui'

const RULE_KEYS = ['rule1', 'rule2', 'rule3', 'rule4', 'rule5', 'rule6'] as const

/** Plain-language explanation of how pasted text gets turned into words — no technical terms. */
export function ImportHelpDialog() {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <Button
        type="button"
        variant="ghost"
        size="sm"
        onClick={() => setOpen(true)}
        className="h-auto gap-1 px-1.5 py-0.5 text-xs font-normal text-muted-foreground hover:text-foreground"
      >
        <HelpCircle className="h-3.5 w-3.5" />
        {t('import.help.trigger')}
      </Button>

      {/* Fixed header/footer, scrollable rule list — the content is too tall for short viewports otherwise. */}
      <DialogContent className="flex max-h-[85vh] w-full max-w-[min(32rem,calc(100%-2rem))] flex-col gap-0 p-0">
        <div className="flex-none space-y-2 p-4 pb-3">
          <DialogTitle>{t('import.help.title')}</DialogTitle>
          <DialogDescription>{t('import.help.intro')}</DialogDescription>
        </div>

        <ol className="flex-1 space-y-4 overflow-y-auto px-4 pb-4 text-sm">
          {RULE_KEYS.map((key, i) => (
            <li key={key} className="flex gap-3">
              <span className="flex h-5 w-5 flex-none items-center justify-center rounded-full bg-muted text-xs font-medium text-muted-foreground">
                {i + 1}
              </span>
              <div className="space-y-1">
                <p className="font-medium">{t(`import.help.${key}Title`)}</p>
                <p className="text-muted-foreground">{t(`import.help.${key}Desc`)}</p>
                {key !== 'rule6' && (
                  <code className="mt-1 block w-fit whitespace-pre-line rounded bg-muted px-2 py-1 font-mono text-xs">
                    {t(`import.help.${key}Example`)}
                  </code>
                )}
              </div>
            </li>
          ))}
        </ol>

        <div className="flex flex-none justify-end gap-2 rounded-b-xl border-t bg-muted/50 p-4">
          <Button onClick={() => setOpen(false)}>{t('import.help.close')}</Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
