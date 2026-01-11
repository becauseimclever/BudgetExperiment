# Feature: Automatic Database Migrations at Startup

## Overview
Ensure database schema is always up-to-date when the application starts by automatically applying pending EF Core migrations. This eliminates manual migration steps during deployment and ensures consistent database state across all environments.

## Current State

Currently, migrations and seeding only run in Development environment:

```csharp
if (app.Environment.IsDevelopment())
{
    await InitializeDatabaseAsync(app);
}
```

This means:
- ❌ Production deployments require manual `dotnet ef database update` commands
- ❌ Risk of deploying code that expects schema changes not yet applied
- ❌ More complex deployment scripts/procedures
- ✅ Seed data only runs in development (correct behavior)

## Desired Behavior

| Environment | Migrations | Seed Data |
|-------------|------------|-----------|
| Development | ✅ Auto-apply | ✅ Run |
| Staging | ✅ Auto-apply | ❌ Skip |
| Production | ✅ Auto-apply | ❌ Skip |

## User Stories

### US-001: Automatic Migrations on Startup
**As a** developer/operator  
**I want** database migrations to apply automatically when the application starts  
**So that** I don't have to manually run migration commands during deployment

### US-002: Development-Only Seed Data
**As a** developer  
**I want** seed data to only run in development  
**So that** production and staging databases aren't polluted with test data

### US-003: Migration Failure Handling
**As an** operator  
**I want** clear logging and error handling when migrations fail  
**So that** I can diagnose and resolve database issues quickly

### US-004: Migration Health Check
**As an** operator  
**I want** to verify the database is accessible and migrations are applied  
**So that** I can monitor deployment health

---

## Implementation Design

### Updated Program.cs Flow

```csharp
// Apply migrations in ALL environments
await ApplyMigrationsAsync(app);

// Seed data ONLY in development
if (app.Environment.IsDevelopment())
{
    await SeedDevelopmentDataAsync(app);
}
```

### Migration Method

```csharp
private static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var pendingCount = pendingMigrations.Count();
        
        if (pendingCount > 0)
        {
            logger.LogInformation(
                "Applying {Count} pending database migration(s): {Migrations}",
                pendingCount,
                string.Join(", ", pendingMigrations));
            
            await context.Database.MigrateAsync();
            
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date. No migrations to apply.");
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to apply database migrations. Application cannot start.");
        throw; // Fail fast - don't start with outdated schema
    }
}
```

### Seed Data Method (Development Only)

```csharp
private static async Task SeedDevelopmentDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await DatabaseSeeder.SeedAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed development data. Continuing startup...");
        // Don't throw - seed failure shouldn't prevent app from starting in dev
    }
}
```

### Test Environment Handling

For integration tests using in-memory database:

```csharp
private static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Check if using relational database
        if (context.Database.IsRelational())
        {
            // Apply migrations for real databases
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            // ... migration logic
        }
        else
        {
            // In-memory/non-relational: just ensure schema exists
            logger.LogInformation("Non-relational database detected. Ensuring schema is created.");
            await context.Database.EnsureCreatedAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to initialize database.");
        throw;
    }
}
```

---

## Configuration Options (Optional Enhancement)

Add configuration to control migration behavior:

### appsettings.json

```json
{
  "Database": {
    "AutoMigrate": true,
    "MigrationTimeoutSeconds": 300
  }
}
```

### Environment-Specific Override

```json
// appsettings.Production.json
{
  "Database": {
    "AutoMigrate": true
  }
}
```

### Configuration Class

```csharp
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    
    /// <summary>
    /// Gets or sets a value indicating whether migrations should run automatically at startup.
    /// Default: true
    /// </summary>
    public bool AutoMigrate { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the timeout for migration operations in seconds.
    /// Default: 300 (5 minutes)
    /// </summary>
    public int MigrationTimeoutSeconds { get; set; } = 300;
}
```

---

## Logging Strategy

### Log Levels by Scenario

| Scenario | Level | Message |
|----------|-------|---------|
| No pending migrations | Information | "Database is up to date. No migrations to apply." |
| Migrations pending | Information | "Applying {Count} pending migration(s): {Names}" |
| Migrations successful | Information | "Database migrations applied successfully." |
| Migration failure | Critical | "Failed to apply database migrations. Application cannot start." |
| Seed data skipped (has data) | Information | "Database already contains data, skipping seed." |
| Seed data applied | Information | "Seeding database with sample data..." |
| Seed failure (dev) | Warning | "Failed to seed development data. Continuing startup..." |

### Structured Logging Properties

```csharp
logger.LogInformation(
    "Applying {MigrationCount} pending migration(s) to database {DatabaseName}",
    pendingCount,
    context.Database.GetDbConnection().Database);
```

---

## Health Check Integration

### Database Health Check

The existing `/health` endpoint should verify database connectivity. Optionally add migration status:

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BudgetDbContext>("database");
```

### Custom Migration Health Check (Optional)

```csharp
public sealed class MigrationHealthCheck : IHealthCheck
{
    private readonly BudgetDbContext _context;

    public MigrationHealthCheck(BudgetDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pending = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            
            if (pending.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Database has {pending.Count()} pending migrations");
            }
            
            return HealthCheckResult.Healthy("Database is up to date");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cannot check database status", ex);
        }
    }
}
```

---

## Implementation Plan (TDD Order)

### Phase 1: Refactor Program.cs
1. [ ] Extract `ApplyMigrationsAsync()` method (runs in all environments)
2. [ ] Extract `SeedDevelopmentDataAsync()` method (runs only in Development)
3. [ ] Update `InitializeDatabaseAsync()` or replace with new methods
4. [ ] Add proper logging with migration names
5. [ ] Handle non-relational database providers (for tests)

### Phase 2: Configuration (Optional)
1. [ ] Create `DatabaseOptions` configuration class
2. [ ] Add configuration section to appsettings.json
3. [ ] Wire up configuration in DI
4. [ ] Use configuration to control auto-migrate behavior

### Phase 3: Health Checks
1. [ ] Verify existing database health check works
2. [ ] Optionally add migration status health check

### Phase 4: Testing
1. [ ] Verify integration tests still work (in-memory handling)
2. [ ] Test with actual PostgreSQL database
3. [ ] Test migration failure scenarios
4. [ ] Test seed data only runs in Development

### Phase 5: Documentation
1. [ ] Update deployment documentation
2. [ ] Document migration timeout configuration
3. [ ] Document how to disable auto-migrate if needed

---

## Deployment Considerations

### First Deployment
- Application will create all tables via migrations
- Database must exist (create via `CREATE DATABASE budgetexperiment;`)
- Connection string must have permissions to create tables

### Subsequent Deployments
- Only new migrations will be applied
- Migrations are idempotent (safe to run multiple times via EF tracking)
- Downtime depends on migration complexity

### Rollback Strategy
If a migration fails:
1. Application won't start (fail fast)
2. Fix the migration or database issue
3. Redeploy

For rollback to previous version:
1. May need manual `dotnet ef database update <PreviousMigration>`
2. Or create a "down" migration
3. Document breaking migrations that can't be rolled back

### Blue-Green Deployments
- Migrations should be backward-compatible when possible
- Add new columns as nullable first
- Remove columns in a later release after code no longer uses them

---

## Edge Cases

1. **Multiple Instances Starting Simultaneously**
   - EF Core migrations are idempotent
   - Use database locking if needed (EF handles this)
   - May want to add explicit lock for very large deployments

2. **Migration Timeout**
   - Large data migrations may exceed default timeout
   - Use `MigrationTimeoutSeconds` configuration
   - Consider breaking large migrations into smaller ones

3. **Database Connection Failure**
   - Log clearly and fail fast
   - Kubernetes/Docker will restart container
   - Health check will report unhealthy

4. **Permission Issues**
   - Connection string user must have DDL permissions
   - Log specific error for permission denied

5. **Concurrent Schema Changes**
   - Don't run manual migrations while app is starting
   - Use deployment locks/gates in CI/CD

---

## Security Considerations

1. **Migration User Permissions**
   - Production: Use separate migration user with DDL rights
   - Or: Same user but audit DDL operations
   - Never use superuser/admin for application connections

2. **Connection String Security**
   - Already handled via user-secrets (dev) and environment variables (prod)
   - Ensure migration logs don't leak connection strings

3. **Audit Trail**
   - EF Core's `__EFMigrationsHistory` table tracks applied migrations
   - Application logs record when migrations were applied

---

## Future Enhancements

- [ ] Migration dry-run mode (show what would be applied)
- [ ] Pre-flight check endpoint for deployments
- [ ] Slack/Teams notification on migration completion
- [ ] Automatic backup before migrations (for self-hosted)
- [ ] Migration execution metrics (duration, row counts)
- [ ] Database versioning API endpoint for diagnostics
