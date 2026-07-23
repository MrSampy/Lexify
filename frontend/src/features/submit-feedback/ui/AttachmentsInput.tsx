import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Paperclip, X } from 'lucide-react'

/** Mirrors AttachmentRules on the backend — the server re-checks both. */
export const MAX_ATTACHMENTS = 3
export const MAX_ATTACHMENT_BYTES = 5 * 1024 * 1024
const ACCEPT = 'image/png,image/jpeg,image/webp,application/pdf'

interface AttachmentsInputProps {
  files: File[]
  onChange: (files: File[]) => void
  onReject: (reason: string) => void
}

function formatSize(bytes: number): string {
  return bytes < 1024 * 1024
    ? `${Math.round(bytes / 1024)} KB`
    : `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function AttachmentsInput({ files, onChange, onReject }: AttachmentsInputProps) {
  const { t } = useTranslation()
  const inputRef = useRef<HTMLInputElement>(null)

  const handlePick = (picked: FileList | null) => {
    if (!picked) return

    const accepted = [...files]
    for (const file of picked) {
      if (accepted.length >= MAX_ATTACHMENTS) {
        onReject(t('feedback.tooManyFiles', { max: MAX_ATTACHMENTS }))
        break
      }
      if (file.size > MAX_ATTACHMENT_BYTES) {
        onReject(t('feedback.fileTooLarge', { name: file.name, max: 5 }))
        continue
      }
      accepted.push(file)
    }

    onChange(accepted)
    // Reset so picking the same file twice in a row still fires a change event.
    if (inputRef.current) inputRef.current.value = ''
  }

  return (
    <div>
      <input
        ref={inputRef}
        type="file"
        multiple
        accept={ACCEPT}
        onChange={(e) => handlePick(e.target.files)}
        style={{ display: 'none' }}
      />

      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        disabled={files.length >= MAX_ATTACHMENTS}
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 8,
          padding: '9px 14px',
          border: '1.5px dashed var(--line-2)',
          borderRadius: 'var(--r-sm)',
          background: 'transparent',
          color: 'var(--fg-2)',
          fontSize: 14,
          fontWeight: 600,
          cursor: files.length >= MAX_ATTACHMENTS ? 'not-allowed' : 'pointer',
          opacity: files.length >= MAX_ATTACHMENTS ? 0.5 : 1,
        }}
      >
        <Paperclip style={{ width: 16, height: 16 }} />
        {t('feedback.addFiles')}
      </button>

      <p style={{ color: 'var(--fg-4)', fontSize: 12, margin: '6px 0 0' }}>
        {t('feedback.filesHint', { max: MAX_ATTACHMENTS })}
      </p>

      {files.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0, margin: '10px 0 0', display: 'grid', gap: 6 }}>
          {files.map((file, index) => (
            <li
              key={`${file.name}-${index}`}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 10,
                padding: '8px 10px',
                background: 'var(--bg-3)',
                border: '1px solid var(--line-2)',
                borderRadius: 'var(--r-sm)',
                fontSize: 13,
              }}
            >
              <span
                style={{
                  flex: 1,
                  minWidth: 0,
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {file.name}
              </span>
              <span style={{ color: 'var(--fg-4)', flexShrink: 0 }}>{formatSize(file.size)}</span>
              <button
                type="button"
                aria-label={t('feedback.removeFile', { name: file.name })}
                onClick={() => onChange(files.filter((_, i) => i !== index))}
                style={{
                  display: 'flex',
                  border: 'none',
                  background: 'transparent',
                  color: 'var(--fg-3)',
                  cursor: 'pointer',
                  flexShrink: 0,
                }}
              >
                <X style={{ width: 16, height: 16 }} />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
