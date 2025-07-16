FROM alpine:3.21 AS builder

LABEL maintainer="lamboartur94@gmail.com" \
    org.opencontainers.image.os="linux" \
    org.opencontainers.image.architecture="amd64" \
    org.opencontainers.image.version="v2" \
    org.opencontainers.image.created="2025-07-08T00:00:00Z" \
    org.opencontainers.image.revision="02" \
    org.opencontainers.image.title="dotnet/sdk:8.0.17" \
    org.opencontainers.image.vendor="LAMBOFIRSTECH"

ENV DOTNET_VERSION=6.0.210
ENV DOTNET_DOWNLOAD_URL=https://builds.dotnet.microsoft.com/dotnet/Sdk/${DOTNET_VERSION}/dotnet-sdk-${DOTNET_VERSION}-linux-musl-x64.tar.gz
ENV DOTNET_ROOT=/opt/dotnet
ENV PATH="${PATH}:${DOTNET_ROOT}"

# Installation les dépendances nécessaires au SDK .NET
RUN apk add --no-cache bash curl icu-libs krb5-libs zlib libgcc libstdc++

# Création du service de lancement du conteneur
RUN adduser -D -u 1000 backend_api && \
    mkdir -p /opt/dotnet

WORKDIR /opt/dotnet

# Téléchargement et installation du SDK manuellement
RUN curl -SL ${DOTNET_DOWNLOAD_URL} -o dotnet.tar.gz && \
    tar -zxf dotnet.tar.gz -C .  && \
    rm dotnet.tar.gz

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Publication de l'application
WORKDIR /src
COPY . .
COPY local-nuget-feed /local-nuget-feed

RUN dotnet nuget add source /local-nuget-feed --name local
RUN dotnet restore
RUN dotnet publish Authentifications.sln -c Release -r linux-musl-x64 --self-contained false -o /app

# Runtime final minimal
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS runtime
RUN adduser -D -u 1000 backend_api
WORKDIR /app
COPY --from=builder /app .
COPY Authentifications/appsettings.* .

ENV ASPNETCORE_URLS=$ASPNETCORE_URLS
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/ssl/certs/backend.pfx

EXPOSE 8081
USER backend_api

ENTRYPOINT ["dotnet", "Teams.dll"]