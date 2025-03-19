#!/bin/bash
# =========================
# Script SonarQube avec Couverture de Code et Rapport Cobertura
# =========================

# Fonction pour gérer les couleurs dans l'affichage
colors() {
    RED="\033[0;31m"
    GREEN="\033[0;32m"
    YELLOW="\033[1;33m"
    CYAN="\033[1;36m"
    NC="\033[0m" # Réinitialisation
    printf "${!1}${2}${NC}\n"
}

# Affichage du répertoire courant pour vérification
pwd

# --------------------
# 1. Vérification des Pré-requis
# --------------------
if [ ! -f appsettings.json ]; then
    colors "RED" "Erreur : Fichier appsettings.json non trouvé."
    exit 1
fi
if [ ! -f .env ]; then
    colors "RED" "Erreur : Fichier .env non trouvé. Veuillez configurer les variables nécessaires."
    exit 1
fi
source .env

# Détection automatique de la solution .sln
SONAR_PROJECT_KEY=$(ls *.sln | sed -E 's/\.sln$//')
SONAR_PROJECT_VERSION=$(jq -r .'Kestrel.ApiVersion' appsettings.json)
SOLUTION_FILE=$(ls *.sln)

# Vérification des variables essentielles
required_vars=("SONAR_PROJECT_KEY" "SONAR_HOST_URL" "SONAR_USER_TOKEN" "BUILD_CONFIGURATION" "SONAR_PROJECT_VERSION")
for var in "${required_vars[@]}"; do
    if [[ -z "${!var}" ]]; then
        colors "RED" "La variable $var n'est pas définie. Veuillez vérifier votre configuration."
        exit 1
    fi
done

# --------------------
# 2. Vérification du Serveur SonarQube
# --------------------
colors "CYAN" "Vérification de l'état du serveur SonarQube à l'adresse $SONAR_HOST_URL"
check_server=$(curl -s -L -o /dev/null -w "%{http_code}" "$SONAR_HOST_URL")
if [[ "$check_server" != "200" && "$check_server" != "302" ]]; then
    colors "RED" "Erreur : Le serveur SonarQube est inaccessible. Code HTTP: $check_server"
    exit 1
fi

# --------------------
# 3. Installation de SonarScanner et ReportGenerator
# --------------------
colors "YELLOW" "Vérification de dotnet-sonarscanner et reportgenerator..."

# Installation de SonarScanner
if ! command -v dotnet-sonarscanner &>/dev/null; then
    colors "CYAN" "Installation de dotnet-sonarscanner..."
    dotnet tool install --global dotnet-sonarscanner --version 6.0.0 || {
        colors "RED" "Échec de l'installation de SonarScanner."
        exit 1
    }
    export PATH="$PATH:$HOME/.dotnet/tools"
else
    colors "CYAN" "SonarScanner déjà installé."
fi

# Installation de ReportGenerator
if ! command -v reportgenerator &>/dev/null; then
    colors "CYAN" "Installation de ReportGenerator..."
    dotnet tool install --global dotnet-reportgenerator-globaltool --version 4.8.6 || {
        colors "RED" "Échec de l'installation de ReportGenerator."
        exit 1
    }
else
    colors "CYAN" "ReportGenerator déjà installé."
fi

# --------------------
# 4. Création du fichier de configuration SonarQube
# --------------------
colors "YELLOW" "Création du fichier de configuration SonarQube"
mkdir -p .sonarqube/conf
cat > SonarQubeAnalysisConfig.xml <<EOL
<?xml version="1.0" encoding="utf-8" ?>
<SonarQubeAnalysisProperties xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns="http://www.sonarsource.com/msbuild/integration/2015/1">
   <Property Name="sonar.host.url">${SONAR_HOST_URL}</Property>
   <Property Name="sonar.token">${SONAR_USER_TOKEN}</Property>
   <Property Name="sonar.projectKey">$SONAR_PROJECT_KEY</Property>
   <Property Name="sonar.projectName">Microservice $SONAR_PROJECT_KEY</Property>
   <Property Name="sonar.projectVersion">$SONAR_PROJECT_VERSION</Property>
</SonarQubeAnalysisProperties>
EOL

# Vérification de la création du fichier
colors "CYAN" "Contenu du dossier .sonarqube/conf :"
chmod -R 777 .sonarqube/
cp SonarQubeAnalysisConfig.xml .sonarqube/conf/SonarQubeAnalysisConfig.xml
# --------------------
# 5. Démarrage de l'analyse SonarQube
# --------------------
colors "YELLOW" "Démarrage de l'analyse SonarQube pour le projet $SONAR_PROJECT_KEY"
dotnet sonarscanner begin \
    /k:"$SONAR_PROJECT_KEY" \
    /d:sonar.host.url="$SONAR_HOST_URL" \
    /d:sonar.token="$SONAR_USER_TOKEN" \
    /d:sonar.cs.opencover.reportsPaths="TestResults/coverage/Cobertura.xml"

# --------------------
# 6. Restauration et Compilation du Projet
# --------------------
colors "YELLOW" "Restauration et compilation du projet $SOLUTION_FILE"
dotnet restore "$SOLUTION_FILE"
dotnet build "$SOLUTION_FILE" --configuration "$BUILD_CONFIGURATION" --no-restore
if [[ $? -ne 0 ]]; then
    colors "RED" "Échec de la compilation. Analyse SonarQube interrompue."
    exit 1
fi

# --------------------
# 7. Exécution des tests avec XPlat Code Coverage
# --------------------
colors "YELLOW" "Exécution des tests et collecte de la couverture de code avec XPlat Code Coverage"
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults
if [[ $? -ne 0 ]]; then
    colors "RED" "Échec des tests. Analyse SonarQube interrompue."
    exit 1
fi

# --------------------
# 8. Vérification du fichier de couverture
# --------------------
COVERAGE_FILE=$(find TestResults -name "coverage.cobertura.xml" | head -n 1)
if [[ -z "$COVERAGE_FILE" ]]; then
    colors "RED" "Erreur : Aucun fichier coverage.cobertura.xml trouvé."
    exit 1
fi
colors "GREEN" "Fichier de couverture trouvé : $COVERAGE_FILE"

# --------------------
# 9. Génération du rapport Cobertura avec ReportGenerator
# --------------------
colors "YELLOW" "Génération du rapport de couverture en format Cobertura"
reportgenerator -reports:"$COVERAGE_FILE" -targetdir:"TestResults/coverage" -reporttypes:cobertura
if [[ $? -ne 0 ]]; then
    colors "RED" "Erreur : Échec de la génération du rapport Cobertura."
    exit 1
fi

# --------------------
# 10. Vérification du fichier Cobertura
# --------------------
if [ ! -f "TestResults/coverage/Cobertura.xml" ]; then
    colors "RED" "Erreur : Le fichier Cobertura.xml n'a pas été généré."
    exit 1
fi
colors "GREEN" "Le fichier Cobertura.xml a été généré avec succès."

# --------------------
# 11. Finalisation de l'analyse SonarQube
# --------------------
colors "YELLOW" "Finalisation de l'analyse SonarQube..."
ls -l .sonarqube/conf/
dotnet sonarscanner end /d:sonar.token="$SONAR_USER_TOKEN"
if [[ $? -ne 0 ]]; then
    colors "RED" "Échec de la finalisation de l'analyse SonarQube."
    exit 1
fi
colors "GREEN" "Analyse SonarQube terminée avec succès."

# --------------------
# 12. Résumé du Processus
# --------------------
colors "GREEN" "####################### Analyse SonarQube Terminée avec Succès ##########################"
colors "CYAN" "|  Rapport de couverture généré en Cobertura et envoyé à SonarQube                      |"
colors "CYAN" "|  Serveur SonarQube accessible et analyse effectuée                                    |"
colors "GREEN" "#########################################################################################"

exit 0