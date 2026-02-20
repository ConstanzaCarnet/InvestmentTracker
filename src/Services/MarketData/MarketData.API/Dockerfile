# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar csproj (para cache de restore)
COPY src/Services/MarketData/MarketData.API/MarketData.API.csproj ./Services/MarketData/MarketData.API/
COPY src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj ./BuildingBlocks/EventBus.Messages/

# RESTORE (AHORA LA RUTA ES LA DEL CONTENEDOR)
RUN dotnet restore ./Services/MarketData/MarketData.API/MarketData.API.csproj

# Copiar todo el repo
COPY . .

# Ir al proyecto real
WORKDIR /src/src/Services/MarketData/MarketData.API

# Build
RUN dotnet build MarketData.API.csproj -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish MarketData.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MarketData.API.dll"]
