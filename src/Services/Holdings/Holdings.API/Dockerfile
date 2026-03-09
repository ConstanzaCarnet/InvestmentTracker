# 1. Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# 2. Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos respetando la estructura de carpetas real para que el .sln y las referencias no se rompan
COPY ["src/Services/Holdings/Holdings.API/Holdings.API.csproj", "src/Services/Holdings/Holdings.API/"]
COPY ["src/Services/Holdings/Holdings.Infrastructure/Holdings.Infrastructure.csproj", "src/Services/Holdings/Holdings.Infrastructure/"]
COPY ["src/Services/Holdings/Holdings.Domain/Holdings.Domain.csproj", "src/Services/Holdings/Holdings.Domain/"]
COPY ["src/Services/Holdings/Holdings.Application/Holdings.Application.csproj", "src/Services/Holdings/Holdings.Application/"]
COPY ["src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj", "src/BuildingBlocks/EventBus.Messages/"]

# Ahora el restore busca en la misma ruta donde copiamos
RUN dotnet restore "src/Services/Holdings/Holdings.API/Holdings.API.csproj"

# Copiamos todo el resto del código
COPY src src

# Cambiamos al directorio de la API para buildear
WORKDIR "src/Services/Holdings/Holdings.API"
RUN dotnet build "Holdings.API.csproj" -c Release -o /app/build

# 3. Publish
FROM build AS publish
RUN dotnet publish "Holdings.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Holdings.API.dll"]