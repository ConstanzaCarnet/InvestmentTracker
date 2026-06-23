# syntax=docker/dockerfile:1

# ===== STAGE 1 — build (SDK) =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /workspace

# (1) Solo .csproj (propios + BuildingBlocks que referencia) -> restore cacheable.
COPY src/Services/Users/Users.API/Users.API.csproj                       src/Services/Users/Users.API/
COPY src/Services/Users/Users.Application/Users.Application.csproj        src/Services/Users/Users.Application/
COPY src/Services/Users/Users.Domain/Users.Domain.csproj                 src/Services/Users/Users.Domain/
COPY src/Services/Users/Users.Infrastructure/Users.Infrastructure.csproj src/Services/Users/Users.Infrastructure/
COPY src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj       src/BuildingBlocks/EventBus.Messages/
RUN dotnet restore src/Services/Users/Users.API/Users.API.csproj

# (2) Código + publish.
COPY src/Services/Users/ src/Services/Users/
COPY src/BuildingBlocks/ src/BuildingBlocks/
RUN dotnet publish src/Services/Users/Users.API/Users.API.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ===== STAGE 2 — final (runtime) =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Users.API.dll"]
