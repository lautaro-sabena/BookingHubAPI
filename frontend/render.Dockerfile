FROM node:18-alpine

ARG NEXT_PUBLIC_API_URL=https://bookinghubapi.onrender.com

WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL}

COPY tsconfig.json next.config.js tailwind.config.ts postcss.config.js ./
COPY src ./src

RUN npm run build

EXPOSE 3000
ENV NODE_ENV=production
ENV PORT=3000

CMD ["npm", "run", "start"]