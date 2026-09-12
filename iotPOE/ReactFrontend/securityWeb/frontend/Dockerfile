# =========================================
# Stage 1: Build the React Application
# =========================================

# Use a lightweight Node.js image for building (customizable via ARG)
FROM node:24-alpine AS builder

# Set the working directory inside the container
WORKDIR /app

# Copy package-related files first to leverage Docker's caching mechanism
COPY SecurityWebapp/securityWeb/frontend/package.json SecurityWebapp/securityWeb/frontend/package-lock.json* ./

# Install project dependencies using npm ci (ensures a clean, reproducible install)
RUN --mount=type=cache,target=/root/.npm npm ci

# Copy the rest of the application source code into the container
COPY SecurityWebapp/securityWeb/frontend/. .

# Build the React.js application (outputs to /app/dist)
RUN npm run build

# =========================================
# Stage 2: Prepare Nginx to Serve Static Files
# =========================================
FROM nginx:1.28.0-alpine AS runner

# Copy custom Nginx config
COPY SecurityWebapp/securityWeb/frontend/nginx.conf /etc/nginx/nginx.conf

# Copy the static build output from the build stage to Nginx's default HTML serving directory
COPY --chown=nginx:nginx --from=builder /app/dist /usr/share/nginx/html

# Use a non-root user for security best practices
USER nginx

# Expose port 8080 to allow HTTP traffic
# Note: The default NGINX container now listens on port 8080 instead of 80
EXPOSE 8080

# Start Nginx directly with custom config
ENTRYPOINT ["nginx", "-c", "/etc/nginx/nginx.conf"]
CMD ["-g", "daemon off;"]

# References
# Dockerdocs, 2026. React.js language-specific guide (Version 10.0) [Source Code] Available at: < https://docs.docker.com/guides/reactjs#containerize-a-reactjs-application > [Accessed 27 August 2026]