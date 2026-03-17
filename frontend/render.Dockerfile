FROM node:18-alpine

WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY tsconfig.json next.config.js tailwind.config.ts postcss.config.js ./
COPY src ./src

RUN npm run build

EXPOSE 3000
ENV NODE_ENV=production
ENV PORT=3000
ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL:-https://bookinghubapi.onrender.com}

CMD ["npm", "run", "start"]