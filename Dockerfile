# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["CafeBot.TelegramBot/CafeBot.TelegramBot.csproj", "CafeBot.TelegramBot/"]
COPY ["CafeBot.Core/CafeBot.Core.csproj", "CafeBot.Core/"]
COPY ["CafeBot.Application/CafeBot.Application.csproj", "CafeBot.Application/"]
COPY ["CafeBot.Infrastructure/CafeBot.Infrastructure.csproj", "CafeBot.Infrastructure/"]

RUN dotnet restore "CafeBot.TelegramBot/CafeBot.TelegramBot.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/CafeBot.TelegramBot"
RUN dotnet build "CafeBot.TelegramBot.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CafeBot.TelegramBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Install cultures for localization (if needed)
RUN apt-get update && apt-get install -y \
    locales \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r cafebot && useradd -r -g cafebot cafebot
RUN chown -R cafebot:cafebot /app
USER cafebot

COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD dotnet CafeBot.TelegramBot.dll --health-check || exit 1

ENTRYPOINT ["dotnet", "CafeBot.TelegramBot.dll"]
