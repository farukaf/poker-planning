FROM mcr.microsoft.com/dotnet/sdk:10.0.402-alpine3.24@sha256:230c3fbae0ceedc70bca04eed2bfb5694e8b248cadbb81010f41b7a993601f55 AS build
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["PokerPlanning/", "PokerPlanning/"]
RUN dotnet restore "PokerPlanning/PokerPlanning.csproj" --locked-mode
COPY . .
RUN dotnet publish "PokerPlanning/PokerPlanning.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13-alpine3.24@sha256:819bd3649d8bf53f35527c68d62e17b348ef6f24ae73073f497f00a878348062 AS final
WORKDIR /app
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s \
  CMD wget -q -O /dev/null http://127.0.0.1:8080/ || exit 1
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PokerPlanning.dll"]
