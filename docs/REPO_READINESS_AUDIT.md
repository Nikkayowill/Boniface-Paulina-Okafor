# Repo Readiness Audit

Last updated: 2026-07-13

This audit tracks cleanup and onboarding issues for the next developer.

## Cleaned This Pass

| Finding | Action |
|---|---|
| `.nuget/packages` was tracked in git even though it is a machine-local package cache. | Removed from git tracking with `git rm -r --cached .nuget`; `.gitignore` already excludes it. |
| Docker SQL healthcheck used the old sqlcmd path. | Updated `docker-compose.yml` to `/opt/mssql-tools18/bin/sqlcmd -C`; container now reports healthy. |
| `ImageService` used lowercase `hospital` folder on Linux while repo folder is `Hospital`. | Updated service and tests to use `Hospital`. |
| `ImageService` fallback pointed to missing `/images/placeholders/default.jpg`. | Updated fallback to existing `/images/placeholders/placeholder.svg`. |
| Seeded doctors referenced two image files that were never committed. | New seed records now use initials, and startup seeding clears only the two known stale image paths from existing seeded records. |
| Frontend contract referenced Flutterwave though current backend only implements Paystack. | Removed Flutterwave references from the active payment wording. |

## External Accounts Needed

See `docs/API_SIGNUP_CHECKLIST.md`.

Highest priority owner accounts:

- Meta WhatsApp Cloud API
- Paystack
- SMTP email provider
- Public HTTPS domain/hosting
- Africa's Talking SMS, if SMS is still desired
- VAPID keys for browser push

## Visual/UI Risks To Check Manually

These need browser screenshots or manual device checks before marking complete:

| Area | Risk | Suggested Check |
|---|---|---|
| Floating WhatsApp button + PWA install button | They are positioned close together on mobile. CSS tries to offset them, but real mobile browser chrome can change available height. | Check 360px, 390px, 430px mobile widths with install prompt available. |
| Public layout depends on CDN Alpine and SignalR script | Slow or blocked networks may affect mobile menu and realtime booking interactions. | Consider vendoring these scripts or documenting CDN dependency. |
| Google Fonts dependency | Rural/slow networks may delay font loading. | Check fallback font rendering and layout shift. |
| Large hospital media folder | Repo includes about 150 MB of hospital placeholder images/video. Useful for real visuals, but heavy for clone size. | Decide whether to keep all media, compress further, move large videos to external storage, or use Git LFS. |
| Admin/patient Bootstrap assets | Bootstrap is vendored under `wwwroot/lib/bootstrap`; keep because admin/patient layouts depend on it. | Do not remove unless replacing admin/patient styling. |
| TinyMCE vendor assets | Admin CMS post editor depends on local TinyMCE files. | Keep unless replacing the CMS editor or installing TinyMCE through npm. |

## Redundant Or Suspicious Files

| Path | Recommendation |
|---|---|
| `.nuget/` | Do not commit. Already removed from tracking; local folder can remain ignored. |
| `bin/`, `obj/`, `node_modules/` | Keep ignored; do not commit. |
| `wwwroot/images/placeholders/Hospital/*.MP4` | Review before handoff. These are large and may not be needed in the repo. |
| `wwwroot/images/placeholders/Hospital/convert.js` | Development helper only. Keep if the media conversion workflow is documented; otherwise remove in a later cleanup. |
| `FRONTEND_BACKEND_INTEGRATION_CONTRACT.md` | Useful, but still describes a possible future separate `Frontend/` workspace. Keep only if React/static frontend split is still planned. |

## Next Cleanup Targets

1. Decide whether large `.MP4` files belong in git, Git LFS, or external storage.
2. Add a teammate read-through checklist.
3. Decide whether to vendor Alpine/SignalR locally for offline/low-bandwidth reliability.
