# 1. La imagen base que SÍ tiene el motor de .NET 10 para correr la app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

# 2. La imagen del SDK para compilar el código
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiamos los archivos de proyecto (Incluyendo el Shared kernel)
COPY ["BomProduction.QueryService/BomProduction.QueryService.csproj", "BomProduction.QueryService/"]
COPY ["BomProduction.Shared/BomProduction.Shared.csproj", "BomProduction.Shared/"]
RUN dotnet restore "./BomProduction.QueryService/BomProduction.QueryService.csproj"

# Copiamos todo el código y compilamos
COPY . .
WORKDIR "/src/BomProduction.QueryService"
RUN dotnet build "./BomProduction.QueryService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3. Publicamos los binarios
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BomProduction.QueryService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. Ensamblamos la imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BomProduction.QueryService.dll"]