---
name: kentico-voice-prompt
description: Cleans up voice-dictated or speech-to-text output before it becomes a Claude Code prompt, fixing garbled Kentico/Xperience by Kentico terminology that generic dictation tools (Windows Voice Typing, Apple Dictation, Wispr Flow, Superwhisper, etc) mishear. Trigger whenever the user pastes raw dictated text and asks to clean it up, fix it, or turn it into a prompt; also trigger on phrases like "I dictated this", "voice to text", "fix my dictation", or "clean up this transcript" when Kentico/XbK terms are plausibly present.
---

## Canonical source

This skill's full instructions live in the shared, tool-agnostic copy at
`.agents/skills/kentico-voice-prompt/SKILL.md` (the same file GitHub Copilot uses). This `.claude` copy is a
pointer to keep both agents in sync.

**Read `.agents/skills/kentico-voice-prompt/SKILL.md` now and follow it exactly.** Do not act on this stub
alone — the canonical file holds the source of truth: every step, reference
file, abort condition, and workflow detail.
