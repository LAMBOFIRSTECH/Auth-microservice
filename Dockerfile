# Image de base pour l'exécution
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8081

# Phase de préparation (copie des artefacts publiés)
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS publish
WORKDIR /src

# Copier les artefacts publiés depuis le pipeline CI
COPY --from=build publish /app

# Phase finale (runtime)
FROM base AS final
WORKDIR /app

# Copier les fichiers publiés depuis la phase précédente
COPY --from=publish /app .

# Copier les fichiers de configuration
COPY Authentifications/appsettings.* . 

# Copier le certificat nécessaire
COPY TasksApi.pfx /etc/ssl/certs/TasksApi.pfx

# Configuration des variables d'environnement
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/ssl/certs/TasksApi.pfx
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=https://+:8081

# Point d'entrée de l'application
ENTRYPOINT ["dotnet", "Authentifications.dll"]
