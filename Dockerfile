# Image de base pour l'exécution
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8081

# Phase de préparation (SDK) - Utiliser l'image SDK pour la publication
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copier les sources de l'application
COPY . . 

# Restaurer les dépendances
RUN dotnet restore

# Publier l'application en mode Release et mettre les artefacts dans /app
RUN dotnet publish Authentifications.sln --configuration Release --output /app

# Phase finale (runtime)
FROM base AS final
WORKDIR /app

# Copier les fichiers publiés depuis la phase de build
COPY --from=build /app . 

# Copier les fichiers de configuration nécessaires
COPY Authentifications/appsettings.* . 

# Copier les certificats nécessaires
COPY TasksApi.pfx /etc/ssl/certs/TasksApi.pfx
COPY Redis/certs/redis-client.pfx /etc/ssl/certs/redis-client.pfx
COPY Redis/certs/ca.crt /etc/ssl/certs/ca.crt

# Configuration des variables d'environnement
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/ssl/certs/TasksApi.pfx
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=https://+:8081

# Point d'entrée de l'application
ENTRYPOINT ["dotnet", "/app/Authentifications.dll"]