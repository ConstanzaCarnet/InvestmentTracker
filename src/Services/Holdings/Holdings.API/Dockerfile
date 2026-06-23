# syntax=docker/dockerfile:1

# ===== STAGE 1 — build (SDK) =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /workspace

# (1) Solo .csproj (propios + BuildingBlocks que referencia) -> restore cacheable.
COPY src/Services/Holdings/Holdings.API/Holdings.API.csproj                       src/Services/Holdings/Holdings.API/
COPY src/Services/Holdings/Holdings.Application/Holdings.Application.csproj        src/Services/Holdings/Holdings.Application/
COPY src/Services/Holdings/Holdings.Domain/Holdings.Domain.csproj                 src/Services/Holdings/Holdings.Domain/
COPY src/Services/Holdings/Holdings.Infrastructure/Holdings.Infrastructure.csproj src/Services/Holdings/Holdings.Infrastructure/
COPY src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj                src/BuildingBlocks/EventBus.Messages/
COPY src/BuildingBlocks/Common.Authentication/Common.Authentication.csproj        src/BuildingBlocks/Common.Authentication/
RUN dotnet restore src/Services/Holdings/Holdings.API/Holdings.API.csproj

# (2) Código + publish.
COPY src/Services/Holdings/ src/Services/Holdings/
COPY src/BuildingBlocks/ src/BuildingBlocks/
RUN dotnet publish src/Services/Holdings/Holdings.API/Holdings.API.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ===== STAGE 2 — final (runtime) =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Holdings.API.dll"]
