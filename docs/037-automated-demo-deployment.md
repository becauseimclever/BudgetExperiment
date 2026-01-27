# Feature 037: Automated Demo Site Deployment
> **Status:** 🗒️ Planning

## Status: Planning 📋

## Overview

Automate the deployment of new releases to the demo environment (`budgetdemo.becauseimclever.com`) when a release build completes successfully. When a version tag is pushed and the Docker image is published to ghcr.io, the demo server should automatically pull and deploy the new version without manual intervention.

## Problem Statement

### Current State

- Docker images are automatically built and published to ghcr.io when version tags are pushed
- GitHub Release is automatically created with changelog
- Demo server requires **manual intervention** to pull and deploy new versions
- Deployment steps: SSH to server → `docker compose pull` → `docker compose up -d`
- Risk of demo site running outdated versions between releases

### Target State

- Demo server automatically updates when a new release is published
- Zero manual steps required for demo deployment
- Deployment status visible in GitHub Actions
- Rollback capability if deployment fails
- Notifications on successful/failed deployments

---

## User Stories

### Automated Deployment

#### US-037-001: Trigger Deployment on Release
**As a** developer  
**I want** the demo site to automatically update when I push a version tag  
**So that** the demo always reflects the latest release without manual work

**Acceptance Criteria:**
- [ ] Deployment triggers after Docker image is successfully published
- [ ] Demo server pulls the new image version
- [ ] Application restarts with the new version
- [ ] Health check confirms successful deployment

#### US-037-002: Deployment Status Visibility
**As a** developer  
**I want** to see deployment status in GitHub Actions  
**So that** I know if the demo update succeeded or failed

**Acceptance Criteria:**
- [ ] GitHub Actions shows deployment job status
- [ ] Logs indicate which version was deployed
- [ ] Failed deployments are clearly marked
- [ ] Deployment duration is visible

#### US-037-003: Secure Remote Deployment
**As a** system administrator  
**I want** deployments to use secure authentication  
**So that** only authorized CI/CD pipelines can trigger deployments

**Acceptance Criteria:**
- [ ] SSH key authentication for remote commands
- [ ] Secrets stored securely in GitHub Actions
- [ ] No credentials exposed in logs
- [ ] Access limited to deployment actions only

### Rollback & Recovery

#### US-037-004: Automatic Health Check
**As a** developer  
**I want** automatic health verification after deployment  
**So that** I know immediately if the new version is working

**Acceptance Criteria:**
- [ ] Health endpoint checked after deployment
- [ ] Deployment marked as failed if health check fails
- [ ] Clear error message if application doesn't start

---

## Technical Design

### Deployment Strategy Options

#### Option A: SSH Remote Execution (Recommended)

GitHub Actions SSHs to the demo server and executes docker compose commands.

**Pros:**
- Simple, direct control
- Uses existing docker-compose.pi.yml
- Full visibility into deployment
- Easy to debug

**Cons:**
- Requires SSH access from GitHub Actions
- Server must be reachable from GitHub's IP ranges

#### Option B: Webhook Trigger

Demo server runs a webhook listener that triggers deployment when called.

**Pros:**
- Server initiates pull (outbound only)
- Works behind firewalls

**Cons:**
- Requires additional webhook service
- More complex setup
- Need to secure webhook endpoint

#### Option C: Watchtower

Watchtower container monitors for new images and auto-updates.

**Pros:**
- No CI/CD changes needed
- Runs on the server

**Cons:**
- Less control over timing
- No CI visibility
- Harder to coordinate with health checks

### Recommended Approach: SSH Remote Execution

```yaml
# New job in docker-build-publish.yml or separate workflow
deploy-demo:
  needs: build-and-push
  if: startsWith(github.ref, 'refs/tags/v')
  runs-on: ubuntu-latest
  environment: demo
  steps:
    - name: Deploy to Demo Server
      uses: appleboy/ssh-action@v1
      with:
        host: ${{ secrets.DEMO_HOST }}
        username: ${{ secrets.DEMO_USER }}
        key: ${{ secrets.DEMO_SSH_KEY }}
        script: |
          cd /path/to/budgetexperiment
          docker compose -f docker-compose.pi.yml pull
          docker compose -f docker-compose.pi.yml up -d
          
    - name: Verify Deployment
      run: |
        sleep 30
        curl -f https://budgetdemo.becauseimclever.com/health || exit 1
```

### GitHub Secrets Required

| Secret | Description |
|--------|-------------|
| `DEMO_HOST` | Demo server hostname/IP |
| `DEMO_USER` | SSH username for deployment |
| `DEMO_SSH_KEY` | Private SSH key for authentication |

### GitHub Environment

Create a `demo` environment in GitHub repository settings:
- Protection rules (optional): require approval for deployments
- Environment secrets: `DEMO_HOST`, `DEMO_USER`, `DEMO_SSH_KEY`
- Deployment URL: `https://budgetdemo.becauseimclever.com`

### Demo Server Setup

```bash
# On demo server: Create deployment user with limited permissions
sudo useradd -m -s /bin/bash deploy
sudo usermod -aG docker deploy

# Create .ssh directory
sudo mkdir -p /home/deploy/.ssh
sudo chmod 700 /home/deploy/.ssh

# Add public key (from GitHub Actions)
echo "ssh-ed25519 AAAA... github-actions-deploy" | sudo tee /home/deploy/.ssh/authorized_keys
sudo chmod 600 /home/deploy/.ssh/authorized_keys
sudo chown -R deploy:deploy /home/deploy/.ssh

# Create deployment directory
sudo mkdir -p /opt/budgetexperiment
sudo chown deploy:deploy /opt/budgetexperiment

# Copy docker-compose file and .env
cp docker-compose.pi.yml /opt/budgetexperiment/
cp .env /opt/budgetexperiment/
```

### Workflow Enhancement

```yaml
# .github/workflows/docker-build-publish.yml (enhanced)
name: Build, Publish and Deploy

on:
  push:
    branches: [main]
    tags: ['v*']
  pull_request:
    branches: [main]

# ... existing build-and-push job ...

  deploy-demo:
    name: Deploy to Demo
    needs: build-and-push
    if: startsWith(github.ref, 'refs/tags/v')
    runs-on: ubuntu-latest
    environment:
      name: demo
      url: https://budgetdemo.becauseimclever.com
    
    steps:
      - name: Deploy via SSH
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ secrets.DEMO_HOST }}
          username: ${{ secrets.DEMO_USER }}
          key: ${{ secrets.DEMO_SSH_KEY }}
          script: |
            cd /opt/budgetexperiment
            echo "Pulling latest image..."
            docker compose -f docker-compose.pi.yml pull
            echo "Restarting services..."
            docker compose -f docker-compose.pi.yml up -d
            echo "Waiting for health check..."
            sleep 10
            docker compose -f docker-compose.pi.yml ps
      
      - name: Verify Health
        run: |
          echo "Waiting for application to stabilize..."
          sleep 20
          echo "Checking health endpoint..."
          curl -sf https://budgetdemo.becauseimclever.com/health || exit 1
          echo "Deployment verified successfully!"
      
      - name: Get Deployed Version
        run: |
          VERSION=$(curl -s https://budgetdemo.becauseimclever.com/api/v1/version 2>/dev/null || echo "unknown")
          echo "Deployed version: $VERSION"
```

---

## Implementation Plan

### Phase 1: Demo Server Preparation

**Objective:** Prepare the demo server for automated deployments

**Tasks:**
- [ ] Create dedicated `deploy` user on demo server
- [ ] Generate SSH key pair for GitHub Actions
- [ ] Configure SSH authorized_keys on server
- [ ] Set up deployment directory structure
- [ ] Test manual SSH access

**Commit:**
```
docs(deploy): document demo server setup for automation

- Add deployment user creation steps
- Document SSH key configuration
- Specify directory structure

Refs: #037
```

### Phase 2: GitHub Environment Setup

**Objective:** Configure GitHub repository for secure deployments

**Tasks:**
- [ ] Create `demo` environment in repository settings
- [ ] Add `DEMO_HOST` secret
- [ ] Add `DEMO_USER` secret  
- [ ] Add `DEMO_SSH_KEY` secret
- [ ] Configure environment protection rules (optional)

### Phase 3: Workflow Enhancement

**Objective:** Add deployment job to CI/CD workflow

**Tasks:**
- [ ] Add `deploy-demo` job to `docker-build-publish.yml`
- [ ] Configure job to run only on version tags
- [ ] Implement SSH deployment step
- [ ] Add health check verification
- [ ] Test with a patch release

**Commit:**
```
ci(deploy): add automated demo deployment on release

- New deploy-demo job triggered on version tags
- SSH-based deployment to demo server
- Health check verification after deployment
- Uses GitHub environment for secrets

Refs: #037
```

### Phase 4: Notification (Optional)

**Objective:** Add deployment notifications

**Tasks:**
- [ ] Add Slack/Discord notification on successful deployment
- [ ] Add notification on failed deployment
- [ ] Include version and deployment URL in notification

**Commit:**
```
ci(deploy): add deployment notifications

- Notify on successful demo deployment
- Alert on deployment failures
- Include version information

Refs: #037
```

### Phase 5: Documentation

**Objective:** Update deployment documentation

**Tasks:**
- [ ] Update ci-cd-deployment.md with new workflow
- [ ] Document rollback procedures
- [ ] Add troubleshooting section
- [ ] Update architecture diagram

**Commit:**
```
docs(deploy): document automated demo deployment

- Add automated deployment workflow details
- Document rollback procedures
- Add troubleshooting guide

Refs: #037
```

---

## Security Considerations

1. **SSH Key Management**
   - Use Ed25519 keys (more secure, shorter)
   - Store private key only in GitHub Secrets
   - Limit key to specific commands if possible (authorized_keys restrictions)

2. **Deploy User Permissions**
   - User should only have docker group access
   - No sudo privileges
   - Limited to deployment directory

3. **Network Security**
   - Demo server firewall should allow SSH from GitHub Actions IPs
   - Consider IP allowlist if supported
   - Use fail2ban for SSH protection

4. **Secret Rotation**
   - Plan for periodic SSH key rotation
   - Document key rotation procedure

---

## Rollback Procedure

If a deployment causes issues:

```bash
# SSH to demo server
ssh deploy@budgetdemo.becauseimclever.com

# View available image versions
docker images ghcr.io/becauseimclever/budgetexperiment

# Update docker-compose to use previous version
cd /opt/budgetexperiment
# Edit docker-compose.pi.yml to specify previous tag
# e.g., image: ghcr.io/becauseimclever/budgetexperiment:3.8.2

docker compose -f docker-compose.pi.yml up -d
```

---

## Monitoring & Observability

- GitHub Actions provides deployment logs
- Health endpoint confirms application is running
- Consider adding version endpoint: `GET /api/v1/version`
- Future: Add deployment metrics/history

---

## Future Enhancements

- **Blue-Green Deployment:** Run new version alongside old before cutover
- **Canary Releases:** Gradual rollout with traffic splitting
- **Automated Rollback:** If health check fails, automatically revert
- **Multiple Environments:** Extend to staging, production
- **Deployment Approval:** Require manual approval for production

---

## References

- [GitHub Actions SSH Deploy](https://github.com/appleboy/ssh-action)
- [GitHub Environments](https://docs.github.com/en/actions/deployment/targeting-different-environments)
- [Docker Compose in Production](https://docs.docker.com/compose/production/)
- [docs/ci-cd-deployment.md](ci-cd-deployment.md) - Current CI/CD documentation
