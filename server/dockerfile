FROM node:20-alpine AS base
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable
WORKDIR /api
RUN pnpm install -g @nestjs/cli


#####################

FROM base AS prod-deps

COPY --chown=node:node package.json pnpm-lock.yaml ./

RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --prod --frozen-lockfile

RUN pnpm install prisma

COPY --chown=node:node /prisma ./prisma/

RUN pnpm exec prisma generate

# USER node

#####################

FROM base AS build

COPY --chown=node:node tsconfig.json package.json pnpm-lock.yaml nest-cli.json ./

COPY --chown=node:node --from=prod-deps /api/node_modules ./node_modules

RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile

# COPY --chown=node:node --from=prod-deps /api/node_modules ./node_modules

COPY --chown=node:node /src ./src

COPY --chown=node:node /prisma ./prisma

RUN pnpm run build

# USER node

#####################

FROM base AS production

ENV NODE_ENV production
COPY --chown=node:node --from=prod-deps /api/node_modules ./node_modules
COPY --chown=node:node --from=prod-deps /api/prisma ./prisma
COPY --chown=node:node --from=build /api/dist ./dist
COPY --chown=node:node --from=build /api/package.json ./

EXPOSE 3000

CMD ["/bin/sh", "-c", "pnpm exec prisma migrate deploy;node dist/main.js"]