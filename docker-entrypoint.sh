mkdir -p /.cache && chmod -R 777 /.cache
chown -R ${PUID}:${PGID} /.cache

mkdir -p /.config && mkdir /.config/Bitwarden\ CLI
chown -R ${PUID}:${PGID} /.config/Bitwarden\ CLI

chown -R ${PUID}:${PGID} /app
exec gosu ${PUID}:${PGID} dotnet /app/Bitwarden.SecureSync.Application.dll
