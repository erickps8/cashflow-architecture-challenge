FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Assistant/CashFlow.Assistant.Api/CashFlow.Assistant.Api.csproj", "src/Assistant/CashFlow.Assistant.Api/"]
RUN dotnet restore "src/Assistant/CashFlow.Assistant.Api/CashFlow.Assistant.Api.csproj"
COPY . .
WORKDIR "/src/src/Assistant/CashFlow.Assistant.Api"
RUN dotnet publish "CashFlow.Assistant.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CashFlow.Assistant.Api.dll"]
