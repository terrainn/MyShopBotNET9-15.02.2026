FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish "MyShopBotNET9.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

COPY --from=build /app/out .
COPY --from=build /app/appsettings.json .
COPY --from=build /app/appsettings.Development.json .

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD dotnet --info || exit 1

CMD ["dotnet", "MyShopBotNET9.dll"]