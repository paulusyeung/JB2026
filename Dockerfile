FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first to maximize restore-layer cache hits.
COPY JB2026.sln ./
COPY JB2026.Api/JB2026.Api.csproj JB2026.Api/
COPY JB2026.Infrastructure/JB2026.Infrastructure.csproj JB2026.Infrastructure/
COPY JB2026.EfCore/JB2026.EfCore.csproj JB2026.EfCore/
COPY JB2026.DataAccess/JB2026.DataAccess.csproj JB2026.DataAccess/

RUN dotnet restore JB2026.Api/JB2026.Api.csproj

COPY JB2026.Api/ JB2026.Api/
COPY JB2026.Infrastructure/ JB2026.Infrastructure/
COPY JB2026.EfCore/ JB2026.EfCore/
COPY JB2026.DataAccess/ JB2026.DataAccess/

RUN dotnet publish JB2026.Api/JB2026.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://127.0.0.1:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "JB2026.Api.dll"]
