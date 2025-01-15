#!/bin/bash

# ====================
# Script SonarQube Analysis
# ====================

# Fonction pour gérer les couleurs dans l'affichage
colors() {
    RED="\033[0;31m"
    GREEN="\033[0;32m"
    YELLOW="\033[1;33m"
    CYAN="\033[1;36m"
    NC="\033[0m" # Réinitialisation
    printf "${!1}${2} ${NC}\n"
}

# --------------------
# 1. Vérification des Pré-requis
# --------------------

# Vérification de la présence du fichier .env
if [ ! -f .env ]; then
    colors "RED" "Erreur : Fichier .env non trouvé. Veuillez configurer les variables nécessaires."
    exit 1
fi

# Chargement des variables depuis le fichier .env
source .env

# Détection automatique de la solution .sln
SONAR_PROJECT_KEY=$(ls *.sln | sed -E 's/\.sln$//')
SOLUTION_FILE=$(ls *.sln)

if [ ! -f "$SOLUTION_FILE" ]; then
    colors "RED" "Erreur : Fichier solution (.sln) introuvable."
    exit 1
fi

if [ -z "$SONAR_PROJECT_KEY" ]; then
    colors "RED" "Erreur : Clé de projet Sonar introuvable."
    exit 1
fi

# Vérification des variables essentielles
if [ -z "$SONAR_HOST_URL" ] || [ -z "$SONAR_USER_TOKEN" ]; then
    colors "RED" "Erreur : Variables SONAR_HOST_URL ou SONAR_USER_TOKEN non définies dans le fichier .env."
    exit 1
fi
ls
ls ../
if [ ! -f "$COVERAGE_REPORT_PATH" ]; then
    colors "RED" "Erreur : Fichier de couverture ($COVERAGE_REPORT_PATH) introuvable."
    exit 1
fi

# --------------------
# 2. Configuration de l'accès SSH (si nécessaire)
# --------------------
if [ -n "$SSH_PRIVATE_KEY" ]; then
    colors "YELLOW" "Configuration de la clé SSH pour SonarQube."
    mkdir -p ~/.ssh
    echo "$SSH_PRIVATE_KEY" >~/.ssh/id_rsa
    chmod 600 ~/.ssh/id_rsa
else
    colors "CYAN" "Aucune clé SSH fournie. Passage à l'étape suivante."
fi

# --------------------
# 3. Vérification du Serveur SonarQube
# --------------------
colors "CYAN" "Vérification de l'état du serveur SonarQube à l'adresse $SONAR_HOST_URL"
check_server=$(curl -s -L -o /dev/null -w "%{http_code}" "$SONAR_HOST_URL")

if [[ "$check_server" != "200" && "$check_server" != "302" ]]; then
    colors "RED" "Erreur : Le serveur SonarQube est inaccessible. Code HTTP: $check_server"
    exit 1
fi

# --------------------
# 4. Analyse SonarQube
# --------------------
colors "YELLOW" "Démarrage de l'analyse SonarQube pour le projet $SONAR_PROJECT_KEY"

# Installation de l'outil SonarScanner si nécessaire
if ! command -v dotnet-sonarscanner &>/dev/null; then
    colors "CYAN" "SonarScanner pour .NET non trouvé. Installation en cours..."
    dotnet tool install --global dotnet-sonarscanner --version 5.11.0
    export PATH="$PATH:$HOME/.dotnet/tools"
else
    export PATH="$PATH:$HOME/.dotnet/tools"
fi
# Initialisation de l'analyse
dotnet sonarscanner begin \
    /k:"$SONAR_PROJECT_KEY" \
    /d:sonar.host.url="$SONAR_HOST_URL" \
    /d:sonar.login="$SONAR_USER_TOKEN" \
    /d:sonar.cs.opencover.reportsPaths="$COVERAGE_REPORT_PATH"

# --------------------
# 5. Compilation du Projet
# --------------------
colors "YELLOW" "Compilation de la solution $SOLUTION_FILE avec configuration $BUILD_CONFIGURATION"
dotnet build "$SOLUTION_FILE" --configuration "$BUILD_CONFIGURATION" --no-restore

# Vérification de la réussite de la compilation
if [[ $? -ne 0 ]]; then
    colors "RED" "Échec de la compilation. Analyse SonarQube interrompue."
    exit 1
fi

# --------------------
# 6. Fin de l'analyse SonarQube
# --------------------
colors "YELLOW" "Finalisation de l'analyse SonarQube"
dotnet sonarscanner end /d:sonar.login="$SONAR_USER_TOKEN"

if [[ $? -ne 0 ]]; then
    colors "RED" "Échec de l'analyse SonarQube."
    exit 1
fi

# --------------------
# 7. Message de Succès
# --------------------
colors "GREEN" "###################### Analyse SonarQube terminée avec succès ######################"
colors "CYAN" "|  Rapport de couverture généré et envoyé à SonarQube                                   |"
colors "CYAN" "|  Serveur SonarQube accessible et analyse complétée                                   |"
colors "GREEN" "######################################################################################"
exit 0
