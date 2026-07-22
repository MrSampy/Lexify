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

// Word-boundary matching (mirrors backend ConversationScoring): a plain substring check let "cat"
// pass inside "category". A term token still matches an inflected token that extends it by up to
// MAX_INFLECTION_SUFFIX chars ("embark" → "embarked"), except for very short terms.
const MIN_PREFIX_MATCH_LENGTH = 4
const MAX_INFLECTION_SUFFIX = 3

/** Lowercase and replace punctuation with spaces so it never blocks a match (matches the server). */
export function normalize(text: string): string {
  return text.toLowerCase().replace(/[^\p{L}\p{N}\s]/gu, ' ')
}

function tokenize(normalizedText: string): string[] {
  return normalizedText.split(/\s+/).filter(Boolean)
}

function tokenMatches(textToken: string, termToken: string): boolean {
  if (textToken === termToken) return true
  const extra = textToken.length - termToken.length
  return (
    termToken.length >= MIN_PREFIX_MATCH_LENGTH &&
    extra > 0 &&
    extra <= MAX_INFLECTION_SUFFIX &&
    textToken.startsWith(termToken)
  )
}

function containsTerm(textTokens: string[], termTokens: string[]): boolean {
  if (termTokens.length === 0 || textTokens.length < termTokens.length) return false
  for (let start = 0; start <= textTokens.length - termTokens.length; start++) {
    let all = true
    for (let i = 0; i < termTokens.length; i++) {
      if (!tokenMatches(textTokens[start + i], termTokens[i])) {
        all = false
        break
      }
    }
    if (all) return true
  }
  return false
}

export function budgetFor(targetWordCount: number): number {
  return Math.max(MIN_BUDGET, targetWordCount + BUDGET_SLACK)
}

export function usedWordIds(targets: ScoreTarget[], learnerMessages: string[]): Set<string> {
  const textTokens = tokenize(normalize(learnerMessages.join(' ')))
  const used = new Set<string>()
  for (const t of targets) {
    if (containsTerm(textTokens, tokenize(normalize(t.term)))) used.add(t.wordId)
  }
  return used
}

function comboBonus(targets: ScoreTarget[], learnerMessages: string[]): number {
  const normTerms = targets.map((t) => tokenize(normalize(t.term)))
  const seen = new Set<number>()
  let bonus = 0
  for (const msg of learnerMessages) {
    const textTokens = tokenize(normalize(msg))
    let newInMessage = 0
    normTerms.forEach((nt, i) => {
      if (!seen.has(i) && containsTerm(textTokens, nt)) {
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
