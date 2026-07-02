const TYPE_STYLES: Record<string, { className: string; label: string }> = {
  word: { className: 'badge-word', label: 'Word' },
  phrase: { className: 'badge-phrase', label: 'Phrase' },
  expression: { className: 'badge-expression', label: 'Expression' },
  idiom: { className: 'badge-idiom', label: 'Idiom' },
}

interface WordTypeBadgeProps {
  type: string
  className?: string
}

export function WordTypeBadge({ type }: WordTypeBadgeProps) {
  const s = TYPE_STYLES[type.toLowerCase()]
  if (s) {
    return <span className={`${s.className} whitespace-nowrap`}>{s.label}</span>
  }
  return (
    <span className="inline-block whitespace-nowrap rounded-[var(--r-pill)] border border-[var(--line-2)] bg-[var(--bg-3)] px-[9px] py-[3px] text-[11px] font-bold text-[var(--fg-2)] [font-family:var(--font-body)]">
      {type}
    </span>
  )
}
