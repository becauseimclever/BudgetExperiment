---
description: "Use when evaluating, planning, or executing feature or bug-fix requests. Enforces spec-driven development: a feature document must exist before implementation begins."
name: "Spec-Driven Work Gate"
---
# Spec-Driven Work Gate

Use this instruction for all feature and bug-fix coordination and execution.

## Policy
- Always follow a spec-driven development process.
- A feature document must exist before any feature or bug-fix implementation begins.
- If a feature or bug-fix request has no feature document, stop implementation work.
- The only allowed action without an existing feature document is creating the new feature document.

## Definition of Ready for Implementation Work
Implementation can start only when all items below are true:
- A feature document exists in `docs/` and follows repository conventions.
- The feature document includes clear scope, acceptance criteria, and implementation tasks.
- Open questions and assumptions are captured in the feature document.

## Required Behavior When Spec Is Missing
1. Do not start coding, refactoring, migration work, or implementation testing for the requested change.
2. Inform the user that implementation work is blocked until a feature document exists.
3. Offer to create the feature document immediately using the repository template and numbering convention.
4. Resume implementation only after the feature document is created and approved for execution.

## Coordination and Handoff Rules
- During multi-agent work, verify feature-doc existence before delegating implementation subtasks.
- Include the feature document path in every implementation handoff package.
- If the feature document changes materially during execution, re-check acceptance criteria before continuing.

## Completion Check
Before closing any implementation task, confirm:
- Feature document path is referenced.
- Implemented work maps to documented acceptance criteria.
- Any scope changes are reflected in the feature document.
