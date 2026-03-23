# Quick Start - Deployment Guide

## Demo Mode (Easiest — No Setup Required)

Get BudgetExperiment running in under a minute with bundled PostgreSQL and no authentication:

```bash
# Clone the repository (or download docker-compose.demo.yml)
git clone https://github.com/becauseimclever/BudgetExperiment.git
cd BudgetExperiment

# Start everything
docker compose -f docker-compose.demo.yml up -d
```

Open [http://localhost:5099](http://localhost:5099). That's it!

### What's included

- **PostgreSQL 18** — bundled and pre-configured, no external database needed
- **Authentication disabled** — no identity provider setup required
- **Persistent data** — stored in a Docker volume that survives restarts

### Managing demo data

```bash
# Stop (keeps data)
docker compose -f docker-compose.demo.yml down

# Stop and delete all data
docker compose -f docker-compose.demo.yml down -v

# Update to latest version
docker compose -f docker-compose.demo.yml pull
docker compose -f docker-compose.demo.yml up -d
```

### Upgrading to production

When you're ready for production use:

1. **Enable authentication** — set `Authentication__Mode=OIDC` and configure a provider. See [docs/AUTH-PROVIDERS.md](docs/AUTH-PROVIDERS.md).
2. **Use an external database** — point `ConnectionStrings__AppDb` to a managed PostgreSQL instance for backups and reliability.
3. **Switch to `docker-compose.pi.yml`** — the production compose file for Raspberry Pi or server deployment (see below).

---

## Raspberry Pi Deployment (Production)# Windows to Raspberry Pi Deployment - Quick Start Guide



This guide will get your BudgetExperiment application running on a Raspberry Pi in under 10 minutes using pre-built Docker images from GitHub Container Registry.This guide will help you deploy your BudgetExperiment application from your Windows machine to your Raspberry Pi.



## Prerequisites Checklist## Prerequisites Setup



- [ ] Raspberry Pi (3B+ or newer) with 64-bit OS### 1. On Your Windows Machine

- [ ] Docker installed on the Pi

- [ ] PostgreSQL database accessible from the PiEnsure you have:

- [ ] GitHub account with access to this repository- ✅ Docker Desktop installed and running

- [ ] Personal Access Token with `read:packages` permission- ✅ PowerShell (comes with Windows)

- ✅ SSH client (built into Windows 10/11)

## Step-by-Step Deployment

To verify Docker buildx support:

### 1. Install Docker (if not already installed)```powershell

docker buildx version

SSH to your Raspberry Pi and run:```



```bashIf not available, update Docker Desktop to the latest version.

# Quick Docker installation

curl -fsSL https://get.docker.com | sudo sh### 2. On Your Raspberry Pi

sudo usermod -aG docker $USER

sudo apt-get install -y docker-compose-plugin#### Install Docker (if not already installed)

``````bash

# SSH to your Pi

Log out and back in for the changes to take effect.ssh pi@raspberry-pi.local



### 2. Authenticate with GitHub Container Registry# Install Docker

curl -fsSL https://get.docker.com -o get-docker.sh

```bashsudo sh get-docker.sh

# Create token at: https://github.com/settings/tokens (need read:packages scope)

docker login ghcr.io -u YOUR_GITHUB_USERNAME -p YOUR_GITHUB_TOKEN# Add your user to docker group

```sudo usermod -aG docker $USER



### 3. Download Deployment Files# Install Docker Compose

sudo apt-get install -y docker-compose

```bash

mkdir -p ~/BudgetExperiment && cd ~/BudgetExperiment# Reboot to apply changes

sudo reboot

# Download docker-compose file```

wget https://raw.githubusercontent.com/becauseimclever/BudgetExperiment/main/docker-compose.pi.yml

```After reboot, verify:

```bash

### 4. Create Environment Filedocker --version

docker-compose --version

```bash```

# Create .env file with your database connection

cat > .env << 'EOF'#### Create Database Configuration

DB_CONNECTION_STRING=Host=YOUR_DB_HOST;Port=5432;Database=budgetexperiment;Username=YOUR_USER;Password=YOUR_PASSWORD```bash

EOF# SSH to your Pi again after reboot

ssh pi@raspberry-pi.local

# Secure the file

chmod 600 .env# Create deployment directory

```mkdir -p ~/BudgetExperiment

cd ~/BudgetExperiment

**⚠️ IMPORTANT**: Replace `YOUR_DB_HOST`, `YOUR_USER`, and `YOUR_PASSWORD` with your actual database details!

# Create .env file

### 5. Deploy the Applicationnano .env

```

```bash

# Pull and start the applicationAdd this content (replace with your actual database details):

docker compose -f docker-compose.pi.yml up -d```env

DB_CONNECTION_STRING=Host=192.168.1.100;Port=5432;Database=budgetexperiment;Username=budgetuser;Password=YourSecurePassword123!

# View logs (Ctrl+C to exit)```

docker compose -f docker-compose.pi.yml logs -f

```Save with `Ctrl+O`, `Enter`, then exit with `Ctrl+X`.



### 6. Verify It's Working### 3. Set Up SSH Key Authentication (Recommended)



```bashThis allows passwordless deployment:

# Test health endpoint

curl http://localhost:5099/health```powershell

# On Windows, generate SSH key (if you don't have one)

# Should return: {"status":"Healthy"}ssh-keygen -t rsa -b 4096

```

# Copy key to Raspberry Pi (enter your Pi password when prompted)

### 7. Access the Applicationtype $env:USERPROFILE\.ssh\id_rsa.pub | ssh pi@raspberry-pi.local "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"



Open a browser and navigate to:# Test passwordless login

- **Application**: `http://YOUR_RASPBERRY_PI_IP:5099`ssh pi@raspberry-pi.local

- **API Docs**: `http://YOUR_RASPBERRY_PI_IP:5099/scalar````



🎉 **You're done!** The application is now running.## Deployment



## Common Commands

```bash
# View logs
docker compose -f docker-compose.pi.yml logs -f

# Stop application
docker compose -f docker-compose.pi.yml stop

# Start application
docker compose -f docker-compose.pi.yml start

# Update to latest image and redeploy
docker compose -f docker-compose.pi.yml pull
docker compose -f docker-compose.pi.yml up -d



# Check status### Option 2: Step-by-Step Deployment

docker compose -f docker-compose.pi.yml ps

```If you want more control:



## Updating the Application

When new versions are released:

```bash
cd ~/BudgetExperiment
docker compose -f docker-compose.pi.yml pull
docker compose -f docker-compose.pi.yml up -d

## Troubleshooting```



**Can't pull image?**## Verify Deployment

- Verify your GitHub token has `read:packages` permission

- Re-run: `docker login ghcr.io -u YOUR_USERNAME -p YOUR_TOKEN`### Check if the application is running



**Can't connect to database?**From your Windows machine:

- Check your `.env` file has the correct connection string```powershell

- Test database connection: `psql -h YOUR_DB_HOST -U YOUR_USER -d budgetexperiment`# Check health endpoint

curl http://raspberry-pi.local:5099/health

**Application won't start?**

- Check logs: `docker compose -f docker-compose.pi.yml logs`# Or open in browser

- Verify port 5099 is not in use: `sudo netstat -tlnp | grep 5099`start http://raspberry-pi.local:5099

```

For more details, see [README.Docker.md](README.Docker.md).

### View logs on the Pi

## Local Development

```bash

**Important**: Do NOT use Docker for local development!# SSH to your Pi

ssh pi@raspberry-pi.local

For local development on your Windows machine:

# Navigate to deployment directory

```powershellcd ~/BudgetExperiment

# Just run the API (it serves the Blazor client automatically)

dotnet run --project c:\ws\BudgetExpirement\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj# View live logs

```docker-compose logs -f



Docker is only for Raspberry Pi deployments. Use standard .NET tooling for local development.# Press Ctrl+C to exit logs

```

### Check container status

```bash
docker-compose ps
```

## Common Issues and Solutions

### Issue: "buildx: command not found"

**Solution:** Update Docker Desktop to the latest version.

### Issue: "Permission denied" when connecting to Pi

**Solutions:**
1. Verify SSH access: `ssh pi@raspberry-pi.local`
2. Check username is correct (default is usually 'pi')
3. Set up SSH key authentication (see Prerequisites above)

### Issue: "Connection refused" to database

**Solutions:**
1. Verify `.env` file exists on Pi: `ssh pi@raspberry-pi.local cat ~/BudgetExperiment/.env`
2. Check database server is running and accessible from Pi
3. Verify connection string is correct
4. Check PostgreSQL pg_hba.conf allows connections from Pi's IP

### Issue: Application won't start

**Check logs:**
```bash
ssh pi@raspberry-pi.local
cd ~/BudgetExperiment
docker-compose logs
```

Common causes:
- Database connection issues
- Port 5099 already in use
- Insufficient resources (especially on Pi 3)

## Management Commands

All these commands run on the Raspberry Pi (via SSH):

```bash
# View logs
docker-compose logs -f

# Restart application
docker-compose restart

# Stop application
docker-compose stop

# Start application
docker-compose start

# Stop and remove (keeps data)
docker-compose down

# Check container status
docker-compose ps

# Check resource usage
docker stats
```

## Updating the Application

When you make code changes and push to main, the CI/CD pipeline builds new images. On the Pi, pull and restart using the commands above.

## Accessing the Application

Once deployed, access from any device on your network:

- **Web Application:** http://raspberry-pi.local:5099
- **API Documentation:** http://raspberry-pi.local:5099/scalar
- **Health Check:** http://raspberry-pi.local:5099/health
- **OpenAPI Spec:** http://raspberry-pi.local:5099/openapi/v1.json

Replace `raspberry-pi.local` with your Pi's IP address if hostname doesn't work.

## Architecture

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────┐
│ Windows PC      │         │ Raspberry Pi     │         │ PostgreSQL  │
│                 │         │                  │         │ Database    │
│ - Build Docker  │ ─SSH──> │ - Docker Runtime │ ───────>│             │
│ - Run Tests     │  SCP    │ - Application    │  TCP    │             │
│ - Cross-compile │         │   Container      │  5432   │             │
└─────────────────┘         └──────────────────┘         └─────────────┘
```

## Files

- `README.Docker.md` - Detailed Docker documentation
- `DEPLOY-QUICKSTART.md` - This file

## Need Help?

1. Check the logs: `ssh pi@raspberry-pi.local "cd ~/BudgetExperiment && docker-compose logs"`
2. Verify all prerequisites are met
3. Ensure database is accessible from the Pi
4. Check firewall settings on both Pi and database server

## Optional: Observability

Add centralized logging with Seq (free single-user license) — no code changes required:

```bash
docker compose -f docker-compose.pi.yml -f docker-compose.observability.yml up -d
```

- Seq UI at `http://<your-host>:8081`
- For OTLP export to Grafana/Jaeger, set `Observability__Otlp__Endpoint` in your `.env`

See [docs/OBSERVABILITY.md](docs/OBSERVABILITY.md) for full configuration reference.
