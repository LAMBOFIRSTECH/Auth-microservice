FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /source
EXPOSE 8081

# Phase de construction
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copie des fichiers du projet et restauration des dépendances
COPY Authentifications/*.csproj Authentifications/
RUN dotnet restore "Authentifications/Authentifications.csproj" --disable-parallel

# Copie du reste du code source et des fichiers de configuration
COPY Authentifications/ Authentifications/

# Construction du projet
RUN dotnet build "Authentifications/Authentifications.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Phase de publication
FROM build AS publish
WORKDIR /src
# Publication des fichiers nécessaires pour l'exécution
RUN dotnet publish "Authentifications/Authentifications.csproj" -c $BUILD_CONFIGURATION -o /app/publish || { echo 'dotnet publish failed'; exit 1; }

# Phase finale d'exécution (RUNTIME)
FROM base AS runtime
WORKDIR /source

# Copier les fichiers publiés depuis la phase de publication
COPY --from=publish /app/publish .

# Copier les fichiers de configuration (y compris appsettings.*)
COPY Authentifications/appsettings.* .

# Copier le certificat nécessaire
COPY TasksApi.pfx /etc/ssl/certs/TasksApi.pfx

# Configuration des variables d'environnement
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/ssl/certs/TasksApi.pfx
# Check plustard
ENV ASPNETCORE_ENVIRONMENT=Development 
ENV ASPNETCORE_URLS=https://+:8081

# Point d'entrée
ENTRYPOINT ["dotnet", "Authentifications.dll"]