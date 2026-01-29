FROM node:20-alpine AS development
WORKDIR /app

# Dependencies installieren
COPY package.json package-lock.json* ./
RUN npm install

# Source Code wird per Volume gemountet (docker-compose.dev.yml)
# Vite Dev Server auf allen Interfaces starten
EXPOSE 8100
CMD ["npx", "vite", "--host", "0.0.0.0", "--port", "8100"]
