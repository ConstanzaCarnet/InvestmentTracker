# syntax=docker/dockerfile:1

# ============================================================
# STAGE 1 — build: usa el SDK (pesado) solo para compilar.
# El build context es la raíz del repo (ver docker-compose: context: .).
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /workspace

# (1) Copiar SOLO los .csproj y restaurar. Esta capa se cachea: mientras no
#     cambien las dependencias, Docker la reutiliza y no rebaja NuGet.
COPY src/Services/MarketData/MarketData.API/MarketData.API.csproj                       src/Services/MarketData/MarketData.API/
COPY src/Services/MarketData/MarketData.Application/MarketData.Application.csproj        src/Services/MarketData/MarketData.Application/
COPY src/Services/MarketData/MarketData.Domain/MarketData.Domain.csproj                 src/Services/MarketData/MarketData.Domain/
COPY src/Services/MarketData/MarketData.Infrastructure/MarketData.Infrastructure.csproj src/Services/MarketData/MarketData.Infrastructure/
RUN dotnet restore src/Services/MarketData/MarketData.API/MarketData.API.csproj

# (2) Recién ahora copiar el código y publicar. Un cambio en un .cs invalida
#     desde acá, pero NO el restore de arriba.
COPY src/Services/MarketData/ src/Services/MarketData/
RUN dotnet publish src/Services/MarketData/MarketData.API/MarketData.API.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ============================================================
# STAGE 2 — final: imagen runtime (liviana, sin SDK, sin código fuente).
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MarketData.API.dll"]
