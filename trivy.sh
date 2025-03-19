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
if [ ! -f .env.ci ]; then
    colors "RED" "Erreur : Fichier .env.ci non trouvé. Veuillez configurer les variables nécessaires."
    exit 1
fi
source .env.ci
# Lien pour accéder au rapport de vulnérabilités
lien=https://develop.lamboft.it/trivy-report
# Répertoire racine du projet
BASE_DIR=$(find . -name "*.csproj" | sed 's|^\./||')

# Chercher tous les fichiers .csproj dans le répertoire
csproj_files=$(find . -name "*.csproj")

# Vérifier si des fichiers .csproj ont été trouvés
if [ -z "$csproj_files" ]; then
    echo "Aucun fichier .csproj trouvé dans $BASE_DIR."
    exit 1
fi
# Créer un repertoire pour le rapports trivy
REPORT_DIR="./TRIVY_DIR"

# Trivy FS scan avec redirection vers un fichier JSON dans le répertoire des rapports
echo -e "${YELLOW}Exécution du scan Trivy sur le répertoire racine du projet ${NC}"

cat $REPORT_DIR/trivy_scan_report.json
echo -e "${CYAN}Pour l'image docker trivy analyse.${NC}"
cat $REPORT_DIR/trivy_docker_image.json

if [ $? -ne 0 ]; then
    echo -e "${RED}Le scan FS du repertoire racine a rencontré une erreur.${NC}"
    exit 1
fi
echo -e "\n${GREEN}Analyse complète terminée. Les rapports sont stockés dans le répertoire $REPORT_DIR.${NC}"
python3 fs_trivy_vulnerabilities.py
if [ $? -ne 0 ]; then
    echo -e "${RED}Le rapport Trivy n'a pas été généré correctement.${NC}"
    exit 1
fi
#usermod -aG www-data gitlab-runner
#chmod 775 /var/www/report/
mv report.html /var/www/report/

jq '[.Results[] | select(.Vulnerabilities != null) | .Vulnerabilities[].Severity] | group_by(.) | map({(.[0]): length}) | add' $REPORT_DIR/trivy_scan_report.json > $REPORT_DIR/Vulnerabilities.json
jq '[.Results[] | select(.Secrets != null) | .Secrets[].Severity] | group_by(.) | map({(.[0]): length}) | add' $REPORT_DIR/trivy_scan_report.json > $REPORT_DIR/Secrets.json

Secret_medium_severity=$(jq -r '.MEDIUM // 0' $REPORT_DIR/Secrets.json)
Secret_high_severity=$(jq -r '.HIGH // 0' $REPORT_DIR/Secrets.json)
Secret_critical_severity=$(jq -r '.CRITICAL // 0' $REPORT_DIR/Secrets.json)

Vulnerabilities_medium_severity=$(jq -r '.MEDIUM // 0' $REPORT_DIR/Vulnerabilities.json)
Vulnerabilities_high_severity=$(jq -r '.HIGH // 0' $REPORT_DIR/Vulnerabilities.json)
Vulnerabilities_critical_severity=$(jq -r '.CRITICAL // 0' $REPORT_DIR/Vulnerabilities.json)

if [ "$Secret_medium_severity" = "null" ]; then
  Secret_medium_severity=0
fi
if [ "$Secret_high_severity" = "null" ]; then
  Secret_high_severity=0
fi
if [ "$Secret_critical_severity" = "null" ]; then
  Secret_critical_severity=0
fi
if [ "$Vulnerabilities_medium_severity" = "null" ]; then
  Vulnerabilities_medium_severity=0
fi
if [ "$Vulnerabilities_high_severity" = "null" ]; then
  Vulnerabilities_high_severity=0
fi
if [ "$Vulnerabilities_critical_severity" = "null" ]; then
  Vulnerabilities_critical_severity=0
fi

# Calculs des totaux avec $((...))
Total_Medium_Severities=$((Secret_medium_severity + Vulnerabilities_medium_severity))
Total_High_Severities=$((Secret_high_severity + Vulnerabilities_high_severity))
Total_Critical_Severities=$((Secret_critical_severity + Vulnerabilities_critical_severity))

# Vérification pour CRITICAL
if [ "$Total_Critical_Severities" -gt 1 ]; then
   colors "RED" "Trivy scan result : $Total_Critical_Severities gravités de type CRITICAL.${NC}"
   colors "CYAN" "Veuillez consulter le rapport de vulnérabilités $lien"
   exit 1
fi

# Vérification pour HIGH
if [ "$Total_High_Severities" -gt 3 ]; then
   colors "RED" "Trivy scan result : $Total_High_Severities gravités de type HIGH.${NC}"
   colors "CYAN" "Veuillez consulter le rapport de vulnérabilités $lien"
   exit 1
fi

# Vérification pour MEDIUM
if [ "$Total_Medium_Severities" -gt 4 ]; then
   colors "RED" "Trivy scan result : $Total_Medium_Severities gravités de type MEDIUM.${NC}"
   colors "CYAN" "Veuillez consulter le rapport de vulnérabilités $lien"
   exit 1
fi
