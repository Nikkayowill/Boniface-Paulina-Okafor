# Data Cleaning Workflow

Use this workflow when a CSV or spreadsheet export has mixed formats, duplicate rows, summary rows, aliases, missing values, or other cleanup issues.

The original file must stay unchanged. Always write cleaned output to a new file and include a short data-quality note that reviewers can inspect before using the cleaned data downstream.

## Best For

- CSV or spreadsheet exports with mixed dates, currencies, duplicates, summary rows, or missing values.
- Data gathered from multiple teams, systems, clinics, or repeated exports.
- Upload prep where downstream systems need consistent columns and values.

## Inputs

Ask the requester to provide:

- The source file path or attachment.
- The problems they already see.
- The output they need, such as a cleaned CSV, cleaned spreadsheet tab, or upload-ready file.
- Any canonical values, such as valid region names, category names, department names, or date format.

## Cleaning Rules

- Do not edit the original file.
- Preserve source row IDs when possible.
- Use one date format across the cleaned copy.
- Keep blank currency cells blank instead of converting them to zero.
- Remove pasted summary or subtotal rows from the cleaned data.
- Normalize aliases only when the intended canonical value is clear.
- Remove duplicate rows only when the duplicate rule is explainable.
- Leave uncertain values unchanged or mark them for review in the data-quality note.

## Common Fixes

### Mixed Dates

Normalize dates to `yyyy-MM-dd` unless the requester specifies another format.

If a date is ambiguous, such as `03/04/2026`, only convert it when the source system or surrounding rows make the intended format clear. Otherwise, keep the original value and flag it.

### Currency Values

Remove display-only formatting from populated currency cells:

- Dollar signs
- Commas
- Leading or trailing spaces

Keep blank cells blank. Do not replace blanks with `0`.

### Duplicate Rows

Prefer source row IDs for duplicate detection. If row IDs are missing, use a stable set of fields such as customer or patient identifier, date, category, and amount.

When duplicates are removed, list the removed row numbers or IDs in the data-quality note.

### Aliases

Normalize aliases through an explicit mapping table.

Example:

| Raw value | Clean value |
| --- | --- |
| `north`, `North Region`, `N.` | `North` |
| `cardio`, `Cardiology Dept` | `Cardiology` |

If the mapping is not obvious, do not guess.

### Summary Rows

Remove rows that are clearly not records, such as:

- `Total`
- `Subtotal`
- `Grand total`
- Blank separator rows
- Repeated header rows pasted into the middle of the file

## Output Package

For each cleanup task, produce:

- A cleaned data file, usually named `<original-name>.cleaned.csv`.
- A data-quality note, usually named `<original-name>.data-quality.md`.

The data-quality note should include:

- Source file name.
- Cleaned file name.
- Date format used.
- Rows changed.
- Rows removed.
- Values normalized through aliases.
- Rows or fields that could not be cleaned confidently.

## Starter Prompt

```text
Clean @source-file.csv.

What's wrong:
- dates are mixed between MM/DD/YYYY and YYYY-MM-DD
- currency values include $, commas, and blank cells
- duplicate rows came from repeated exports
- region and category names use several aliases
- pasted summary rows are mixed into the data

What I want:
- write a cleaned CSV
- keep the original file unchanged
- use yyyy-MM-dd dates
- keep blank currency cells blank
- preserve source row IDs when possible
- add a short data-quality note with rows changed, removed, or not cleaned confidently
```

## Review Checklist

Before using the cleaned output:

- Confirm the original file is unchanged.
- Compare row counts before and after cleanup.
- Review removed rows.
- Review alias mappings.
- Spot-check dates and currency fields.
- Check the data-quality note for unresolved items.
