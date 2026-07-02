interface LanguageBadgeProps {
  code: string
  className?: string
}

export function LanguageBadge({ code, className }: LanguageBadgeProps) {
  return (
    <span
      className={`inline-flex items-center rounded-[var(--r-sm)] border border-[var(--line-2)] bg-[var(--bg-3)] px-[7px] py-0.5 text-[10px] font-semibold tracking-[0.08em] uppercase text-[var(--fg-3)] [font-family:var(--font-mono)] ${className ?? ''}`}
    >
      {code}
    </span>
  )
}
