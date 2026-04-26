# ---- Build stage --------------------------------------------------------
FROM node:22-alpine AS build

WORKDIR /app/ClientApp

# Copy package manifests first — maximises Docker layer cache hits.
# The lock file and .npmrc are stable; source files change far more often.
COPY package.json pnpm-lock.yaml .npmrc ./

# Enable pnpm via Corepack and install all dependencies from the lockfile.
# --frozen-lockfile ensures reproducible builds and fails fast on drift.
RUN corepack enable pnpm && pnpm install --frozen-lockfile

# Copy the rest of the source tree and build the production bundle.
# vite.config.ts sets outDir: '../wwwroot/app', so the built assets land
# at /app/wwwroot/app inside the container.
COPY . .
RUN pnpm run build

# ---- Runtime stage -------------------------------------------------------
FROM nginx:alpine AS runtime

# Replace the default nginx server block with one that handles the /app/
# base path and falls back to index.html for client-side routing.
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy the built static assets from the build stage.
COPY --from=build /app/wwwroot/app /usr/share/nginx/html/app

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
