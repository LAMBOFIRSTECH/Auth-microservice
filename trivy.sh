#!/bin/bash

# ====================
# Script de scan Trivy
# ====================

colors() {
    RED="\033[0;31m"
    GREEN="\033[0;32m"
    YELLOW="\033[1;33m"
    CYAN="\033[1;36m"
    NC="\033[0m" # Réinitialisation
    printf "${!1}${2} ${NC}\n"
}

# Répertoire racine du projet
BASE_DIR=$(find . -name "*.csproj" | sed 's|^\./||')

# Chercher tous les fichiers .csproj dans le répertoire
csproj_files=$(find . -name "*.csproj")

# Vérifier si des fichiers .csproj ont été trouvés
if [ -z "$csproj_files" ]; then
    echo "Aucun fichier .csproj trouvé dans $BASE_DIR."
    exit 0
fi
# Créer un repertoire pour le rapports trivy
REPORT_DIR="./trivy_reports"
mkdir -p "$REPORT_DIR"
# Trivy FS scan avec redirection vers un fichier JSON dans le répertoire des rapports
echo -e "${YELLOW}Exécution du scan Trivy sur le répertoire racine du projet ${NC}"
trivy fs ./ --format json --output "$REPORT_DIR/trivy_scan_report.json"

if [ $? -ne 0 ]; then
    echo -e "${RED}Le scan FS du repertoire racine a rencontré une erreur.${NC}"
    exit 0
fi
echo -e "\n${GREEN}Analyse complète terminée. Les rapports sont stockés dans le répertoire $REPORT_DIR.${NC}"
python3 fs_trivy_vulnerabilities.py
if [ $? -ne 0 ]; then
    echo -e "${RED}Le rapport Trivy n'a pas été généré correctement.${NC}"
    exit 0
fi
# Filtrer le json et récupérer quantité de sévérités | mettre le resultat dans un dictionnaire exemple => ("HIGH",3)
# trivy fs --severity MEDIUM,HIGH,CRITICAL --format json $REPORT_DIR/trivy_scan_report.json | jq -r '.Results[].Secrets[].Severity' | sort | uniq -c
# trivy fs --severity MEDIUM,HIGH,CRITICAL --format json ./ | jq -r '.Results[].Vulnerabilities[].Severity' | sort | uniq -c

## En fonction des valeurs reçues on va émettre des conditions pour faire échouer ou passer le pipeline
### Additionner les sévérités de Results[].Vulnerabilities[].Severity et Results[].Secrets[].Severity
### si nombre total de severité medium >= 5 Echec
### si nombre total de severité high >= 3 Echec
### si nombre total de severité critical >= 1 Echec

# accéder au fichier html http://localhost:port/file.html (on pourra faire passer ceci derriere nginx à terme)
# soit mettre dans nginx le fichier html et créer un lien symbolique pour charger le fichier html dans nginx à chaque update
