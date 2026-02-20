# 1. Imagen base para correr la app
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# .NET 9 usa el 8080 por defecto
EXPOSE 8080 
ENV ASPNETCORE_URLS=http://+:8080

# 2. Imagen para compilar (SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Copiamos los archivos de proyecto para restaurar dependencias
COPY ["src/Services/Users/Users.API/Users.API.csproj", "Services/Users/Users.API/"]
COPY ["src/Services/Users/Users.Domain/Users.Domain.csproj", "Services/Users/Users.Domain/"]
COPY ["src/Services/Users/Users.Application/Users.Application.csproj", "Services/Users/Users.Application/"]
COPY ["src/Services/Users/Users.Infrastructure/Users.Infrastructure.csproj", "Services/Users/Users.Infrastructure/"]

COPY ["src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj", "BuildingBlocks/EventBus.Messages/"]
# Restauramos las dependencias desde API ya que tiene referencias a los otros proyectos
RUN dotnet restore "Services/Users/Users.API/Users.API.csproj"
COPY . .
WORKDIR "src/Services/Users/Users.API"
RUN dotnet build "Users.API.csproj" -c Release -o /app/build

# 3. Publicación
FROM build AS publish
RUN dotnet publish "Users.API.csproj" -c Release -o /app/publish

# 4. Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Users.API.dll"]