FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/AiCostMonitor.Api/AiCostMonitor.Api.csproj", "src/AiCostMonitor.Api/"]
COPY ["src/AiCostMonitor.Core/AiCostMonitor.Core.csproj", "src/AiCostMonitor.Core/"]
COPY ["src/AiCostMonitor.Shared/AiCostMonitor.Shared.csproj", "src/AiCostMonitor.Shared/"]
RUN dotnet restore "src/AiCostMonitor.Api/AiCostMonitor.Api.csproj"
COPY . .
RUN dotnet publish "src/AiCostMonitor.Api/AiCostMonitor.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AiCostMonitor.Api.dll"]
