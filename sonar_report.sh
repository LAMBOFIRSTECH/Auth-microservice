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

SONAR_PROJECT_KEY=$(ls *.sln | sed -E 's/\.sln$//')
if [ -z "$SONAR_PROJECT_KEY" ]; then
    colors "RED" "Erreur : la clé de projet Sonar n'a pas été trouvée."
    exit 1
fi
curl --request GET \
    --url "${SONAR_HOST_URL}/api/measures/component?metricKeys=ncloc%2Ccode_smells%2Ccomplexity%2Ccoverage&component=${SONAR_PROJECT_KEY}" \
    -u "${SONAR_USER_TOKEN}:" \
    -H "Accept: application/json" \
    -o "${SONAR_PROJECT_KEY}-sonar-report.json"

coverage=$(echo "${SONAR_PROJECT_KEY}-sonar-report.json" | jq -r '.component.measures[] | select(.metric == "coverage") | .value')
coverage_value=$(echo "$coverage" | awk '{print $1 + 0}')
if (($(echo "$coverage_value > 0.0" | bc -l))); then 
    colors "RED" "Le code proposé n'est pas entierement couvert"
    echo ""
    colors "YELLOW" "L'image docker ne sera pas construite"
    exit 1
fi
exit 0 # Code de retour pour ce script on va le récupérer dans le script docker_image.sh
