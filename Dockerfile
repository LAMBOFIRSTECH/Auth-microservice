# Image de base pour l'exécution
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8081

# Phase de préparation (copie des artefacts publiés)
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS publish
WORKDIR /src

# Ajouter les sources de l'application pour publication
COPY . . 
RUN dotnet publish Authentifications.sln --configuration Release --output /app

# Phase finale (runtime)
FROM base AS final
WORKDIR /app

# Copier les fichiers publiés depuis la phase précédente
COPY --from=publish /app .
RUN echo "Voici le dossier app"
RUN ls -R /app
RUN echo "FINISHED 1"
RUN echo "Voici le dossier app/Authentifications"
RUN ls -R /app/Authentifications
RUN echo "FINISHED 2"
# Copier les fichiers de configuration
COPY Authentifications/appsettings.* . 

# Copier le certificat nécessaire
COPY TasksApi.pfx /etc/ssl/certs/TasksApi.pfx
COPY Redis/certs/redis-client.pfx /etc/ssl/certs/redis-client.pfx
COPY Redis/certs/ca.crt /etc/ssl/certs/ca.crt

# Configuration des variables d'environnement
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/ssl/certs/TasksApi.pfx
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=https://+:8081

# Point d'entrée de l'application
ENTRYPOINT ["dotnet", "/app/Authentifications.dll"]