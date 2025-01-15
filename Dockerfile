# Image de base pour l'exécution
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8081

# Phase de publication (génération des fichiers nécessaires à l'exécution)
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS publish
WORKDIR /src

# Ajouter les sources NuGet (ajout du cache local et d'un repo externe si nécessaire)
RUN dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org
RUN dotnet restore Authentifications/*.csproj --disable-parallel

# Copier les fichiers de la solution
COPY Authentifications/*.csproj ./Authentifications/
COPY Authentifications/ ./Authentifications/

# Publier les fichiers nécessaires pour l'exécution
RUN dotnet publish -c Release -o /app/publish --no-restore

# Phase finale (runtime)
FROM base AS final
WORKDIR /app

# Copier les fichiers publiés depuis la phase de publication
COPY --from=publish /app/publish .

# Copier les fichiers de configuration
COPY Authentifications/appsettings.* . 

# Copier le certificat nécessaire
COPY TasksApi.pfx /etc/ssl/certs/TasksApi.pfx

# Configuration des variables d'environnement
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/ssl/certs/TasksApi.pfx
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=https://+:8081

# Point d'entrée
ENTRYPOINT ["dotnet", "Authentifications.dll"]
