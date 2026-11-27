# Production image: the API (ASP.NET Core) also serves the Angular build from wwwroot.
# Build: docker build -t oschterhas-api:local .   (context = repository root, see .dockerignore)
# Base images are pinned; Dependabot (docker) proposes updates.

FROM node:24.22-alpine AS web
WORKDIR /src/apps/web
COPY apps/web/package.json apps/web/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY design-system /src/design-system
COPY apps/web ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS api
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
WORKDIR /src/apps/api
COPY apps/api/global.json apps/api/Directory.Build.props apps/api/Directory.Packages.props ./
COPY apps/api/src ./src
RUN dotnet publish src/Oschterhas.Api -c Release -o /out -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14
WORKDIR /app
COPY --from=api /out ./
COPY --from=web /src/apps/web/dist/web/browser ./wwwroot
# Directory for the data protection keys; a named volume mounted here takes over this owner.
RUN mkdir -p /app/keys && chown "$APP_UID" /app/keys
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0 \
    ReverseProxy__DataProtectionKeysDirectory=/app/keys
# Non-root user of the .NET images (uid 1654).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Oschterhas.Api.dll"]
