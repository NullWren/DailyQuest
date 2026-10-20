# Copy-paste prompt for an AI chat

Use this when you want an AI chat to turn a plain-English list into a WrenFeed import file. Copy the prompt below into a chat, then paste your tasks under **My tasks**. Attach or paste [template.json](template.json) and [README.md](README.md) too, so the chat has the current format rules. Save its final JSON answer as a UTF-8 `.json` file and use **Settings → Import quests** in WrenFeed. Review the import preview before adding anything.

```text
Help me create one WrenFeed quest-import JSON file from my task list. Use the attached WrenFeed template.json and import README.md as the format specification.

Before writing JSON, ask me concise questions about any important missing or ambiguous detail. In particular, do not guess a game's reset time, timezone, weekday, or whether a reset is fixed versus measured after completion. If you can verify a game schedule from a current, reliable source, tell me the source and what you found; otherwise ask me what to use. Do not invent quests or extra tasks.

Each independently resettable task should be its own quest. Use the same `game` value to group quests from one game visually, even when their resets differ. A quest can have `lines` for small subtasks or notes that share its reset rule. Set `checkbox` to true only when I say the line should be checked separately. Put each quest's short explanatory text in `description`.

Use only these WrenFeed reset types: daily `local-midnight`, daily `fixed-utc` with `timeUtc`, daily `hours-after-completion` with integer `hours`, weekly `fixed-utc` with `weekdayUtc` and `timeUtc`, or weekly `seven-days-after-completion`. Use the exact allowed field names and values from the attached files. Keep names and descriptions short. Do not include IDs, history, completions, points, comments, markdown, or unsupported JSON fields.

After I answer your questions, return one valid JSON object with `format`, `version`, and `quests`, containing both daily and weekly entries if needed. Check that the JSON parses, the field lengths and number ranges match the README, and there are no duplicate combinations of frequency, game, and quest name. Output only the final JSON in a single code block, with no trailing commas.

My tasks:
[Paste my games and tasks here. Include any known reset rules, short descriptions, and which sub-lines should have checkboxes.]
```

If the AI chat asks about something you do not know yet, you can ask it to leave that task out for now. The import accepts daily and weekly quests in the same file, so you can add the missing task later.
