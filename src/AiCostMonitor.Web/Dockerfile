FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/AiCostMonitor.Web/AiCostMonitor.Web.csproj", "src/AiCostMonitor.Web/"]
COPY ["src/AiCostMonitor.Shared/AiCostMonitor.Shared.csproj", "src/AiCostMonitor.Shared/"]
COPY ["src/AiCostMonitor.Core/AiCostMonitor.Core.csproj", "src/AiCostMonitor.Core/"]
RUN dotnet restore "src/AiCostMonitor.Web/AiCostMonitor.Web.csproj"
COPY . .
RUN dotnet publish "src/AiCostMonitor.Web/AiCostMonitor.Web.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY src/AiCostMonitor.Web/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
