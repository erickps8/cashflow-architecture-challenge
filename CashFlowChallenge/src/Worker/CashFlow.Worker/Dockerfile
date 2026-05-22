# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

# Esta fase é usada para compilar o projeto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["src/Worker/CashFlow.Worker/CashFlow.Worker.csproj", "src/Worker/CashFlow.Worker/"]

RUN dotnet restore "./src/Worker/CashFlow.Worker/CashFlow.Worker.csproj"

COPY . .

WORKDIR "/src/src/Worker/CashFlow.Worker"

RUN dotnet build "./CashFlow.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "./CashFlow.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CashFlow.Worker.dll"]