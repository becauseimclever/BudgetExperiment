### 2026-04-25: Client module coverage reporting & gating policy

**By:** User (via Copilot)

**What:** 
- Client module (UI/Blazor) code coverage shall be **reported in all coverage reports**
- Client module coverage shall be **excluded from CI quality gates** (no minimum threshold)
- Other modules (Domain, Application, Infrastructure, API) retain their coverage gates

**Why:** 
Client module coverage has been consistently problematic throughout Phase 1-3 due to:
- Markup-heavy Razor components with minimal C# logic
- Coverage tools measure C# only, not template rendering
- Functional test coverage quality ≠ coverage percentage for UI
- Focus should be on functional completeness, not percentage targets for UI layers

**Implication:**
- Report Client coverage as informational metric (FYI)
- Do NOT fail CI for Client coverage drops
- Continue writing high-quality functional tests (bUnit patterns)
- Prioritize meaningful test coverage over percentage targets for UI

**Decision:** Going forward, Client module is excluded from all CI quality gates while remaining visible in reports.
