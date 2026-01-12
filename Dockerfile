# Multi-stage Dockerfile for BudgetExperiment
# Builds from source and creates optimized runtime image
# Supports multi-architecture builds (amd64, arm64)

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
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
RUN dotnet publish "src/BudgetExperiment.Api/BudgetExperiment.Api.csproj" \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install necessary dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    ca-certificates \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create a non-root user for security (use UID 1001 to avoid conflicts)
RUN useradd -m -u 1001 appuser && \
    chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8080

# Environment variables (can be overridden at runtime)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "BudgetExperiment.Api.dll"]
