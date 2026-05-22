FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Launch/1-Application/CashFlow.Launch.Api/CashFlow.Launch.Api.csproj", "src/Launch/1-Application/CashFlow.Launch.Api/"]

RUN dotnet restore "./src/Launch/1-Application/CashFlow.Launch.Api/CashFlow.Launch.Api.csproj"

COPY . .

WORKDIR "/src/src/Launch/1-Application/CashFlow.Launch.Api"

RUN dotnet build "./CashFlow.Launch.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "./CashFlow.Launch.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CashFlow.Launch.Api.dll"]