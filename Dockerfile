# Multi-stage Dockerfile for BudgetExperiment
# Builds from source and creates optimized runtime image
# Supports multi-architecture builds (amd64, arm64)

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION=0.0.0-docker
WORKDIR /build

# Copy project files for restore
COPY ["Directory.Build.props", "./"]
COPY ["stylecop.json", "./"]
COPY ["src/BudgetExperiment.Domain/BudgetExperiment.Domain.csproj", "src/BudgetExperiment.Domain/"]
COPY ["src/BudgetExperiment.Application/BudgetExperiment.Application.csproj", "src/BudgetExperiment.Application/"]
COPY ["src/BudgetExperiment.Infrastructure/BudgetExperiment.Infrastructure.csproj", "src/BudgetExperiment.Infrastructure/"]
COPY ["src/BudgetExperiment.Api/BudgetExperiment.Api.csproj", "src/BudgetExperiment.Api/"]
COPY ["src/BudgetExperiment.Client/BudgetExperiment.Client.csproj", "src/BudgetExperiment.Client/"]
COPY ["src/BudgetExperiment.Contracts/BudgetExperiment.Contracts.csproj", "src/BudgetExperiment.Contracts/"]

# Restore dependencies
RUN dotnet restore "src/BudgetExperiment.Api/BudgetExperiment.Api.csproj"

# Copy all source code
COPY ["src/", "src/"]

# Publish (combines build and publish in one step to avoid path issues)
# Pass VERSION to override MinVer (which can't read .git in Docker)
RUN dotnet publish "src/BudgetExperiment.Api/BudgetExperiment.Api.csproj" \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:MinVerVersionOverride=${VERSION}

# Runtime stage — chiseled: distroless Ubuntu Noble, non-root (UID 1654) by default,
# no shell/package manager, ~50% smaller than standard images.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Environment variables (can be overridden at runtime)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# No HEALTHCHECK — chiseled images have no shell or curl.
# Health monitoring via docker-compose or external access to /health endpoint.

# Entry point
ENTRYPOINT ["dotnet", "BudgetExperiment.Api.dll"]
