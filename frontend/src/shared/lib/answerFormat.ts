/**
 * Turns a submitted/expected answer into human-readable lines. matching_pairs travels as the wire
 * format "term|translation;term|translation" — split it into one "term → translation" line per
 * pair. Everything else is a single line as-is.
 */
export function answerToLines(questionType: string, answer: string): string[] {
  if (questionType === 'matching_pairs') {
    return answer
      .split(';')
      .filter(Boolean)
      .map((pair) => pair.replace('|', ' → ').trim())
  }
  return answer ? [answer] : ['—']
}

/** True for types whose answer is long/multi-part and reads better stacked than inline. */
export function isMultilineAnswer(questionType: string): boolean {
  return questionType === 'matching_pairs' || questionType === 'sentence_builder'
}
