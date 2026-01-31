# Feature 037: Automated Demo Site Deployment
> **Status:** ⏸️ On Hold (Depends on 036 - E2E tests disabled until auth is fixed)

## Overview

Automate the deployment of new releases to the demo environment (`budgetdemo.becauseimclever.com`) when a release build completes successfully. When a version tag is pushed and the Docker image is published to ghcr.io, the demo server should automatically pull and deploy the new version without manual intervention.

## Problem Statement

### Current State

- Docker images are automatically built and published to ghcr.io when version tags are pushed
- GitHub Release is automatically created with changelog
- Demo server requires **manual intervention** to pull and deploy new versions
- Deployment steps: SSH to server → `docker compose pull` → `docker compose up -d`
- Risk of demo site running outdated versions between releases
- No visibility into deployment status from GitHub

### Target State

- Demo server automatically updates when a new release is published
- Zero manual steps required for demo deployment
- Deployment status visible in GitHub Actions with dedicated job
- Health check verification ensures deployment succeeded
- Rollback capability if deployment fails
- Clear documentation for troubleshooting

---

## User Stories

### Automated Deployment

#### US-037-001: Trigger Deployment on Release
**As a** developer
**I want** the demo site to automatically update when I push a version tag
**So that** the demo always reflects the latest release without manual work

**Acceptance Criteria:**
- [ ] Deployment triggers after Docker image is successfully published
- [ ] Demo server pulls the specific version tag (not just `latest`)
- [ ] Application restarts with the new version
- [ ] Health check confirms successful deployment
- [ ] GitHub Actions job shows green/red status

#### US-037-002: Deployment Status Visibility
**As a** developer
**I want** to see deployment status in GitHub Actions
**So that** I know if the demo update succeeded or failed

**Acceptance Criteria:**
- [ ] GitHub Actions shows separate `deploy-demo` job
- [ ] Logs indicate which version was deployed
- [ ] Failed deployments are clearly marked with reason
- [ ] Deployment duration is visible
- [ ] Environment link shows deployment URL

#### US-037-003: Secure Remote Deployment
**As a** system administrator
**I want** deployments to use secure authentication
**So that** only authorized CI/CD pipelines can trigger deployments

**Acceptance Criteria:**
- [ ] Ed25519 SSH key authentication for remote commands
- [ ] Secrets stored securely in GitHub Actions (environment secrets)
- [ ] No credentials exposed in logs
- [ ] Deploy user has minimal permissions (docker group only)
- [ ] SSH key restricted to deployment commands only

#### US-037-004: Automatic Health Verification
**As a** developer
**I want** automatic health verification after deployment
**So that** I know immediately if the new version is working

**Acceptance Criteria:**
- [ ] Health endpoint (`/health`) checked after deployment
- [ ] Deployment marked as failed if health check fails after 60s
- [ ] Container status logged for debugging
- [ ] Version endpoint shows deployed version

---

## Technical Design

### Deployment Strategy Comparison

| Approach | Complexity | Visibility | Security | Recommendation |
|----------|------------|------------|----------|----------------|
| **SSH Remote Execution** | Low | High (GH Actions logs) | Medium (SSH key) | ✅ Recommended |
| Webhook Trigger | Medium | Medium | Medium | Future option |
| Watchtower | Low | Low | High (no inbound) | Not recommended |

### Recommended Approach: SSH Remote Execution

GitHub Actions SSHs to the demo server and executes docker compose commands directly.

**Why SSH?**
- Simple, direct control over deployment
- Uses existing `docker-compose.pi.yml` infrastructure
- Full visibility into deployment steps
- Easy to debug and extend
- No additional services to maintain on server

**Security Mitigations:**
- Dedicated `deploy` user with minimal permissions
- Ed25519 key (more secure than RSA)
- Key can be restricted to specific commands via `authorized_keys`
- GitHub Environment protection rules for approval gates

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  GitHub Actions                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  docker-build-publish.yml                                  │ │
│  │                                                            │ │
│  │  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐ │ │
│  │  │ build-and-   │───>│ create-      │───>│ deploy-demo  │ │ │
│  │  │ push         │    │ release      │    │              │ │ │
│  │  └──────────────┘    └──────────────┘    └──────────────┘ │ │
│  │        │                                        │          │ │
│  │        │ push image                             │ SSH      │ │
│  │        ▼                                        ▼          │ │
│  └────────────────────────────────────────────────────────────┘ │
│            │                                        │            │
│            ▼                                        │            │
│  ┌──────────────────────┐                          │            │
│  │  ghcr.io/...         │                          │            │
│  │  budgetexperiment:X  │                          │            │
│  └──────────────────────┘                          │            │
│            │                                        │            │
└────────────│────────────────────────────────────────│────────────┘
             │                                        │
             │ docker pull                            │
             ▼                                        ▼
┌─────────────────────────────────────────────────────────────────┐
│  Demo Server (budgetdemo.becauseimclever.com)                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  /opt/budgetexperiment-demo/                               │ │
│  │  ├── docker-compose.demo.yml                               │ │
│  │  └── .env                                                  │ │
│  │                                                            │ │
│  │  deploy@server:                                            │ │
│  │  - docker compose pull                                     │ │
│  │  - docker compose up -d                                    │ │
│  │  - health check verification                               │ │
│  └────────────────────────────────────────────────────────────┘ │
│                            │                                     │
│                            ▼                                     │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  NGINX (reverse proxy)                                     │ │
│  │  - TLS termination                                         │ │
│  │  - proxy_pass to :5100                                     │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### GitHub Secrets Required

| Secret | Description | Example |
|--------|-------------|---------|
| `DEMO_HOST` | Demo server hostname/IP | `demo.example.com` |
| `DEMO_USER` | SSH username for deployment | `deploy` |
| `DEMO_SSH_KEY` | Private Ed25519 SSH key | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `DEMO_PORT` | SSH port (optional, default 22) | `22` |

### GitHub Environment Configuration

Create a `demo` environment in GitHub repository settings:

1. **Settings** → **Environments** → **New environment** → Name: `demo`
2. **Environment secrets** (add all required secrets)
3. **Deployment branches**: Select "Selected branches" → Add `v*` pattern
4. **Environment URL**: `https://budgetdemo.becauseimclever.com`
5. **Protection rules** (optional):
   - Required reviewers: Add team members for approval gate
   - Wait timer: 0 minutes (immediate after approval)

### Demo Server Docker Compose

Create a dedicated compose file for the demo environment:

```yaml
# docker-compose.demo.yml
# Demo site configuration - different port to avoid conflict with production
# Located at: /opt/budgetexperiment-demo/docker-compose.demo.yml

services:
  budgetexperiment-demo:
    image: ghcr.io/becauseimclever/budgetexperiment:${VERSION:-latest}
    container_name: budgetexperiment-demo
    ports:
      - "5100:8080"  # Demo uses port 5100 (nginx proxies from 443)
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__AppDb=${DB_CONNECTION_STRING}
      - Authentication__Authentik__Enabled=${AUTHENTIK_ENABLED:-true}
      - Authentication__Authentik__Authority=${AUTHENTIK_AUTHORITY}
      - Authentication__Authentik__Audience=${AUTHENTIK_AUDIENCE}
      - Authentication__Authentik__RequireHttpsMetadata=true
      - Logging__LogLevel__Default=Information
      - Logging__LogLevel__Microsoft.AspNetCore=Warning
      - AllowedHosts=*
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 512M
```

### Workflow Enhancement

Add new job to `.github/workflows/docker-build-publish.yml`:

```yaml
  deploy-demo:
    name: Deploy to Demo
    needs: build-and-push
    if: startsWith(github.ref, 'refs/tags/v')
    runs-on: ubuntu-latest
    environment:
      name: demo
      url: https://budgetdemo.becauseimclever.com
    
    steps:
      - name: Extract version from tag
        id: version
        run: |
          VERSION=${GITHUB_REF#refs/tags/v}
          echo "version=$VERSION" >> $GITHUB_OUTPUT
          echo "Deploying version: $VERSION"

      - name: Deploy via SSH
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ secrets.DEMO_HOST }}
          username: ${{ secrets.DEMO_USER }}
          key: ${{ secrets.DEMO_SSH_KEY }}
          port: ${{ secrets.DEMO_PORT || 22 }}
          script: |
            set -e
            cd /opt/budgetexperiment-demo
            
            echo "=== Deployment starting for version ${{ steps.version.outputs.version }} ==="
            
            # Set version for docker compose
            export VERSION="${{ steps.version.outputs.version }}"
            
            echo "Pulling image version: $VERSION"
            docker compose -f docker-compose.demo.yml pull
            
            echo "Stopping current container..."
            docker compose -f docker-compose.demo.yml down --timeout 30
            
            echo "Starting new version..."
            docker compose -f docker-compose.demo.yml up -d
            
            echo "Waiting for container to be healthy..."
            sleep 15
            
            echo "Container status:"
            docker compose -f docker-compose.demo.yml ps
            
            echo "=== Deployment script complete ==="

      - name: Wait for Application Startup
        run: sleep 30

      - name: Verify Health Endpoint
        run: |
          echo "Checking health endpoint..."
          for i in {1..6}; do
            if curl -sf https://budgetdemo.becauseimclever.com/health; then
              echo "Health check passed!"
              exit 0
            fi
            echo "Attempt $i/6 failed, waiting 10 seconds..."
            sleep 10
          done
          echo "Health check failed after 6 attempts"
          exit 1

      - name: Verify Deployed Version
        run: |
          echo "Checking deployed version..."
          DEPLOYED=$(curl -sf https://budgetdemo.becauseimclever.com/api/v1/config 2>/dev/null | jq -r '.version' || echo "unknown")
          EXPECTED="${{ steps.version.outputs.version }}"
          echo "Deployed: $DEPLOYED"
          echo "Expected: $EXPECTED"
          if [[ "$DEPLOYED" == "$EXPECTED" ]]; then
            echo "✅ Version verified!"
          else
            echo "⚠️ Version mismatch (may be expected if version endpoint differs)"
          fi

      - name: Deployment Summary
        run: |
          echo "## Demo Deployment Complete 🚀" >> $GITHUB_STEP_SUMMARY
          echo "" >> $GITHUB_STEP_SUMMARY
          echo "| Property | Value |" >> $GITHUB_STEP_SUMMARY
          echo "|----------|-------|" >> $GITHUB_STEP_SUMMARY
          echo "| Version | ${{ steps.version.outputs.version }} |" >> $GITHUB_STEP_SUMMARY
          echo "| URL | https://budgetdemo.becauseimclever.com |" >> $GITHUB_STEP_SUMMARY
          echo "| Health | ✅ Verified |" >> $GITHUB_STEP_SUMMARY
```

---

## Implementation Plan

### Phase 1: Demo Server Preparation

**Objective:** Prepare the demo server for automated deployments

**Tasks:**
- [ ] Create dedicated `deploy` user on demo server
- [ ] Generate Ed25519 SSH key pair for GitHub Actions
- [ ] Configure SSH authorized_keys on server
- [ ] Create deployment directory `/opt/budgetexperiment-demo/`
- [ ] Copy `docker-compose.demo.yml` and `.env` to server
- [ ] Test manual SSH access and docker commands
- [ ] Verify NGINX proxy configuration (port 5100)

**Server Setup Commands:**
```bash
# Create deploy user
sudo useradd -m -s /bin/bash deploy
sudo usermod -aG docker deploy

# Create SSH directory
sudo mkdir -p /home/deploy/.ssh
sudo chmod 700 /home/deploy/.ssh

# Generate SSH key pair (run on local machine, copy private to GitHub)
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github_deploy_key

# Add public key to server
echo "ssh-ed25519 AAAA... github-actions-deploy" | sudo tee /home/deploy/.ssh/authorized_keys
sudo chmod 600 /home/deploy/.ssh/authorized_keys
sudo chown -R deploy:deploy /home/deploy/.ssh

# Create deployment directory
sudo mkdir -p /opt/budgetexperiment-demo
sudo chown deploy:deploy /opt/budgetexperiment-demo

# Copy files (as deploy user)
sudo -u deploy cp docker-compose.demo.yml /opt/budgetexperiment-demo/
sudo -u deploy cp .env /opt/budgetexperiment-demo/

# Verify docker access
sudo -u deploy docker ps
```

**Commit:**
```
docs(deploy): document demo server setup for automation

- Add deployment user creation steps
- Document SSH key configuration
- Specify directory structure
- Add nginx port verification

Refs: #037
```

---

### Phase 2: GitHub Environment Setup

**Objective:** Configure GitHub repository for secure deployments

**Tasks:**
- [ ] Create `demo` environment in repository settings
- [ ] Add `DEMO_HOST` environment secret
- [ ] Add `DEMO_USER` environment secret
- [ ] Add `DEMO_SSH_KEY` environment secret (private key)
- [ ] Configure deployment branches to `v*` tags only
- [ ] Set environment URL to `https://budgetdemo.becauseimclever.com`
- [ ] (Optional) Add required reviewers for approval gate

**Verification:**
- Environment appears in Settings → Environments
- Secrets show as configured (not visible)
- Test deployment triggers only on version tags

---

### Phase 3: Docker Compose for Demo

**Objective:** Create dedicated demo compose configuration

**Tasks:**
- [ ] Create `docker-compose.demo.yml` with demo-specific settings
- [ ] Configure port 5100 (different from production 5099)
- [ ] Add VERSION environment variable support
- [ ] Test locally that compose file is valid
- [ ] Deploy to `/opt/budgetexperiment-demo/` on server

**Commit:**
```
feat(deploy): add docker-compose.demo.yml for automated deployment

- Demo-specific compose with VERSION variable
- Port 5100 to separate from production
- Resource limits for demo environment

Refs: #037
```

---

### Phase 4: Workflow Enhancement

**Objective:** Add deployment job to CI/CD workflow

**Tasks:**
- [ ] Add `deploy-demo` job to `docker-build-publish.yml`
- [ ] Configure job dependency on `build-and-push`
- [ ] Add condition to run only on version tags
- [ ] Implement SSH deployment using `appleboy/ssh-action`
- [ ] Add health check verification step
- [ ] Add version verification step
- [ ] Add deployment summary to GitHub step summary
- [ ] Test with a patch release (e.g., v3.9.1)

**Commit:**
```
ci(deploy): add automated demo deployment on release

- New deploy-demo job triggered on version tags
- SSH-based deployment to demo server
- Health check verification after deployment
- Version verification and summary
- Uses GitHub environment for secrets

Refs: #037
```

---

### Phase 5: Testing & Verification

**Objective:** Verify the complete deployment pipeline

**Tasks:**
- [ ] Create test tag (e.g., `v3.99.0-test`) and push
- [ ] Verify build completes successfully
- [ ] Verify deploy-demo job triggers
- [ ] Verify SSH connection succeeds
- [ ] Verify container pulls and starts
- [ ] Verify health check passes
- [ ] Verify demo site accessible at https://budgetdemo.becauseimclever.com
- [ ] Delete test tag after verification
- [ ] Test rollback procedure manually

---

### Phase 6: Documentation

**Objective:** Update deployment documentation

**Tasks:**
- [ ] Update `ci-cd-deployment.md` with new workflow diagram
- [ ] Add troubleshooting section
- [ ] Document rollback procedures
- [ ] Update feature doc status to Complete
- [ ] Add to CHANGELOG

**Commit:**
```
docs(deploy): document automated demo deployment

- Update CI/CD architecture diagram
- Add troubleshooting guide
- Document rollback procedures
- Complete feature 037

Refs: #037
```

---

## Testing Strategy

### Automated Tests

| Test | Description | Verification |
|------|-------------|--------------|
| Build triggers deploy | Push version tag → deploy job runs | GitHub Actions UI |
| SSH connection | Deploy user can connect | SSH step succeeds |
| Container pull | Correct version pulled | Docker logs |
| Health check | App responds on /health | Curl exit code 0 |
| Version match | Deployed version matches tag | API response |

### Manual Testing Checklist

- [ ] Push a test version tag (e.g., `v99.0.0-test`)
- [ ] Watch GitHub Actions for all jobs to complete
- [ ] SSH to demo server, verify container running
- [ ] Open https://budgetdemo.becauseimclever.com in browser
- [ ] Check /health endpoint returns 200
- [ ] Check /api/v1/config returns correct version
- [ ] Delete test tag: `git tag -d v99.0.0-test && git push origin :refs/tags/v99.0.0-test`

### Failure Scenarios to Test

- [ ] Invalid SSH key → Job should fail with auth error
- [ ] Container fails to start → Health check should fail
- [ ] Network timeout → SSH step should timeout and fail
- [ ] Invalid version tag → Container pull should fail

---

## Security Considerations

### SSH Key Security

| Aspect | Implementation |
|--------|----------------|
| Key type | Ed25519 (more secure, shorter than RSA) |
| Key storage | GitHub Environment Secrets (encrypted) |
| Key rotation | Document procedure, rotate annually |
| Access scope | Deploy user only has docker group access |

### Deploy User Restrictions

```bash
# The deploy user should have:
# ✅ docker group membership (to run docker commands)
# ❌ sudo privileges (not needed)
# ❌ shell login from password (key-only auth)
# ❌ access to other directories (chroot optional)

# Verify user permissions
groups deploy
# Output: deploy docker

# Lock password (key-only auth)
sudo passwd -l deploy
```

### Network Security

- Demo server firewall allows SSH from any IP (GitHub Actions IPs vary)
- Alternative: Use self-hosted runner inside network
- Consider fail2ban for SSH brute-force protection
- HTTPS enforced via nginx (TLS termination)

### Command Restriction (Optional)

For maximum security, restrict the SSH key to specific commands:

```bash
# In /home/deploy/.ssh/authorized_keys:
command="cd /opt/budgetexperiment-demo && docker compose -f docker-compose.demo.yml $SSH_ORIGINAL_COMMAND",no-port-forwarding,no-X11-forwarding,no-agent-forwarding ssh-ed25519 AAAA...
```

---

## Rollback Procedure

### Quick Rollback

If a deployment causes issues, rollback to previous version:

```bash
# SSH to demo server
ssh deploy@budgetdemo.becauseimclever.com

# Go to deployment directory
cd /opt/budgetexperiment-demo

# Check available image versions locally
docker images ghcr.io/becauseimclever/budgetexperiment --format "{{.Tag}}"

# Update to specific previous version
export VERSION=3.8.2
docker compose -f docker-compose.demo.yml pull
docker compose -f docker-compose.demo.yml up -d

# Verify rollback
curl https://budgetdemo.becauseimclever.com/health
```

### Full Rollback Steps

1. **Identify last working version** from GitHub Releases or image tags
2. **SSH to demo server** as deploy user
3. **Set VERSION environment variable** to previous version
4. **Pull and restart** using docker compose
5. **Verify health** endpoint responds
6. **Investigate** the failing version in staging

---

## Troubleshooting

### Common Issues

| Issue | Symptoms | Solution |
|-------|----------|----------|
| SSH connection refused | Job fails at SSH step | Check firewall, SSH service, IP allowlist |
| Permission denied (publickey) | Auth failure in logs | Verify key matches, authorized_keys permissions |
| Container fails to start | Health check times out | Check `docker logs budgetexperiment-demo` |
| Health check fails | Curl returns non-200 | App error, check container logs |
| Version mismatch | Old version still running | Verify VERSION env var, check image pulled |

### Debug Commands

```bash
# Check container status
docker compose -f docker-compose.demo.yml ps

# View container logs
docker compose -f docker-compose.demo.yml logs --tail 100

# Check if correct image version
docker inspect budgetexperiment-demo --format '{{.Config.Image}}'

# Manual health check
curl -v http://localhost:5100/health

# Check disk space (images can fill disk)
df -h
docker system df

# Clean old images
docker image prune -a --filter "until=168h"
```

### GitHub Actions Debugging

```yaml
# Add to workflow for debugging
- name: Debug SSH
  run: |
    echo "Host: ${{ secrets.DEMO_HOST }}"
    echo "User: ${{ secrets.DEMO_USER }}"
    # Never echo the key!
```

---

## Monitoring & Observability

### Current Monitoring

| What | How | Where |
|------|-----|-------|
| Deployment status | GitHub Actions | Actions tab |
| Container health | Docker healthcheck | `docker ps` |
| Application health | /health endpoint | Browser/curl |
| Version deployed | /api/v1/config | API response |

### Future Enhancements

- [ ] Slack/Discord notifications on deploy success/failure
- [ ] Deployment metrics (duration, success rate)
- [ ] Uptime monitoring (external service)
- [ ] Log aggregation

---

## Future Enhancements

| Enhancement | Description | Priority |
|-------------|-------------|----------|
| Slack notifications | Alert on deploy success/failure | Medium |
| Automatic rollback | Revert if health check fails | High |
| Blue-green deployment | Zero-downtime updates | Low |
| Multiple environments | Extend to staging/production | Medium |
| Deployment approval | Require manual approval | Low |
| Self-hosted runner | Run inside private network | Low |

---

## References

- [GitHub Actions SSH Deploy](https://github.com/appleboy/ssh-action) - SSH action used
- [GitHub Environments](https://docs.github.com/en/actions/deployment/targeting-different-environments) - Environment configuration
- [Docker Compose in Production](https://docs.docker.com/compose/production/) - Best practices
- [ci-cd-deployment.md](ci-cd-deployment.md) - Current CI/CD documentation
- [docker-compose.pi.yml](../docker-compose.pi.yml) - Base compose file

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @copilot |
| 2026-01-30 | Fleshed out technical design, added architecture diagram, troubleshooting, testing strategy | @copilot |
