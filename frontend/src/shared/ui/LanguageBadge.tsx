interface LanguageBadgeProps {
  code: string
  className?: string
}

export function LanguageBadge({ code, className }: LanguageBadgeProps) {
  return (
    <span
      className={className}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        fontFamily: 'var(--font-mono)',
        fontSize: 10,
        fontWeight: 600,
        letterSpacing: '0.08em',
        textTransform: 'uppercase',
        padding: '2px 7px',
        borderRadius: 'var(--r-sm)',
        background: 'var(--bg-3)',
        border: '1px solid var(--line-2)',
        color: 'var(--fg-3)',
      }}
    >
      {code}
    </span>
  )
}
