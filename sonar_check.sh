#!/bin/bash

# Fonction pour gérer les couleurs dans l'affichage
colors() {
    RED="\033[0;31m"
    GREEN="\033[0;32m"
    YELLOW="\033[1;33m"
    CYAN="\033[1;36m"
    NC="\033[0m" # Réinitialisation
    printf "${!1}${2} ${NC}\n"
}

# Vérification de la présence du fichier .env
if [ ! -f .env ]; then
    colors "RED" "Erreur : fichier .env non trouvé"
    exit 1
fi

# Chargement des variables depuis le fichier .env
source .env

# Détection automatique de la solution .sln
SONAR_PROJECT_KEY=$(ls *.sln | sed -E 's/\.sln$//')
FILE=$(ls *.sln)

if [ ! -f "$FILE" ]; then
    colors "RED" "Erreur : Veuillez ajouter le fichier Authentifications.sln"
    exit 1
fi

if [ -z "$SONAR_PROJECT_KEY" ]; then
    colors "RED" "Erreur : la clé de projet Sonar n'a pas été trouvée."
    exit 1
fi


# Configuration SSH pour utiliser les clés privées
mkdir -p ~/.ssh
echo "$SSH_PRIVATE_KEY" >~/.ssh/id_rsa
chmod 600 ~/.ssh/id_rsa

# Vérification de l'état du serveur SonarQube
check_server=$(curl -s -L -o /dev/null -w "%{http_code}" "$SONAR_HOST_URL")
if [ "$check_server" != "200" ] && [ "$check_server" != "302" ]; then
    colors "RED" "Le serveur SonarQube est inaccessible. Code HTTP: $check_server"
    exit 1
fi
if [ ! -f "$COVERAGE_REPORT_PATH" ]; then
  echo "Erreur : Le fichier de couverture '$COVERAGE_REPORT_PATH' est introuvable."
  exit 1
fi
# Initialisation de l'analyse SonarQube
colors "YELLOW" "Démarrage de l'analyse SonarQube"
# dotnet tool install --global dotnet-sonarscanner --version 5.11.0
export PATH="$PATH:$HOME/.dotnet/tools"

dotnet sonarscanner begin \
    /k:"$SONAR_PROJECT_KEY" \
    /d:sonar.host.url=${SONAR_HOST_URL} \
    /d:sonar.login=${SONAR_USER_TOKEN} \
    /d:sonar.cs.opencover.reportsPaths=${COVERAGE_REPORT_PATH}

# Compilation du projet
colors "YELLOW" "Construction du projet ${SONAR_PROJECT_KEY}"
dotnet build $FILE --configuration "${BUILD_CONFIGURATION}" --no-restore

# Vérification de la réussite de la compilation
if [[ $? -ne 0 ]]; then
    colors "RED" "Échec de la construction du projet."
    exit 1
fi

# Fin de l'analyse SonarQube
colors "YELLOW" "Fin de l'analyse SonarQube"
dotnet sonarscanner end /d:sonar.login="${SONAR_USER_TOKEN}"

# Vérification du code de sortie de l'analyse SonarQube
if [[ $? -ne 0 ]]; then
    colors "RED" "Échec de l'analyse SonarQube."
    exit 1
fi

# Affichage d'un message de succès
colors "GREEN" "###################### Analyse SonarQube terminée avec succès ######################"
colors "CYAN" "|  1- Rapport de couverture généré et envoyé à SonarQube                                |"
colors "CYAN" "|  2- Test de connexion sur l'instance Docker SonarQube                                 |"
colors "CYAN" "|  3- Analyse SonarQube complétée                                                      |"
colors "GREEN" "######################################################################################"
exit 0
