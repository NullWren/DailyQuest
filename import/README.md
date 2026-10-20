# Importing quests into WrenFeed

Copy [template.json](template.json), replace the examples with your own quests, and save it as a `.json` file. In WrenFeed, open **Settings → Import quests**, choose the file, review the preview, and select **Add quests**. One file can contain daily and weekly quests together. The template demonstrates every reset option available in the editor and both checkbox and note lines.

You can use the ready-to-copy [AI chat prompt](AI-PROMPT.md) with this README and the template to create your quest file from plain-English tasks. It asks about important missing reset details instead of guessing. Check the game-specific reset rules it suggests before importing. Use ordinary UTF-8 JSON: double quotes around field names and text, commas between items, and no comments or trailing commas.

Each item in `quests` represents one independently resettable quest. `game` is the optional game-group heading; give related quests the same `game` value to show them in one panel. `name` is the quest title and `description` is the optional short text below it. A `lines` item with `"checkbox": true` is a subtask; `false` is an informational note. All checkable lines share the quest's reset rule. If two tasks need different resets or separate descriptions, make them separate quests in the same game group.

| Field | Meaning |
| --- | --- |
| `format` | Required: `"wrenfeed-quests"`. |
| `version` | Required: `1`. |
| `quests` | Required array of 1–500 quest objects. |
| `frequency` | Required per quest: `"daily"` or `"weekly"`. |
| `game` | Optional group heading, up to 80 characters. Omit or use `""` for ungrouped. |
| `name` | Required quest name, 1–80 characters. |
| `description` | Optional, up to 220 characters. |
| `reset` | Required for weekly quests; optional for daily quests (defaults to local midnight). |
| `lines` | Optional array of up to 100 objects, each with `text` (1–160 characters) and `checkbox` (`true` or `false`). |

Reset objects use these exact forms:

| Frequency | Reset | JSON |
| --- | --- | --- |
| Daily | Local midnight | `{ "type": "local-midnight" }` |
| Daily | Fixed UTC time | `{ "type": "fixed-utc", "timeUtc": "00:00" }` |
| Daily | Hours after completion | `{ "type": "hours-after-completion", "hours": 23 }` |
| Weekly | Fixed UTC weekday and time | `{ "type": "fixed-utc", "weekdayUtc": "Monday", "timeUtc": "00:00" }` |
| Weekly | 00:00 UTC on the seventh day after completion | `{ "type": "seven-days-after-completion" }` |

`hours` must be a whole number from 1 to 8760. `timeUtc` uses 24-hour `HH:MM`; weekday names are Sunday through Saturday. Imported quests start incomplete and get new IDs. The import does not change existing quests, settings, or completion history. Entries with the same tab, game group, and quest name as an existing quest (including an archived quest) or an earlier entry in the same file are shown as duplicates and skipped; capitalization does not matter. The preview shows every addition and skip. Invalid or unknown fields stop the entire import before anything is saved.

**Import quests adds definitions.** It is different from **Restore backup**, which replaces the whole journal and includes history. Export a backup from Settings if you need a full copy of your existing data.
