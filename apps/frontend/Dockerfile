FROM node:20-alpine AS base
WORKDIR /app

# Development image
FROM base AS development
COPY package*.json ./
RUN npm install
COPY . .
EXPOSE 8100
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]

# Build image
FROM base AS build
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Production image with nginx
FROM nginx:alpine AS production
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
