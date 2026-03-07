# Team Decisions

## CSS Isolation Pattern for Bootstrap Overrides

**Author:** Steve Rogers (Frontend Lead)  
**Date:** 2026-03-06  
**Related:** Issue #31, PR #38

### Context

Conference card components (`ConferenceItem`) use a `.state` icon with `position: absolute; right: 20px; top: 10px` to pin a status icon to the top-right corner of the card. Bootstrap's `.card` class does not set `position: relative`, which caused the icon to escape its card container.

### Decision

**Add `position: relative` to the `.card` rule inside component-level CSS isolation files** rather than:
- Patching Bootstrap globally (would affect all cards site-wide)
- Changing the icon's positioning strategy (absolute placement is correct UX for overlay icons)
- Adding an extra wrapper `<div>` around the card (unnecessary DOM complexity)

### Rationale

Blazor CSS isolation scopes `.razor.css` rules with a unique `b-xxxx` attribute, so overriding Bootstrap class properties in component CSS files is safe and surgical — it only affects that component's rendered output. This respects the principle of minimal blast radius for fixes.

### Pattern Going Forward

Whenever a component uses `position: absolute` children inside a Bootstrap structural class (`.card`, `.list-group-item`, etc.), the component's `.razor.css` file should explicitly set `position: relative` on that Bootstrap class. Do not rely on Bootstrap to provide a stacking context.

---

## SpeakerPage Pagination Pattern

**Author:** Steve Rogers (Frontend Lead)  
**Date:** 2026-03-06  
**Related issue:** #35

### Context

`SpeakerPage.razor` uses a paginated API (`SearchActiveConferencesAsync`) that returns a plain `List<Conference>` with no total count or page metadata.

### Decision

**Pagination buttons must live outside the conference cards flex container**, in a semantic `<nav>` element, to avoid being styled/stretched as cards.

**Last-page detection heuristic:** `Conferences.Count < pagesize` signals the final page (the API returned fewer items than requested). This is the best available signal given the current API contract.

**Disabled state pattern:** Use Blazor's `disabled="@(boolExpr)"` binding — Blazor correctly omits the attribute when `false` and renders it when `true`.

### Consequences

- If the API is ever updated to return total count or a `hasNextPage` flag, update `IsLastPage` to use that instead.
- This pattern should be applied consistently to any other paginated list pages added in the future.

---

## Main Branch Protection Rules

**Status:** Implemented  
**Date:** 2025-01-XX  
**Author:** Natasha Romanoff (Security & Auth Lead)  
**Requested by:** Jimmy Engström

### Context

The `main` branch is the production deployment branch for NextTechEvent. To maintain code quality, prevent accidental pushes, and ensure security review, branch protection rules are required.

### Decision

Applied comprehensive branch protection rules to the `main` branch of `CodingAfterWork/NextTechEvent`:

#### Required Status Checks
- **`build`** - ASP.NET Core build and Azure deployment workflow must pass
- **`guard`** - Squad branch guard must pass (prevents internal team files from reaching main)
- **Strict mode enabled** - branches must be up-to-date with main before merging

#### Pull Request Requirements
- **Minimum 1 approving review** required before merge
- **Stale reviews dismissed** when new commits are pushed
- Code owner reviews not required (can be added later if needed)

#### Administrative Controls
- **Admin enforcement enabled** - even admins must follow PR process (no bypass)
- **Force pushes blocked** - history cannot be rewritten
- **Branch deletion blocked** - main cannot be deleted
- **Direct pushes blocked** - all changes must go through PR workflow

### Rationale

1. **Defense in depth**: Multiple layers (review + CI) catch issues before production
2. **Audit trail**: PR process ensures all changes are documented and reviewed
3. **Quality gates**: Build and guard checks prevent broken or inappropriate code from merging
4. **No exceptions**: Admin enforcement prevents accidental bypass of security controls

### Impact

- All contributors must create feature branches and open PRs to change main
- At least one team member review is required for every change
- CI must pass before merge (build succeeds, no forbidden paths detected)
- Production deployments are now gated by code review and automated checks

### Verification

Branch protection confirmed via GitHub REST API:
- Endpoint: `GET /repos/CodingAfterWork/NextTechEvent/branches/main/protection`
- All rules active and enforced
- Status checks: `build`, `guard`
- Required reviews: 1
- Admin enforcement: true
- Force pushes: false
- Deletions: false
