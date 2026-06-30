const TYPE_STYLES: Record<string, { bg: string; color: string; border: string; label: string }> = {
  word: { bg: '#1e3a5f', color: '#60a5fa', border: 'rgba(96,165,250,0.3)', label: 'Word' },
  phrase: { bg: '#2e1065', color: '#c084fc', border: 'rgba(192,132,252,0.3)', label: 'Phrase' },
  expression: {
    bg: '#431407',
    color: '#fb923c',
    border: 'rgba(251,146,60,0.3)',
    label: 'Expression',
  },
  idiom: { bg: '#14532d', color: '#4ade80', border: 'rgba(74,222,128,0.3)', label: 'Idiom' },
}

interface WordTypeBadgeProps {
  type: string
  className?: string
}

export function WordTypeBadge({ type }: WordTypeBadgeProps) {
  const s = TYPE_STYLES[type.toLowerCase()] ?? {
    bg: 'var(--bg-3)',
    color: 'var(--fg-2)',
    border: 'var(--line-2)',
    label: type,
  }
  return (
    <span
      style={{
        display: 'inline-block',
        fontFamily: 'var(--font-mono)',
        fontSize: 11,
        padding: '3px 9px',
        borderRadius: 'var(--r-pill)',
        background: s.bg,
        color: s.color,
        border: `1px solid ${s.border}`,
        whiteSpace: 'nowrap',
      }}
    >
      {s.label}
    </span>
  )
}
