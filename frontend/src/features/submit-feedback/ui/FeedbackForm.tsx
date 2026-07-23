import { useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Lightbulb, Bug, Star, HelpCircle, type LucideIcon } from 'lucide-react'
import { Button, Checkbox, LxSelect, Spinner } from '@/shared/ui'
import { useAuthStore } from '@/entities/user'
import {
  FEEDBACK_CATEGORIES,
  useSubmitFeedbackMutation,
  type FeedbackType,
  type SubmitFeedbackResult,
} from '@/entities/feedback'
import { StarRating } from './StarRating'
import { AttachmentsInput } from './AttachmentsInput'

const schema = z
  .object({
    type: z.enum(['suggestion', 'bug', 'review', 'question']),
    category: z.string().optional(),
    subject: z.string().trim().min(5, 'feedback.subjectMin').max(150, 'feedback.subjectMax'),
    message: z.string().trim().min(10, 'feedback.messageMin').max(4000, 'feedback.messageMax'),
    rating: z.number().int().min(1).max(5).optional(),
    contactEmail: z.union([z.string().email('auth.emailInvalid'), z.literal('')]).optional(),
    // Plain boolean rather than z.literal(true) so the unchecked default is still a valid form
    // value — the refine below is what blocks submitting without consent.
    consent: z.boolean(),
  })
  .refine((v) => v.consent, { path: ['consent'], message: 'feedback.consentRequired' })
  // A rating only means something on a review — the server enforces the same rule.
  .refine((v) => v.type !== 'review' || v.rating != null, {
    path: ['rating'],
    message: 'feedback.ratingRequired',
  })

type FormValues = z.infer<typeof schema>

const TYPES: { value: FeedbackType; labelKey: string; icon: LucideIcon }[] = [
  { value: 'suggestion', labelKey: 'feedback.typeSuggestion', icon: Lightbulb },
  { value: 'bug', labelKey: 'feedback.typeBug', icon: Bug },
  { value: 'review', labelKey: 'feedback.typeReview', icon: Star },
  { value: 'question', labelKey: 'feedback.typeQuestion', icon: HelpCircle },
]

interface FeedbackFormProps {
  onSubmitted: (result: SubmitFeedbackResult) => void
}

export function FeedbackForm({ onSubmitted }: FeedbackFormProps) {
  const { t } = useTranslation()
  const user = useAuthStore((s) => s.user)
  const submit = useSubmitFeedbackMutation()

  const [attachments, setAttachments] = useState<File[]>([])

  const {
    control,
    register,
    handleSubmit,
    watch,
    setValue,
    setError,
    clearErrors,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      type: 'suggestion',
      category: '',
      subject: '',
      message: '',
      contactEmail: user?.email ?? '',
      consent: false,
    },
  })

  const type = watch('type')

  const onSubmit = async (values: FormValues) => {
    try {
      const result = await submit.mutateAsync({
        type: values.type,
        category: values.category || undefined,
        subject: values.subject.trim(),
        message: values.message.trim(),
        rating: values.type === 'review' ? values.rating : undefined,
        contactEmail: values.contactEmail || undefined,
        consent: true,
        attachments,
      })
      toast.success(t('feedback.successToast', { code: result.ticketCode }))
      onSubmitted(result)
    } catch (err: unknown) {
      const response = (err as { response?: { status?: number; data?: { message?: string } } })
        ?.response
      const message =
        response?.status === 429
          ? t('feedback.rateLimited')
          : (response?.data?.message ?? t('feedback.submitFailed'))
      setError('root', { message })
    }
  }

  const fieldError = (key: string | undefined) =>
    key ? (
      <p style={{ color: 'var(--danger)', fontSize: 13, marginTop: 4 }} role="alert">
        {t(key)}
      </p>
    ) : null

  return (
    <form onSubmit={handleSubmit(onSubmit)} style={{ display: 'grid', gap: 20 }}>
      {/* Type — first, because it decides whether the rating below is shown at all. */}
      <fieldset style={{ border: 'none', padding: 0, margin: 0 }}>
        <legend className="ds-sm" style={{ fontWeight: 700, marginBottom: 8 }}>
          {t('feedback.typeLabel')}
        </legend>
        <div
          role="radiogroup"
          aria-label={t('feedback.typeLabel')}
          style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}
        >
          {TYPES.map(({ value, labelKey, icon: Icon }) => {
            const active = type === value
            return (
              <button
                key={value}
                type="button"
                role="radio"
                aria-checked={active}
                onClick={() => {
                  setValue('type', value, { shouldValidate: false })
                  if (value !== 'review') setValue('rating', undefined)
                  clearErrors('rating')
                }}
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: 8,
                  padding: '9px 14px',
                  border: `1.5px solid ${active ? 'var(--accent-color)' : 'var(--line-2)'}`,
                  borderRadius: 'var(--r-pill)',
                  background: active ? 'var(--accent-color)' : 'var(--bg-1)',
                  color: active ? '#fff' : 'var(--fg-2)',
                  fontSize: 14,
                  fontWeight: 600,
                  cursor: 'pointer',
                }}
              >
                <Icon style={{ width: 16, height: 16 }} />
                {t(labelKey)}
              </button>
            )
          })}
        </div>
      </fieldset>

      {type === 'review' && (
        <div>
          <label className="ds-sm" style={{ fontWeight: 700, display: 'block', marginBottom: 8 }}>
            {t('feedback.ratingLabel')}
          </label>
          <Controller
            control={control}
            name="rating"
            render={({ field }) => (
              <StarRating
                value={field.value}
                onChange={(v) => field.onChange(v)}
                label={t('feedback.ratingLabel')}
              />
            )}
          />
          {fieldError(errors.rating?.message)}
        </div>
      )}

      <div>
        <label
          htmlFor="feedback-category"
          className="ds-sm"
          style={{ fontWeight: 700, display: 'block', marginBottom: 8 }}
        >
          {t('feedback.categoryLabel')}
        </label>
        <Controller
          control={control}
          name="category"
          render={({ field }) => (
            <LxSelect
              value={field.value || 'none'}
              onValueChange={(v) => field.onChange(v === 'none' ? '' : v)}
              triggerStyle={{ width: '100%', maxWidth: 280 }}
              options={[
                { value: 'none', label: t('feedback.categoryNone') },
                ...FEEDBACK_CATEGORIES.map((c) => ({
                  value: c,
                  label: t(`feedback.category.${c}`),
                })),
              ]}
            />
          )}
        />
      </div>

      <div>
        <label
          htmlFor="feedback-subject"
          className="ds-sm"
          style={{ fontWeight: 700, display: 'block', marginBottom: 8 }}
        >
          {t('feedback.subjectLabel')}
        </label>
        <input
          id="feedback-subject"
          type="text"
          className="lx-input"
          placeholder={t('feedback.subjectPlaceholder')}
          {...register('subject')}
        />
        {fieldError(errors.subject?.message)}
      </div>

      <div>
        <label
          htmlFor="feedback-message"
          className="ds-sm"
          style={{ fontWeight: 700, display: 'block', marginBottom: 8 }}
        >
          {t('feedback.messageLabel')}
        </label>
        <textarea
          id="feedback-message"
          className="lx-input"
          rows={7}
          placeholder={t(
            type === 'bug' ? 'feedback.messagePlaceholderBug' : 'feedback.messagePlaceholder',
          )}
          style={{ resize: 'vertical', minHeight: 140 }}
          {...register('message')}
        />
        {fieldError(errors.message?.message)}
      </div>

      <div>
        <label
          htmlFor="feedback-email"
          className="ds-sm"
          style={{ fontWeight: 700, display: 'block', marginBottom: 8 }}
        >
          {t('feedback.emailLabel')}
        </label>
        <input
          id="feedback-email"
          type="email"
          autoComplete="email"
          className="lx-input"
          style={{ maxWidth: 360 }}
          {...register('contactEmail')}
        />
        <p style={{ color: 'var(--fg-4)', fontSize: 12, margin: '6px 0 0' }}>
          {t('feedback.emailHint')}
        </p>
        {fieldError(errors.contactEmail?.message)}
      </div>

      <div>
        <label className="ds-sm" style={{ fontWeight: 700, display: 'block', marginBottom: 8 }}>
          {t('feedback.attachmentsLabel')}
        </label>
        <AttachmentsInput
          files={attachments}
          onChange={setAttachments}
          onReject={(reason) => toast.error(reason)}
        />
      </div>

      <div style={{ display: 'flex', alignItems: 'flex-start', gap: 10 }}>
        <Controller
          control={control}
          name="consent"
          render={({ field }) => (
            <Checkbox
              id="feedback-consent"
              checked={field.value}
              onCheckedChange={(checked) => field.onChange(checked === true)}
            />
          )}
        />
        <div>
          <label htmlFor="feedback-consent" style={{ fontSize: 14, cursor: 'pointer' }}>
            {t('feedback.consentLabel')}
          </label>
          {fieldError(errors.consent?.message)}
        </div>
      </div>

      {errors.root && (
        <p style={{ color: 'var(--danger)', fontSize: 14, margin: 0 }} role="alert">
          {errors.root.message}
        </p>
      )}

      <div>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? <Spinner size="sm" /> : t('feedback.submit')}
        </Button>
      </div>
    </form>
  )
}
