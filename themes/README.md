# Optional WrenFeed themes

WrenFeed keeps its original dark quest log as **Default**. The themes here are optional, fully offline files. They change colors, fonts, borders, and small built-in decorations; they do not change quests or history. [Neon Aviary](neon-aviary.json) has a navy-and-teal circuit look. [Adventurer's Ledger](adventurers-ledger.json) has parchment cards, brass flourishes, and a small illustrated wren in the header.

| Neon Aviary | Adventurer's Ledger |
| --- | --- |
| ![Neon Aviary preview](previews/neon-aviary.png) | ![Adventurer's Ledger preview](previews/adventurers-ledger.png) |

To use one, open **Settings → Install theme file…**, choose its `.json` file, then select it in the **Appearance** list. If you downloaded the whole WrenFeed ZIP, the files are already in its `themes` folder. If you are browsing the GitHub repo, open a theme file and save its raw contents as a `.json` file first. Theme selection is immediate and remembered when WrenFeed reopens. Choose **Default** at any time to return to the original design.

Installed copies live in `%LOCALAPPDATA%\DailyQuest\themes\`. The original files in this repo or shareable ZIP can be deleted after installation. WrenFeed does not download themes or run code from them. Backups save the selected theme ID but do not include theme files; install the same theme file on another computer if you restore a backup there. If the selected file is missing, WrenFeed displays Default until you reinstall it or choose another theme.

To make your own, copy [template.json](template.json). You can give an AI chatbot this README and the template, then ask it to create a WrenFeed theme. Check the colors and fonts before installing. JSON requires double quotes, commas between items, and no comments or trailing commas.

| Field | Meaning |
| --- | --- |
| `format`, `version` | Exactly `"wrenfeed-theme"` and `1`. |
| `id` | Unique lowercase letters, numbers, and hyphens, starting with a letter; maximum 40 characters. `quest` is reserved for Default. |
| `name`, `description` | Display name (1–60 characters) and explanation (1–160 characters). |
| `colors` | All eight named colors from the template, each in `#RRGGBB` format. `Ink` and `Muted` must have at least 4.5:1 contrast against `Canvas`, `Panel`, and `Field`. `Accent` and `Success` must have at least 4.5:1 contrast against `Canvas`. |
| `fonts` | `body` and `display` are installed Windows font family names. WrenFeed does not download or install fonts. |
| `decoration` | `none` (original plain cards), `circuit` (angular trim), or `filigree` (ornate trim). |
| `mark` | A 1–4 character symbol beside the WrenFeed name. |
| `mascot` | `none` or `wren`. The latter shows a small built-in vector wren in the header. |

The theme file is limited to 64 KB. Unknown fields and invalid colors are rejected. Installing a file with an existing theme ID can replace its installed copy after confirmation. Theme files contain only these declarative settings; there are no scripts, remote assets, or arbitrary XAML.
