# Decision: SpeakerPage Pagination Pattern

**Author:** Steve Rogers (Frontend Lead)
**Date:** 2026-03-06
**Related issue:** #35

## Context

`SpeakerPage.razor` uses a paginated API (`SearchActiveConferencesAsync`) that returns a plain `List<Conference>` with no total count or page metadata.

## Decision

**Pagination buttons must live outside the conference cards flex container**, in a semantic `<nav>` element, to avoid being styled/stretched as cards.

**Last-page detection heuristic:** `Conferences.Count < pagesize` signals the final page (the API returned fewer items than requested). This is the best available signal given the current API contract.

**Disabled state pattern:** Use Blazor's `disabled="@(boolExpr)"` binding — Blazor correctly omits the attribute when `false` and renders it when `true`.

## Consequences

- If the API is ever updated to return total count or a `hasNextPage` flag, update `IsLastPage` to use that instead.
- This pattern should be applied consistently to any other paginated list pages added in the future.
