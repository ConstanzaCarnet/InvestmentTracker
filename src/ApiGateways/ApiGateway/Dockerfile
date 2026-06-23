# syntax=docker/dockerfile:1

# ===== STAGE 1 — build (SDK) =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /workspace

# (1) Solo .csproj (gateway + Common.Authentication) -> restore cacheable.
COPY src/ApiGateways/ApiGateway/ApiGateway.csproj                          src/ApiGateways/ApiGateway/
COPY src/BuildingBlocks/Common.Authentication/Common.Authentication.csproj src/BuildingBlocks/Common.Authentication/
RUN dotnet restore src/ApiGateways/ApiGateway/ApiGateway.csproj

# (2) Código + publish.
COPY src/ApiGateways/ApiGateway/ src/ApiGateways/ApiGateway/
COPY src/BuildingBlocks/ src/BuildingBlocks/
RUN dotnet publish src/ApiGateways/ApiGateway/ApiGateway.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ===== STAGE 2 — final (runtime) =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
