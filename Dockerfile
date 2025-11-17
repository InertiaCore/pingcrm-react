# Stage 1: Build frontend assets with Node.js
FROM node:24-alpine AS frontend-build

WORKDIR /src/ClientApp

# Copy package files and install dependencies
COPY ClientApp/package*.json ./
RUN npm ci

# Copy frontend source and build
COPY ClientApp/ ./
RUN npm run build

# Stage 2: Build .NET application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build

WORKDIR /src

# Copy project file and restore dependencies
COPY PingCRM.csproj .
RUN dotnet restore

# Copy the rest of the application (excluding ClientApp)
COPY . .
RUN rm -rf ClientApp

# Copy built frontend assets from previous stage
COPY --from=frontend-build /src/wwwroot/build ./wwwroot/build

# Build and publish the .NET application with Docker build flags
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false /p:CI=true /p:DOCKER_BUILD=true

# Use the official .NET 8 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Copy the published application
COPY --from=backend-build /app/publish .

# Create data and uploads directories
RUN mkdir -p data wwwroot/uploads/users

# Expose port 80
EXPOSE 80
EXPOSE 443

# Set environment variable for production
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "PingCRM.dll"]
