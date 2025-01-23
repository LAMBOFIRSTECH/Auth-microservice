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
BASE_DIR="./Authentication/Authentications/"
csproj_files=$(find "$BASE_DIR" -name "*.csproj")

# Vérifier si des fichiers .csproj ont été trouvés
if [ -z "$csproj_files" ]; then
    echo "Aucun fichier .csproj trouvé dans $BASE_DIR."
    exit 0
fi
for file in $csproj_files; do
    echo "Analyse du fichier : $file"
    project_dir=$(dirname "$file")
    project_name=$(basename "$file" .csproj)
    report_file="/tmp/trivy_scan_report_${project_name}.json"

    # Trivy FS scan avec redirection vers un fichier JSON
    trivy fs "$project_dir" --format json --output /tmp/trivy_scan_report.json
    trivy fs --scanners misconfig "$project_dir" --format json --output /tmp/conf.json

    # Vérification du statut
    if [ $? -ne 0 ]; then
        colors "RED" "Le scan pour $file a rencontré une erreur."
    else
        colors "GREEN" "Scan terminé pour $file."
    fi
done

echo "Analyse complète terminée."
