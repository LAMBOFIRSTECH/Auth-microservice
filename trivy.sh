#!/bin/bash

# ====================
# Script de scan Trivy
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

# Définir la racine du projet, incluant le projet principal et les tests
BASE_DIR="./Authentifications"  # Répertoire racine du projet

# Chercher tous les fichiers .csproj dans le répertoire
csproj_files=$(find "$BASE_DIR" -name "*.csproj")

# Vérifier si des fichiers .csproj ont été trouvés
if [ -z "$csproj_files" ]; then
    echo "Aucun fichier .csproj trouvé dans $BASE_DIR."
    exit 0
fi

# Pour chaque fichier .csproj trouvé, effectuer un scan Trivy sur son répertoire
for file in $csproj_files; do
    echo "Analyse du fichier : $file"
    project_dir=$(dirname "$file")

    # Trivy FS scan avec redirection vers un fichier JSON dans /tmp
    trivy fs "$project_dir" --format json --output "/tmp/trivy_scan_report_$(basename $project_dir).json"

    # Vérification du statut
    if [ $? -ne 0 ]; then
        echo "Le scan pour $file a rencontré une erreur."
    else
        echo "Scan terminé pour $file."
    fi
done

echo "Analyse complète terminée."
