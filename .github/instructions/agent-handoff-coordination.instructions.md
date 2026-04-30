---
description: "Use when coordinating multi-agent work, delegating subtasks, or handing off outputs between specialists. Enforces clear ownership, dependency tracking, acceptance criteria, and final synthesis quality checks."
name: "Agent Handoff Coordination"
---
# Agent Handoff Coordination

Use this instruction when a task requires more than one specialist agent or when work must pass between agents.

## Goals
- Keep handoffs clear, fast, and low-risk.
- Prevent duplicated work, dropped requirements, and unclear ownership.
- Ensure each handoff includes enough context for the next agent to execute confidently.

## Core Rules
- Assign a single owner for each subtask.
- Define explicit acceptance criteria before delegation.
- Include dependencies and execution order for every subtask.
- Prefer parallel execution only when subtasks are independent.
- Route all coordinated task outputs through Dotnet Auditor Reviewer before final delivery.
- Treat this instruction as a hard rule for coordinated multi-agent work.
- When requirements, scope, or constraints are ambiguous, ask for clarification before proceeding.

## Required Handoff Package
Every handoff must include:
- Task objective: what outcome is required.
- Scope boundaries: what is in scope and out of scope.
- Input context: files, constraints, standards, and assumptions.
- Deliverables: exact expected outputs.
- Validation: required checks or tests.
- Done definition: measurable completion criteria.

## Coordination Workflow
1. Classify the request by domain and risk.
2. Break work into subtasks with clear ownership.
3. Mark dependency graph: parallel vs sequential execution.
4. Delegate with a complete handoff package.
5. Review outputs for conflicts, gaps, and unmet criteria.
6. Route all coordinated outputs to Dotnet Auditor Reviewer for standards validation.
7. Merge outcomes into one final response with clear ownership notes.

## Conflict and Blocker Handling
- If two agent outputs conflict, pause synthesis and resolve by repository standards and task requirements.
- If requirements are ambiguous, ask for clarification first. If immediate progress is required, state assumptions explicitly, continue with lowest-risk steps only, and request confirmation.
- If a dependency is blocked, report blocker, impact, and the next best executable step.

## Quality Checks Before Final Response
- All subtasks have an owner and status.
- Acceptance criteria are met or explicitly flagged.
- Cross-agent assumptions are reconciled.
- Audit gate is completed for all coordinated work.
- Final response includes a concise completion summary.
