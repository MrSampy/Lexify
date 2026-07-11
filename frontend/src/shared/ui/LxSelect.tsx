import type { CSSProperties, ReactNode } from 'react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

export interface LxSelectOption {
  value: string
  label: ReactNode
}

interface LxSelectProps {
  value: string
  onValueChange: (value: string) => void
  options: LxSelectOption[]
  placeholder?: string
  disabled?: boolean
  /** Extra trigger styling — commonly a fixed width to match the layout the native select had. */
  triggerStyle?: CSSProperties
}

/**
 * Drop-in replacement for a native `<select className="lx-input">`. The trigger matches the app's
 * input styling so it sits cleanly beside other `lx-input`s, but the dropdown is the themed base-ui
 * popup instead of the browser's native list (which looked foreign to the design). It also vertically
 * centers the trigger text — a fixed-height native select clipped descenders (e.g. the "g" in
 * "languages").
 */
export function LxSelect({
  value,
  onValueChange,
  options,
  placeholder,
  disabled,
  triggerStyle,
}: LxSelectProps) {
  // base-ui's Select.Value renders the raw value unless the root is given an items map to look up
  // the label from — without this the trigger would show e.g. "all" instead of "All roles".
  const items = Object.fromEntries(options.map((o) => [o.value, o.label]))

  return (
    <Select
      items={items}
      value={value}
      onValueChange={(v) => v != null && onValueChange(v as string)}
      disabled={disabled}
    >
      <SelectTrigger
        style={{
          height: 36,
          background: 'var(--bg-4)',
          borderWidth: 1.5,
          borderColor: 'var(--line-2)',
          borderRadius: 'var(--r-md)',
          fontFamily: 'var(--font-body)',
          fontSize: 13,
          color: 'var(--fg-1)',
          cursor: 'pointer',
          ...triggerStyle,
        }}
      >
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {options.map((o) => (
          <SelectItem key={o.value} value={o.value}>
            {o.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
