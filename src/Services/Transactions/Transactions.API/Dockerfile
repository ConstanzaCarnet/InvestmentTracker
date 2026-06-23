# syntax=docker/dockerfile:1

# ===== STAGE 1 — build (SDK) =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /workspace

# (1) Solo .csproj (propios + BuildingBlocks que referencia) -> restore cacheable.
COPY src/Services/Transactions/Transactions.API/Transactions.API.csproj                       src/Services/Transactions/Transactions.API/
COPY src/Services/Transactions/Transactions.Application/Transactions.Application.csproj        src/Services/Transactions/Transactions.Application/
COPY src/Services/Transactions/Transactions.Domain/Transactions.Domain.csproj                 src/Services/Transactions/Transactions.Domain/
COPY src/Services/Transactions/Transactions.Infrastructure/Transactions.Infrastructure.csproj src/Services/Transactions/Transactions.Infrastructure/
COPY src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj                             src/BuildingBlocks/EventBus.Messages/
COPY src/BuildingBlocks/Common.Authentication/Common.Authentication.csproj                     src/BuildingBlocks/Common.Authentication/
RUN dotnet restore src/Services/Transactions/Transactions.API/Transactions.API.csproj

# (2) Código + publish.
COPY src/Services/Transactions/ src/Services/Transactions/
COPY src/BuildingBlocks/ src/BuildingBlocks/
RUN dotnet publish src/Services/Transactions/Transactions.API/Transactions.API.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ===== STAGE 2 — final (runtime) =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Transactions.API.dll"]
