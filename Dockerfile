FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src/backend

COPY backend/ ./
RUN dotnet publish src/ISS.Api/ISS.Api.csproj -c Release -o /app/backend-publish

FROM node:22-bookworm-slim AS frontend-build
WORKDIR /src/frontend

COPY frontend/package*.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    Database__InitializationMode=Migrate \
    ISS_API_BASE_URL=http://127.0.0.1:8080 \
    NODE_ENV=production \
    PORT=3000

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

COPY --from=backend-build /app/backend-publish ./backend
COPY --from=frontend-build /src/frontend/.next ./frontend/.next
COPY --from=frontend-build /src/frontend/public ./frontend/public
COPY --from=frontend-build /src/frontend/package.json ./frontend/package.json
COPY --from=frontend-build /src/frontend/package-lock.json ./frontend/package-lock.json
COPY --from=frontend-build /src/frontend/node_modules ./frontend/node_modules
COPY deploy/railway/start.sh /app/start.sh

RUN sed -i 's/\r$//' /app/start.sh \
    && chmod +x /app/start.sh \
    && mkdir -p /app/backend/App_Data

EXPOSE 3000

CMD ["/app/start.sh"]
