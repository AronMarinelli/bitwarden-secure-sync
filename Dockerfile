FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
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

ENV PATH="/app:${PATH}" \
    PUID=0 \
    PGID=0

ENV GOSU_VERSION 1.19

RUN set -eux; \
	\
	apk add --no-cache --virtual .gosu-deps \
		ca-certificates \
		dpkg \
		gnupg \
	; \
	\
	dpkgArch="$(dpkg --print-architecture | awk -F- '{ print $NF }')"; \
	wget -O /usr/local/bin/gosu "https://github.com/tianon/gosu/releases/download/$GOSU_VERSION/gosu-$dpkgArch"; \
	wget -O /usr/local/bin/gosu.asc "https://github.com/tianon/gosu/releases/download/$GOSU_VERSION/gosu-$dpkgArch.asc"; \
	\

	export GNUPGHOME="$(mktemp -d)"; \
	gpg --batch --keyserver hkps://keys.openpgp.org --recv-keys B42F6819007F00F88E364FD4036A9C25BF357DD4; \
	gpg --batch --verify /usr/local/bin/gosu.asc /usr/local/bin/gosu; \
	gpgconf --kill all; \
	rm -rf "$GNUPGHOME" /usr/local/bin/gosu.asc; \
	\

	apk del --no-network .gosu-deps; \
	\
	chmod +x /usr/local/bin/gosu; \

	gosu --version; \
	gosu nobody true

VOLUME ["/app/config", "/app/data"]

COPY --from=publish /app/publish .
ENTRYPOINT ["/bin/sh", "docker-entrypoint.sh"]

LABEL org.opencontainers.image.authors="aron@marinelli.nl"
LABEL org.opencontainers.image.url="https://github.com/AronMarinelli/bitwarden-secure-sync"
LABEL org.opencontainers.image.title="Bitwarden Secure Sync"
LABEL org.opencontainers.image.description ="A simple tool that can be used to export your Bitwarden vault to a local file periodically."
