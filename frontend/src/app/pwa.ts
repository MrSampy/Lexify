import { createElement } from 'react'
import { registerSW } from 'virtual:pwa-register'
import { toast } from 'sonner'
import i18n from '@/shared/config/i18n'
import { Mascot } from '@/shared/ui'

/**
 * Service-worker registration with an update prompt: when a new build is deployed, the user
 * sees a toast and chooses when to reload, instead of the page silently swapping mid-session.
 */
export function setupPwa() {
  const updateSW = registerSW({
    onNeedRefresh() {
      toast(i18n.t('pwa.updateAvailable'), {
        duration: Infinity,
        icon: createElement(Mascot, { pose: 'builder', size: 32 }),
        action: {
          label: i18n.t('pwa.reload'),
          onClick: () => void updateSW(true),
        },
      })
    },
  })
}
