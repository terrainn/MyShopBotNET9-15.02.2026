FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Копируем файл проекта и восстанавливаем зависимости
COPY *.csproj .
RUN dotnet restore

# Копируем все остальные файлы
COPY . .

# ЯВНО копируем appsettings.json (на всякий случай)
COPY appsettings.json .
COPY appsettings.Development.json .

RUN dotnet publish -c Release -o out

# Финальный образ
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# Копируем собранное приложение
COPY --from=build /app/out .

# ЯВНО копируем конфиги в финальный образ
COPY --from=build /app/appsettings.json .
COPY --from=build /app/appsettings.Development.json .

# Запускаем бота
CMD ["dotnet", "MyShopBotNET9.dll"]