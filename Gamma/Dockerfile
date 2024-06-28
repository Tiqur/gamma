FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy everything
COPY . ./

# Install missing dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libstdc++6 \
    libc6

EXPOSE 2205
CMD ["dotnet", "gamma.dll"]
