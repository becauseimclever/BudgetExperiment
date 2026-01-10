# Feature 001: Database Reset

## Status
**Status:** Completed  
**Created:** 2026-01-09  
**Completed:** 2026-01-09  
**Priority:** High

## Overview

Reset the database schema and data to start fresh with a new architectural direction.

## Goals

- [x] Drop all existing tables and data
- [x] Remove or archive existing EF Core migrations
- [x] Create a clean slate for new domain model design
- [x] Document the new starting point

## Background

The current database schema was built incrementally during early experimentation. To move forward with a cleaner architecture, we need to reset and rebuild from scratch.

## Implementation Plan

### Phase 1: Backup & Archive
1. Export any valuable sample data if needed
2. Archive existing migrations for reference

### Phase 2: Database Reset
1. Drop existing database or create new empty database
2. Remove existing migrations from `BudgetExperiment.Infrastructure/Migrations`
3. Update `BudgetDbContext` if needed for new entity structure

### Phase 3: Fresh Start
1. Design new domain entities (TDD approach)
2. Create initial migration
3. Apply migration to fresh database

## Technical Notes

### Database Connection
The connection string is stored in user secrets for `BudgetExperiment.Api`:
```bash
dotnet user-secrets set "ConnectionStrings:AppDb" "<connection-string>" --project c:\ws\BudgetExpirement\src\BudgetExperiment.Api
```

### Migration Commands
```bash
# Remove all migrations (manual deletion recommended)
# Location: c:\ws\BudgetExpirement\src\BudgetExperiment.Infrastructure\Migrations\

# Create new initial migration
dotnet ef migrations add InitialCreate --project c:\ws\BudgetExpirement\src\BudgetExperiment.Infrastructure --startup-project c:\ws\BudgetExpirement\src\BudgetExperiment.Api

# Apply migration
dotnet ef database update --project c:\ws\BudgetExpirement\src\BudgetExperiment.Infrastructure --startup-project c:\ws\BudgetExpirement\src\BudgetExperiment.Api
```

### Drop Database (if needed)
```sql
-- PostgreSQL
DROP DATABASE IF EXISTS budgetexperiment;
CREATE DATABASE budgetexperiment;
```

## Acceptance Criteria

- [ ] Existing migrations archived or removed
- [ ] Database is empty/reset
- [ ] New initial migration can be created successfully
- [ ] Application starts without errors against clean database
- [ ] Ready for new domain model development

## Dependencies

None - this is a foundational reset.

## Related Features

- Next: TBD (new domain model design)

## Notes

This reset enables a fresh start for the budget application architecture. Future features will be numbered sequentially (002, 003, etc.) for easy reference.
