# Kentico Voice Prompt Cleanup — Install

For devs already using Claude Code. No extra accounts, servers, or config.

## Install (per-project)
1. Copy this folder into your repo at `.claude/skills/kentico-voice-prompt/`.
2. Done. Claude Code loads `SKILL.md` on demand; it isn't in your context
   window until it's actually relevant.

## Install (all projects, one machine)
Copy the folder to `~/.claude/skills/kentico-voice-prompt/` instead. Applies
across every project on that machine.

## Use it
1. Dictate into whatever you already use: Windows Voice Typing, Apple
   Dictation, a browser's mic button, Wispr Flow, Superwhisper, whatever's
   free and already installed.
2. Paste the raw output into Claude Code and say "clean this up" or "fix my
   dictation" (or just paste it, the skill triggers on its own if the text
   looks like a transcript with Kentico terms in it).
3. Claude returns the corrected prompt plus a short list of what it changed,
   so you can sanity-check it before running.
4. Send the corrected text as your actual prompt.

## Contribute
Found a mishearing not in `glossary.md`? Add a line under the right section
and open a PR / share it back. The glossary is the only part that needs to
grow over time; the workflow in `SKILL.md` doesn't need to change.
