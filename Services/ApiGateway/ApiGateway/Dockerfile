# --- Stage 1: Build & Publish ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Копируем .csproj и делаем restore (кешируется)
COPY ["ApiGateway.csproj", "./"]
RUN dotnet restore "./ApiGateway.csproj"

# 2. Копируем весь исходный код
COPY . .

# 3. Сразу публикуем в /app/publish
# /p:UseAppHost=false — критически важно: не создавать native exe
RUN dotnet publish "./ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Указываем порт и точку входа
EXPOSE 5000
ENTRYPOINT ["dotnet", "ApiGateway.dll"]