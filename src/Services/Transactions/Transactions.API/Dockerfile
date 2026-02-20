# 1. Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# 2. Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Services/Transactions/Transactions.API/Transactions.API.csproj", "Services/Transactions/Transactions.API/"]
COPY ["src/Services/Transactions/Transactions.Infrastructure/Transactions.Infrastructure.csproj", "Services/Transactions/Transactions.Infrastructure/"]
COPY ["src/Services/Transactions/Transactions.Domain/Transactions.Domain.csproj", "Services/Transactions/Transactions.Domain/"]
COPY ["src/Services/Transactions/Transactions.Application/Transactions.Application.csproj", "Services/Transactions/Transactions.Application/"]
# IMPORTANTE: EventBus.Messages.
COPY ["src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj", "BuildingBlocks/EventBus.Messages/"]

RUN dotnet restore "Services/Transactions/Transactions.API/Transactions.API.csproj"

COPY . .

# Build
WORKDIR "/src/src/Services/Transactions/Transactions.API"
RUN dotnet build "Transactions.API.csproj" -c Release -o /app/build

# 3. Publish
FROM build AS publish
RUN dotnet publish "Transactions.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Transactions.API.dll"]