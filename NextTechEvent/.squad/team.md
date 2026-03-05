# Squad Team

> NextTechEvent — Call for Papers platform for speakers, attendees, and conference organizers.

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. Does not generate domain artifacts. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Tony Stark | Backend Lead | `.squad/agents/tony-stark/charter.md` | ✅ Active |
| Steve Rogers | Frontend Lead | `.squad/agents/steve-rogers/charter.md` | ✅ Active |
| Pepper Potts | UX Lead | `.squad/agents/pepper-potts/charter.md` | ✅ Active |
| Natasha Romanoff | Security & Auth | `.squad/agents/natasha-romanoff/charter.md` | ✅ Active |
| Bruce Banner | Testing & QA | `.squad/agents/bruce-banner/charter.md` | ✅ Active |
| Nick Fury | Organizer Tools Lead | `.squad/agents/nick-fury/charter.md` | ✅ Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 📋 Silent |

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and code style cleanup
- Dependency updates and version bumps
- Small isolated features with clear specs
- Boilerplate/scaffolding generation
- Documentation fixes and README updates

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns
- Migration scripts with well-defined schemas

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions and system design
- Multi-system integration requiring coordination
- Ambiguous requirements needing clarification
- Security-critical changes (auth, encryption, access control)
- Performance-critical paths requiring benchmarking
- Changes requiring cross-team discussion

## Project Context

- **Stack:** .NET 9, Blazor, Entity Framework Core, .NET Aspire
- **Description:** Call for Papers platform — speakers submit sessions, attendees browse schedules, organizers manage CFP lifecycle and scheduling.
- **Universe:** Marvel Cinematic Universe
- **Project:** NextTechEvent
- **Created:** 2026-03-05
