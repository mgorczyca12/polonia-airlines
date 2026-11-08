FROM node:26-alpine AS build
WORKDIR /app
RUN npm install --global pnpm@12.4.2
COPY frontend/polonia-web/package.json frontend/polonia-web/pnpm-lock.yaml frontend/polonia-web/pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile
COPY frontend/polonia-web/ ./
RUN pnpm build

FROM build AS production-dependencies
RUN pnpm prune --prod

FROM node:26-alpine AS runtime
ENV NODE_ENV=production
ENV HOST=0.0.0.0
ENV PORT=3000
WORKDIR /app
COPY --from=production-dependencies --chown=node:node /app/node_modules ./node_modules
COPY --from=build --chown=node:node /app/dist ./dist
COPY --from=build --chown=node:node /app/package.json ./package.json
USER node
EXPOSE 3000
CMD ["node", "node_modules/srvx/bin/srvx.mjs", "--prod", "-s", "../client", "dist/server/server.js"]
