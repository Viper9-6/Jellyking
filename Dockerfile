# ============================================================
# Stage 1 — Build the React frontend
# ============================================================
FROM node:20-alpine AS frontend-build

WORKDIR /app/frontend

COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci

COPY frontend/ ./

# Build outputs to ../src/Jellyking.Host/wwwroot (see vite.config.ts)
COPY src/Jellyking.Host/wwwroot/ ../src/Jellyking.Host/wwwroot/
RUN npm run build

# ============================================================
# Stage 2 — Build the .NET application
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend-build

WORKDIR /app

COPY Jellyking.sln ./
COPY src/Jellyking.Core/Jellyking.Core.csproj     src/Jellyking.Core/
COPY src/Jellyking.Host/Jellyking.Host.csproj     src/Jellyking.Host/

# Restore packages before copying source so this layer is cached.
RUN dotnet restore

COPY src/ src/

# Copy the compiled frontend from Stage 1.
COPY --from=frontend-build /app/src/Jellyking.Host/wwwroot src/Jellyking.Host/wwwroot

RUN dotnet publish src/Jellyking.Host/Jellyking.Host.csproj \
    --configuration Release \
    --no-restore \
    --output /publish

# ============================================================
# Stage 3 — Runtime image (small Alpine)
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Install icu for globalization support (needed by .NET on Alpine).
RUN apk add --no-cache icu-libs

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_ENVIRONMENT=Production \
    DataDirectory=/data

WORKDIR /app

COPY --from=backend-build /publish .

# Data directory: users, services, settings, encrypted credentials,
# DataProtection keys, and the self-signed TLS cert. Persist this!
VOLUME ["/data"]

EXPOSE 5656

ENTRYPOINT ["./Jellyking"]
