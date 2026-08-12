FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce sadece csproj'ları kopyala -> layer cache restore adımını korur
COPY KeepApi.Common/KeepApi.Common.csproj KeepApi.Common/
COPY KeepApi.Data/KeepApi.Data.csproj KeepApi.Data/
COPY KeepApi.Application/KeepApi.Application.csproj KeepApi.Application/
COPY KeepApi.Infrastructure/KeepApi.Infrastructure.csproj KeepApi.Infrastructure/
COPY KeepApi/KeepApi.csproj KeepApi/

RUN dotnet restore KeepApi/KeepApi.csproj

COPY . .

RUN dotnet publish KeepApi/KeepApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN useradd -m appuser \
    && mkdir -p /app/dp-keys /app/logs/errorlogs \
    && chown -R appuser:appuser /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5080
ENV ASPNETCORE_ENVIRONMENT=Production

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "KeepApi.dll"]