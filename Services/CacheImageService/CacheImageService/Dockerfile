# --- Stage 1: Build & Publish ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["CacheImageService.csproj", "./"]
RUN dotnet restore "./CacheImageService.csproj"

COPY . .
RUN dotnet publish "./CacheImageService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5007
ENTRYPOINT ["dotnet", "CacheImageService.dll"]
