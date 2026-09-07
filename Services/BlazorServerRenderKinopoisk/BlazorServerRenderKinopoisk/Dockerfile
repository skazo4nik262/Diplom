FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["BlazorServerRenderKinopoisk.csproj", "./"]
RUN dotnet restore "./BlazorServerRenderKinopoisk.csproj"

COPY . .
RUN dotnet publish "./BlazorServerRenderKinopoisk.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5044
ENTRYPOINT ["dotnet", "BlazorServerRenderKinopoisk.dll"]
