FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Install missing dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libstdc++6 \
    libc6

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App

# Copy the built app from the build stage
COPY --from=build /app/out ./

# Ensure the gamma executable has the correct permissions
RUN chmod +x ./gamma

# Set the entry point for the application
ENTRYPOINT ["./gamma"]

