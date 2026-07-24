export function SectionCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section
      style={{
        background: 'var(--bg-1)',
        border: '1.5px solid var(--line-2)',
        borderRadius: 'var(--r-md)',
        padding: '20px 24px',
      }}
    >
      <h2
        style={{
          margin: '0 0 14px',
          fontFamily: 'var(--font-body)',
          fontWeight: 700,
          fontSize: 13,
          textTransform: 'uppercase',
          letterSpacing: '0.06em',
          color: 'var(--fg-2)',
        }}
      >
        {title}
      </h2>
      {children}
    </section>
  )
}
