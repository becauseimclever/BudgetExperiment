# CI/CD and Deployment Architecture

This document explains the automated build and deployment pipeline for BudgetExperiment.

## Overview

BudgetExperiment uses a **CI/CD-first approach** where Docker images are built automatically in GitHub Actions and deployed to target environments (like Raspberry Pi) by pulling pre-built images.

### Key Principles

1. **No local Docker builds** - Developers use standard .NET tooling (`dotnet run`)
2. **CI builds everything** - GitHub Actions builds multi-architecture Docker images
3. **Deploy by pulling** - Target servers pull images from GitHub Container Registry
4. **Separation of concerns** - Development workflow is distinct from deployment workflow

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         DEVELOPMENT                              │
│                                                                   │
│  Developer Machine                                               │
│  ┌─────────────────────────────────────────┐                    │
│  │  Local Development                       │                    │
│  │  - Edit code                             │                    │
│  │  - dotnet run (API + Client)            │                    │
│  │  - dotnet test                           │                    │
│  │  - User secrets for DB connection        │                    │
│  └─────────────────────────────────────────┘                    │
│                     │                                             │
│                     │ git push                                    │
│                     ▼                                             │
└─────────────────────────────────────────────────────────────────┘
                      │
                      │
┌─────────────────────────────────────────────────────────────────┐
│                       CI/CD PIPELINE                              │
│                                                                   │
│  GitHub Actions                                                   │
│  ┌─────────────────────────────────────────┐                    │
│  │  .github/workflows/                      │                    │
│  │  docker-build-publish.yml                │                    │
│  │                                          │                    │
│  │  1. Checkout code                        │                    │
│  │  2. Setup QEMU (multi-arch support)      │                    │
│  │  3. Setup Docker Buildx                  │                    │
│  │  4. Build for amd64 + arm64              │                    │
│  │  5. Publish to ghcr.io                   │                    │
│  └─────────────────────────────────────────┘                    │
│                     │                                             │
│                     │ docker push                                 │
│                     ▼                                             │
│  ┌─────────────────────────────────────────┐                    │
│  │  GitHub Container Registry               │                    │
│  │  ghcr.io/fortinbra/budgetexpirement     │                    │
│  │                                          │                    │
│  │  Tags:                                   │                    │
│  │  - latest                                │                    │
│  │  - main                                  │                    │
│  │  - v1.0.0                                │                    │
│  │  - v1.0                                  │                    │
│  └─────────────────────────────────────────┘                    │
└─────────────────────────────────────────────────────────────────┘
                      │
                      │ docker pull
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                        DEPLOYMENT                                 │
│                                                                   │
│  Raspberry Pi (or other servers)                                 │
│  ┌─────────────────────────────────────────┐                    │
│  │  1. Authenticate: docker login ghcr.io   │                    │
│  │  2. Create .env with DB connection       │                    │
│  │  3. Pull image: docker compose pull      │                    │
│  │  4. Start: docker compose up -d          │                    │
│  └─────────────────────────────────────────┘                    │
│                     │                                             │
│                     ▼                                             │
│  ┌─────────────────────────────────────────┐                    │
│  │  Running Container                       │                    │
│  │  - Connects to external PostgreSQL       │                    │
│  │  - Runs migrations on startup            │                    │
│  │  - Serves API + Blazor client            │                    │
│  │  - Exposed on port 5099                  │                    │
│  └─────────────────────────────────────────┘                    │
└─────────────────────────────────────────────────────────────────┘
```

## Components

### 1. Development Environment

**Location**: Developer workstations

**Tools**:
- .NET 10 SDK
- Visual Studio / VS Code
- PostgreSQL (local or remote)
- User secrets for configuration

**Workflow**:
```powershell
# Standard .NET development
dotnet run --project src/BudgetExperiment.Api/BudgetExperiment.Api.csproj
```

**Key Points**:
- NO Docker required
- API project serves both REST API and Blazor WebAssembly client
- Database connection via user secrets (never committed)
- Fast iteration cycles

### 2. CI/CD Pipeline

**Location**: GitHub Actions

**Trigger Events**:
- Push to `main` branch
- Creation of version tags (`v*`)
- Pull requests (build only, no publish)

**Workflow File**: `.github/workflows/docker-build-publish.yml`

**Steps**:
1. Checkout repository
2. Setup QEMU (for cross-architecture builds)
3. Setup Docker Buildx (multi-platform builder)
4. Login to GitHub Container Registry
5. Extract metadata (tags, labels)
6. Build Docker image for `linux/amd64` and `linux/arm64`
7. Push to `ghcr.io/fortinbra/budgetexpirement`

**Build Process**:
- Uses multi-stage Dockerfile
- Stage 1: Build from source using .NET SDK
- Stage 2: Create runtime image with only published output
- Supports both amd64 (x86_64) and arm64 (Raspberry Pi) architectures

**Image Tags**:
- `latest` - Always points to most recent main build
- `main` - Same as latest
- `v1.0.0` - Specific version (when tagged)
- `v1.0` - Major.minor version
- `v1` - Major version only
- `main-<sha>` - Specific commit from main branch

### 3. Container Registry

**Location**: GitHub Container Registry (ghcr.io)

**Image**: `ghcr.io/fortinbra/budgetexpirement`

**Access**:
- Public read access (default for public repos)
- Requires authentication with Personal Access Token (PAT) with `read:packages` scope
- Use: `docker login ghcr.io -u USERNAME -p PAT`

### 4. Deployment Environment

**Target**: Raspberry Pi (or any Linux server)

**Prerequisites**:
- Docker and Docker Compose installed
- Access to PostgreSQL database
- GitHub PAT with `read:packages` scope
- Network connectivity to ghcr.io and database

**Deployment File**: `docker-compose.pi.yml`

**Configuration**:
- Environment variables from `.env` file
- Database connection string: `ConnectionStrings__AppDb`
- Port mapping: 5099:8080
- Resource limits (CPU, memory)
- Health checks
- Restart policy

**Deployment Steps**:
```bash
# 1. Authenticate
docker login ghcr.io -u USERNAME -p PAT

# 2. Create .env
echo 'DB_CONNECTION_STRING=Host=...;Database=...' > .env
chmod 600 .env

# 3. Deploy
docker compose -f docker-compose.pi.yml up -d

# 4. Monitor
docker compose -f docker-compose.pi.yml logs -f
```

## Workflows

### Development Workflow

1. Developer clones repository
2. Sets up user secrets for local database:
   ```powershell
   dotnet user-secrets set "ConnectionStrings:AppDb" "..." --project src/BudgetExperiment.Api
   ```
3. Runs API: `dotnet run --project src/BudgetExperiment.Api/BudgetExperiment.Api.csproj`
4. Makes code changes
5. Tests locally
6. Opens a feature pull request into `develop`
7. Release-ready work is promoted from `develop` to `main`

### Build and Publish Workflow

1. Standard CI validates pushes and pull requests on `develop` and `main`
2. When `main` is ready to ship, a release tag is created from `main`
3. The Docker publish workflow triggers from that tag (or manual dispatch)
4. Multi-architecture Docker image is built
5. Image is pushed to ghcr.io with appropriate tags
6. Deployment environments can now pull the new image

### Update Deployment Workflow

1. On Raspberry Pi, pull latest image:
   ```bash
   docker compose -f docker-compose.pi.yml pull
   ```
2. Restart with new image:
   ```bash
   docker compose -f docker-compose.pi.yml up -d
   ```
3. Verify deployment:
   ```bash
   curl http://localhost:5099/health
   ```

## File Structure

### CI/CD Files
```
.github/
  workflows/
    docker-build-publish.yml    # Docker image build and publish workflow
    build-test-release.yml       # .NET build and test workflow (separate)
    pr-build-test.yml           # PR validation workflow
```

### Deployment Files
```
Dockerfile                      # Multi-stage build from source
docker-compose.pi.yml           # Raspberry Pi deployment config
.env.example                    # Example environment variables
```

### Documentation
```
README.Docker.md                # Comprehensive deployment guide
DEPLOY-QUICKSTART.md           # Quick start for Raspberry Pi
docs/
  ci-cd-deployment.md          # This file - architecture overview
```

### Deprecated Files

All legacy local build/deploy scripts have been removed from the repository to avoid confusion. Use CI/CD-built images and pull-based deployments only.

## Security Considerations

### Secrets Management

**Development**:
- Database connection strings stored in user secrets
- Never committed to repository
- Per-developer configuration

**CI/CD**:
- GitHub Actions uses `GITHUB_TOKEN` (auto-provided)
- No additional secrets required for ghcr.io push
- Repository visibility controls access

**Deployment**:
- Database connection in `.env` file on server
- File permissions: `chmod 600 .env`
- `.env` in `.gitignore`
- Never logged or exposed in container output

### Container Security

- **.NET runtime**: Microsoft `noble-chiseled` images — distroless Ubuntu, non-root by default (UID 1654), no shell or package manager, ~50% smaller attack surface
- **PostgreSQL**: Docker Hardened Image (`dhi.io/postgres:18`) — continuously patched, SLSA Build Level 3 provenance, minimal footprint
- Multi-stage builds minimize image size (SDK build stage not shipped)
- No HEALTHCHECK in Dockerfiles (chiseled images have no shell); health monitoring via external access to `/health` endpoint
- Resource limits prevent resource exhaustion
- **Policy**: Always prefer Docker Hardened Images when available; use Microsoft chiseled variants for .NET (not in Docker's hardened catalog). Fall back to Alpine/slim only when no hardened option exists.

### Network Security

- Database not in container (separation of concerns)
- Only necessary port exposed (5099)
- SSL/TLS for database connections (recommended)
- Reverse proxy with HTTPS recommended for production

## Maintenance

### Updating Dependencies

1. Update NuGet packages locally
2. Test changes
3. Commit and push
4. GitHub Actions rebuilds image automatically
5. Deploy updated image to servers

### Database Migrations

- Migrations run automatically on container startup
- Ensure database user has necessary permissions
- Consider backup before deploying migration changes
- Test migrations in development first

### Monitoring

**Container Health**:
```bash
docker compose -f docker-compose.pi.yml ps
docker stats budgetexperiment
```

**Application Health**:
```bash
curl http://localhost:5099/health
```

**Logs**:
```bash
docker compose -f docker-compose.pi.yml logs -f
```

## Troubleshooting

### Common Issues

**Can't pull image from ghcr.io**:
- Solution: Verify GitHub PAT has `read:packages` scope
- Solution: Check internet connectivity
- Solution: Re-authenticate: `docker login ghcr.io`

**Container starts but can't connect to database**:
- Solution: Verify `.env` file connection string
- Solution: Test connectivity from container to database server
- Solution: Check firewall rules
- Solution: Verify PostgreSQL allows remote connections

**Build fails in GitHub Actions**:
- Solution: Check workflow run logs in GitHub
- Solution: Verify Dockerfile syntax
- Solution: Check for .NET build errors

**Image works on amd64 but fails on arm64 (or vice versa)**:
- Solution: Check architecture-specific dependencies
- Solution: Verify .NET runtime for target architecture
- Solution: Test locally with Docker buildx

## Performance Considerations

### Build Performance

- GitHub Actions runners: Good performance for both architectures
- Buildx cache: Uses GitHub Actions cache for faster rebuilds
- Multi-stage builds: Separate layers for better caching

### Runtime Performance

- Raspberry Pi 3B+: Adequate for small workloads, reduce resource limits
- Raspberry Pi 4 (4GB+): Recommended for production
- x86_64 servers: Best performance

### Database Performance

- External PostgreSQL: Use connection pooling
- Keep database close to application (network latency)
- Regular maintenance (VACUUM, ANALYZE)

## Future Enhancements

### Potential Improvements

1. **Kubernetes Support**: Add Helm charts for k8s deployments
2. **Automated Rollbacks**: Health check-based rollback in deployments
3. **Multiple Environments**: Dev, staging, production tags/configs
4. **Secrets Management**: Integrate with HashiCorp Vault or Azure Key Vault
5. **Monitoring**: Add Prometheus metrics and Grafana dashboards
6. **Database in Container**: Optional containerized PostgreSQL for development
7. **ARM32 Support**: Add armv7 for older Raspberry Pi models

### Not Planned (By Design)

- Local Docker development workflow (use .NET tooling instead)
- Manual image builds (use CI/CD exclusively)
- Database migrations in separate step (automatic on startup is simpler)

## Conclusion

This architecture provides:
- **Simplicity**: Clear separation between development and deployment
- **Automation**: No manual build or deploy steps
- **Flexibility**: Multi-architecture support
- **Security**: Proper secrets management
- **Maintainability**: Standard CI/CD patterns
- **Developer Experience**: Fast local iteration with .NET tooling

The key insight: **Docker is a deployment artifact, not a development tool** for this project.
