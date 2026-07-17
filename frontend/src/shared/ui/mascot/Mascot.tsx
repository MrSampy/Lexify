import { useEffect, useState, type CSSProperties } from 'react'
import greeting from './poses/greeting.png'
import celebrate from './poses/celebrate.png'
import sleep from './poses/sleep.png'
import confused from './poses/confused.png'
import diving from './poses/diving.png'
import lost from './poses/lost.png'
import builder from './poses/builder.png'
import scientist from './poses/scientist.png'
import pointing from './poses/pointing.png'
import celebrateVid from './poses/celebrate.webm'
import sleepVid from './poses/sleep.webm'
import divingVid from './poses/diving.webm'
import './mascot.css'

// Lexi the axolotl — see Info/lexify-mascot.md for the pose/screen map and the
// usage rules (lives in pauses, never overlaps content, never shames the user).
const POSES = {
  greeting,
  celebrate,
  sleep,
  confused,
  diving,
  lost,
  builder,
  scientist,
  pointing,
} as const

export type MascotPose = keyof typeof POSES

// Poses that ship an animated WebM. `loop: false` plays once (celebration);
// the still PNG is always the poster/fallback and the reduced-motion variant.
const ANIMATED: Partial<Record<MascotPose, { src: string; loop: boolean }>> = {
  celebrate: { src: celebrateVid, loop: false },
  sleep: { src: sleepVid, loop: true },
  diving: { src: divingVid, loop: true },
}

function usePrefersReducedMotion() {
  const [reduced, setReduced] = useState(
    () => window.matchMedia('(prefers-reduced-motion: reduce)').matches,
  )
  useEffect(() => {
    const mq = window.matchMedia('(prefers-reduced-motion: reduce)')
    const onChange = (e: MediaQueryListEvent) => setReduced(e.matches)
    mq.addEventListener('change', onChange)
    return () => mq.removeEventListener('change', onChange)
  }, [])
  return reduced
}

interface MascotProps {
  pose: MascotPose
  /** Rendered square size in px (source assets are 512×512). */
  size?: number
  /**
   * Play the pose's WebM animation when one exists. Ignored (falls back to the
   * still image) under prefers-reduced-motion or when the pose has no video.
   */
  animate?: boolean
  /** Gentle idle bobbing for still images; disabled by prefers-reduced-motion. */
  float?: boolean
  /**
   * Accessible alt text. Omit for decorative use (the default) — the image is
   * then hidden from screen readers.
   */
  label?: string
  style?: CSSProperties
}

export function Mascot({
  pose,
  size = 128,
  animate = false,
  float = false,
  label,
  style,
}: MascotProps) {
  const reduced = usePrefersReducedMotion()
  const video = ANIMATED[pose]

  if (animate && video && !reduced) {
    return (
      <video
        src={video.src}
        poster={POSES[pose]}
        width={size}
        height={size}
        autoPlay
        muted
        playsInline
        loop={video.loop}
        aria-hidden={label ? undefined : true}
        aria-label={label}
        style={{ userSelect: 'none', ...style }}
      />
    )
  }

  return (
    <img
      src={POSES[pose]}
      width={size}
      height={size}
      alt={label ?? ''}
      aria-hidden={label ? undefined : true}
      draggable={false}
      className={float ? 'mascot-float' : undefined}
      style={{ userSelect: 'none', ...style }}
    />
  )
}
