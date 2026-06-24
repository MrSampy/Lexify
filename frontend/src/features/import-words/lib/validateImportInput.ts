export function validateImportInput(
  rawText: string,
  targetLanguageId: number,
  nativeLanguageId: number,
): string | null {
  if (!rawText.trim()) return 'Please paste some text to format.'
  if (rawText.length > 10_000) return 'Text is too long — maximum 10 000 characters.'
  if (targetLanguageId === nativeLanguageId) return 'Target and native languages must be different.'
  return null
}
