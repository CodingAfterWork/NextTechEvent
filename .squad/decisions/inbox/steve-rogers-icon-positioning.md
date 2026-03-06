# Decision: Scoped Bootstrap card position override for absolutely-positioned icons

**Author:** Steve Rogers  
**Date:** 2026-03-06  
**Related:** Issue #31, PR #38

## Context

Conference card components (`ConferenceItem`) use a `.state` icon with `position: absolute; right: 20px; top: 10px` to pin a status icon to the top-right corner of the card. Bootstrap's `.card` class does not set `position: relative`, which caused the icon to escape its card container.

## Decision

**Add `position: relative` to the `.card` rule inside component-level CSS isolation files** rather than:
- Patching Bootstrap globally (would affect all cards site-wide)
- Changing the icon's positioning strategy (absolute placement is correct UX for overlay icons)
- Adding an extra wrapper `<div>` around the card (unnecessary DOM complexity)

## Rationale

Blazor CSS isolation scopes `.razor.css` rules with a unique `b-xxxx` attribute, so overriding Bootstrap class properties in component CSS files is safe and surgical — it only affects that component's rendered output. This respects the principle of minimal blast radius for fixes.

## Pattern Going Forward

Whenever a component uses `position: absolute` children inside a Bootstrap structural class (`.card`, `.list-group-item`, etc.), the component's `.razor.css` file should explicitly set `position: relative` on that Bootstrap class. Do not rely on Bootstrap to provide a stacking context.
