# Steve Rogers — Work History

## Sessions

### Issue #31 — Fix .state icon positioning (2026-03-06)

**Branch:** `squad/31-state-icon-positioning`
**PR:** https://github.com/CodingAfterWork/NextTechEvent/pull/38

**Problem:** The `.state` status icon uses `position: absolute; right: 20px; top: 10px` to place itself in the top-right corner of a conference card. However, Bootstrap's `.card` class does not set `position: relative`, so the icon escaped the card and positioned itself relative to the nearest positioned ancestor in the DOM tree — causing visual misalignment.

**Fix:** Added `position: relative` to the `.card` rule in both `ConferenceItem.razor.css` and `ConferencePage.razor.css` (across both `NextTechEvent.Client` and `NextTechEventNet7` projects). Blazor CSS isolation scopes these rules to their respective components, so Bootstrap cards elsewhere in the app are unaffected.

**Files changed:**
- `NextTechEvent/NextTechEvent/NextTechEvent.Client/Pages/ConferenceItem.razor.css`
- `NextTechEvent/NextTechEvent/NextTechEvent.Client/Pages/ConferencePage.razor.css`
- `NextTechEvent/NextTechEventNet7/Pages/ConferenceItem.razor.css`
- `NextTechEvent/NextTechEventNet7/Pages/ConferencePage.razor.css`

## Learnings

- **Bootstrap cards need `position: relative` when using absolutely-positioned children.** Bootstrap does not set this by default. When overlaying icons, badges, or controls on a `.card`, always add `position: relative` to the scoped `.card` rule in the component's CSS isolation file — never globally.
- **Blazor CSS isolation is safe for Bootstrap overrides.** Because Blazor scopes `.razor.css` rules with a `b-xxxx` attribute selector, adding Bootstrap class overrides (like `.card { position: relative }`) in component CSS files is safe and won't bleed into unrelated components.
