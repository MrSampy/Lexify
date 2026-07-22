// Client-side mirror of backend ConversationScoring for the live HUD. The authoritative final score
// comes from the server (end-of-conversation summary); this only drives the in-chat counters.

export interface ScoreTarget {
  wordId: string
  term: string
}

const POINTS_PER_WORD = 10
const COMBO_BONUS_PER_EXTRA = 5
const BUDGET_SLACK = 2
const MIN_BUDGET = 3

/** Lowercase and replace punctuation with spaces so it never blocks a match (matches the server). */
export function normalize(text: string): string {
  return text.toLowerCase().replace(/[^\p{L}\p{N}\s]/gu, ' ')
}

export function budgetFor(targetWordCount: number): number {
  return Math.max(MIN_BUDGET, targetWordCount + BUDGET_SLACK)
}

export function usedWordIds(targets: ScoreTarget[], learnerMessages: string[]): Set<string> {
  const text = normalize(learnerMessages.join(' '))
  const used = new Set<string>()
  for (const t of targets) {
    const nt = normalize(t.term).trim()
    if (nt && text.includes(nt)) used.add(t.wordId)
  }
  return used
}

function comboBonus(targets: ScoreTarget[], learnerMessages: string[]): number {
  const norm = targets.map((t) => normalize(t.term).trim())
  const seen = new Set<number>()
  let bonus = 0
  for (const msg of learnerMessages) {
    const text = normalize(msg)
    let newInMessage = 0
    norm.forEach((nt, i) => {
      if (!seen.has(i) && nt && text.includes(nt)) {
        seen.add(i)
        newInMessage++
      }
    })
    if (newInMessage >= 2) bonus += (newInMessage - 1) * COMBO_BONUS_PER_EXTRA
  }
  return bonus
}

export function livePoints(
  usedCount: number,
  targets: ScoreTarget[],
  learnerMessages: string[],
): number {
  return usedCount * POINTS_PER_WORD + comboBonus(targets, learnerMessages)
}
