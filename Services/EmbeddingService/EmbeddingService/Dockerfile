# --- Stage 1: Build & Publish ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["EmbeddingService.csproj", "./"]
RUN dotnet restore "./EmbeddingService.csproj"

COPY . .
RUN dotnet publish "./EmbeddingService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5004
ENTRYPOINT ["dotnet", "EmbeddingService.dll"]
