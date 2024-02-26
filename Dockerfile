FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Bitwarden.SecureSync.Application/Bitwarden.SecureSync.Application.csproj", "Bitwarden.SecureSync.Application/"]
COPY ["Bitwarden.SecureSync.Interfaces/Bitwarden.SecureSync.Interfaces.csproj", "Bitwarden.SecureSync.Interfaces/"]
COPY ["Bitwarden.SecureSync.Logic/Bitwarden.SecureSync.Logic.csproj", "Bitwarden.SecureSync.Logic/"]
COPY ["Bitwarden.SecureSync.Models/Bitwarden.SecureSync.Models.csproj", "Bitwarden.SecureSync.Models/"]
RUN dotnet restore "Bitwarden.SecureSync.Application/Bitwarden.SecureSync.Application.csproj"
COPY . .
WORKDIR "/src/Bitwarden.SecureSync.Application"
RUN dotnet build "Bitwarden.SecureSync.Application.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Bitwarden.SecureSync.Application.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bitwarden.SecureSync.Application.dll"]

VOLUME ["/app/config", "/app/data"]

USER root
RUN chown -R $APP_UID /app
USER $APP_UID

LABEL org.opencontainers.image.authors="aron@marinelli.nl"
LABEL org.opencontainers.image.url="https://github.com/AronMarinelli/bitwarden-secure-sync"
LABEL org.opencontainers.image.title="Bitwarden Secure Sync"
LABEL org.opencontainers.image.description ="A simple tool that can be used to export your Bitwarden vault to a local file periodically."
