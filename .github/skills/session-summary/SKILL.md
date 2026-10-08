---
name: session-summary
description: 'Create or update a concise dated record of a repository work session. Use at the end of a coding or planning session, or when asked to summarize decisions, changes, checks, and follow-ups in docs/ai-chats.'
argument-hint: 'Optional summary topic'
user-invocable: true
disable-model-invocation: false
---

# Session Summary

Create one concise Markdown summary per calendar day of the current substantive repository session in `docs/ai-chats/`, using the user's current local date.

## Procedure

1. Review the conversation and workspace state. Inspect the relevant `git status` and diff so the summary distinguishes completed changes from pre-existing or unrelated work.
2. Capture the session goal, decisions and constraints, files changed, checks actually run and their results, and deferred work or blockers.
3. Be factual: label proposals, unverified behavior, and manual GitHub setup as pending. Never claim a check passed unless it was run and passed.
4. Do not copy the chat transcript, include secrets or tokens, or claim remote changes when work was only local.
5. Name the file `YYYY-MM-DD-<topic>.md` using today's date and a concise kebab-case topic. Update this session's existing summary only if its filename date matches today. On a new day, create a new dated file and link to the previous day's summary for context; do not append today's work to yesterday's file. If today's notes were already appended to yesterday's file, move only those notes to the new file, preserving yesterday's history. Do not overwrite a summary from another session. Create `docs/ai-chats/` if it does not exist.
6. Keep the note brief and useful for someone resuming the work. Mention its path in the final response.

Skip trivial, unrelated conversation with no repository work or project decisions. Respect an explicit request not to create or update a summary.