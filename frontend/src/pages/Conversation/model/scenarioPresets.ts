/**
 * Ready-made roleplay scenarios for the setup page. The `text` is what gets sent as the scenario —
 * it stays English because the backend splices it into an English system prompt ("Play out this
 * scenario together: …"); Lexi still replies in the target language. Only the chip labels are localized.
 * All texts must stay under the 200-char scenario cap.
 */
export interface ScenarioPreset {
  id: string
  emoji: string
  labelKey: string
  text: string
}

export const SCENARIO_PRESETS: ScenarioPreset[] = [
  {
    id: 'cafe',
    emoji: '☕',
    labelKey: 'chat.presets.cafe',
    text: 'Ordering food and drinks at a cozy café; you are the barista.',
  },
  {
    id: 'interview',
    emoji: '💼',
    labelKey: 'chat.presets.interview',
    text: 'A friendly job interview for a position I would love to get.',
  },
  {
    id: 'travel',
    emoji: '✈️',
    labelKey: 'chat.presets.travel',
    text: 'Checking into a hotel and asking for tips about the city.',
  },
  {
    id: 'shopping',
    emoji: '🛍️',
    labelKey: 'chat.presets.shopping',
    text: 'Shopping for clothes; you are the shop assistant helping me choose.',
  },
  {
    id: 'doctor',
    emoji: '🩺',
    labelKey: 'chat.presets.doctor',
    text: 'A visit to the doctor to describe how I feel and get advice.',
  },
  {
    id: 'friends',
    emoji: '🎉',
    labelKey: 'chat.presets.friends',
    text: 'Catching up with an old friend about our weekend plans.',
  },
  {
    id: 'movies',
    emoji: '🎬',
    labelKey: 'chat.presets.movies',
    text: 'Discussing a movie we both recently watched and what we liked.',
  },
  {
    id: 'kitchen',
    emoji: '🍳',
    labelKey: 'chat.presets.kitchen',
    text: 'Cooking dinner together; we discuss the recipe and ingredients.',
  },
]
