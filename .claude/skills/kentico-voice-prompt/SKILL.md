---
name: kentico-voice-prompt
description: Cleans up voice-dictated or speech-to-text output before it becomes a Claude Code prompt, fixing garbled Kentico/Xperience by Kentico terminology that generic dictation tools (Windows Voice Typing, Apple Dictation, Wispr Flow, Superwhisper, etc) mishear. Trigger whenever the user pastes raw dictated text and asks to clean it up, fix it, or turn it into a prompt; also trigger on phrases like "I dictated this", "voice to text", "fix my dictation", or "clean up this transcript" when Kentico/XbK terms are plausibly present.
---

# Kentico Voice Prompt Cleanup

Dictation tools transcribe phonetically. They have never heard of "Xperience by
Kentico" or "KentiCopilot," so they guess the nearest English words. This skill
fixes those guesses before the text becomes a prompt or gets sent anywhere.

## Workflow

The goal of this skill is to fix the input text. Do not display your thought
process or steps you will take.

1. Read `glossary.md` in this skill folder. It maps correct Kentico/XbK terms to
   their common mishearings.
2. Compare the input against the glossary. Fix any mishearing you find, even
   partial or unlisted variants that clearly sound like a glossary term.
3. Fix obvious dictation artifacts unrelated to Kentico (duplicated words, stray
   "period"/"comma" spoken literally, run-on sentences from no pause detection)
   but do NOT rewrite tone, restructure sentences, or "improve" phrasing beyond
   what dictation actually got wrong. This is a correction pass, not an editing
   pass.
4. If a term is ambiguous (could be two different glossary entries), pick the
   more likely one given surrounding context.
5. Only output the corrected text, ready to use as a prompt.
6. When you correct any term, add it to [glossary.md](glossary.md) (canonical
   form, the garbled variants encountered, one-line meaning if non-obvious).

## Notes

- Never guess a correction that isn't in the glossary and isn't a clear near-
  miss of a glossary entry. If a word looks like ordinary English and isn't
  close to a glossary term, leave it alone.
- Keep the glossary file updated. It's community-maintained; add new mishears as
  they're discovered rather than hardcoding fixes into this file.
