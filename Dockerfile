FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# СОЗДАЕМ ПАПКУ ДЛЯ ДАННЫХ
RUN mkdir -p /app/data

COPY --from=build /app/publish .

# Указываем, что папка data будет volume
VOLUME ["/app/data"]

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyShopBotNET9.dll"]