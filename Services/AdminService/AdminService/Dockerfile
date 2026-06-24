FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["AdminService.csproj", "./"]
RUN dotnet restore "./AdminService.csproj"

COPY . .
RUN dotnet publish "./AdminService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5009
ENTRYPOINT ["dotnet", "AdminService.dll"]
