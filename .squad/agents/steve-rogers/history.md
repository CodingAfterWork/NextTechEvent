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

---

## Team Decision: CSS Isolation Pattern (2026-03-06)

**Status:** Adopted team-wide convention

### Pattern

**Position context setup (`position: relative`) must live in component CSS files, not globally.**

When a component uses `position: absolute` children inside a Bootstrap structural class (`.card`, `.list-group-item`, etc.), the component's `.razor.css` file must explicitly set `position: relative` on that Bootstrap class.

**Never:**
- Patch Bootstrap globally
- Rely on Bootstrap to provide the stacking context
- Change the child's positioning strategy

**Always:**
- Add the override in the component's CSS isolation file
- Respect the minimal blast radius principle
- Leverage Blazor's scoped `b-xxxx` attribute isolation

### Why This Matters

This pattern emerged from fixing the `.state` icon escape bug in #31. It establishes a repeatable, safe way to customize Bootstrap's default behavior across the application without risk of unintended side effects.
