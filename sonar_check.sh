#!/bin/bash
colors() {
  RED="\033[0;31m"
  GREEN="\033[0;32m"
  YELLOW="\033[1;33m"
  CYAN="\033[1;36m"

  NC="\033[0m"
  printf "${!1}${2} ${NC}\n" # <-- bash
}
if [ ! -f .env ]; then
  colors "RED" "Erreur : fichier .env non trouvé"
  exit 1
fi
source .env

if [ ! -f pom.xml ]; then
  colors "RED" "Erreur : Veillez ajouté le fichier pom.xml"
  exit 1
fi
SONAR_PROJECT_KEY=$(cat pom.xml | grep '<sonar.projectKey>' | sed 's/.*<sonar.projectKey>\(.*\)<\/sonar.projectKey>.*/\1/')
if [ -z "$SONAR_PROJECT_KEY" ]; then
  echo "Erreur : la clé de projet Sonar n'a pas été trouvée."
  exit 1
fi

mkdir -p ~/.ssh
echo "$SSH_PRIVATE_KEY" >~/.ssh/id_rsa # On va copier la clé privée du serveur vers mgs de clés du runner
chmod 600 ~/.ssh/id_rsa

check_server=$(curl -s -L -o /dev/null -w "%{http_code}" "$SONAR_HOST_URL")

if [ "$check_server" != "200" ] && [ "$check_server" != "302" ]; then
  colors "RED" "Le serveur Sonarqube est down"
  exit 1
fi

mvn verify sonar:sonar \
  -Dsonar.host.url="${SONAR_HOST_URL}" \
  -Dsonar.login="${SONAR_USER_TOKEN}" \
  -Dsonar.coverage.jacoco.xmlReportPaths=target/site/jacoco/jacoco.xml

# Vérifie le code de sortie de la commande Maven
if [[ $? -ne 0 ]]; then
  colors "RED" "Échec de l'analyse SonarQube."
  exit 1
fi
colors "YELLOW" "#######################Analyse SonarQube#########################################################"
colors "CYAN"   "|  1- Récupération du dossier target du repertoire créé par le compte de service gitlab-runner  |" 
colors "CYAN"   "|  2- Test de connexion sur l'instance docker sonarqube                                         |"
colors "CYAN"   "|  3- Analyse sonarqube rapport Jacoco                                                          |"
colors "GREEN"  "######################Analyse SonarQube terminée avec succès#####################################"
echo ""
exit 0
