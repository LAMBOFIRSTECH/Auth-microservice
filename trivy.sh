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

for file in $csproj_files; do
    file=$(realpath "$file")  # Convertit le chemin en absolu
    echo -e "\n${CYAN}Analyse du fichier : $file${NC}"
    project_dir=$(dirname "$file")

    # Trivy FS scan avec redirection vers un fichier JSON dans le répertoire des rapports
    echo -e "${YELLOW}Exécution du scan Trivy sur le répertoire : $project_dir${NC}"
    trivy fs "$project_dir" --format json --output "$REPORT_DIR/trivy_scan_report_$(basename $project_dir).json"
    #trivy fs ./ --format json --output toto.json on le fait pour tout le repertoire projet


    # Vérification du statut du conteneur Docker
    if [ $? -ne 0 ]; then
        echo -e "${RED}Le scan pour $file a rencontré une erreur.${NC}"
    else
        echo -e "${GREEN}Scan terminé pour $file.${NC}"
    fi
done
# si l'instruction précédente se termine avec un succès 
# lancer le script python (qui va lire chaque rapport de trivy et insérer les data dans un tableau html généré)
# utiliser python -m http.server port => pour lancer un serveur web 
# accéder au fichier html http://localhost:port/file.html (on pourra faire passer ceci derriere nginx à terme) 
# soit mettre dans nginx le fichier html et créer un lien symbolique pour charger le fichier html dans nginx à chaque update
# Indication de fin d'analyse
echo -e "\n${GREEN}Analyse complète terminée. Les rapports sont stockés dans le répertoire $REPORT_DIR.${NC}"

