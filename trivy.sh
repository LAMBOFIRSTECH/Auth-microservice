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
BASE_DIR="./Authentifications" # Répertoire racine du projet

# Chercher tous les fichiers .csproj dans le répertoire
csproj_files=$(find "$BASE_DIR" -name "*.csproj")

# Vérifier si des fichiers .csproj ont été trouvés
if [ -z "$csproj_files" ]; then
    echo "Aucun fichier .csproj trouvé dans $BASE_DIR."
    exit 0
fi

# Créer un répertoire pour stocker les rapports de Trivy
REPORT_DIR="./trivy_reports"
mkdir -p "$REPORT_DIR"

# Obtenir le chemin absolu de BASE_DIR
BASE_DIR_ABS=$(pwd)/$BASE_DIR

# Lancer Trivy en mode serveur (une seule fois)
echo -e "${YELLOW}Démarrage du serveur Trivy...${NC}"
docker rm -f trivy-server || true
docker run -d \
  -p 4954:4954 \
  --name trivy-server \
  aquasec/trivy:latest server

# Pour chaque fichier .csproj trouvé, effectuer un scan Trivy sur son répertoire
for file in $csproj_files; do
    echo -e "\n${CYAN}Analyse du fichier : $file${NC}"
    project_dir=$(dirname "$file")

    # Trivy FS scan avec redirection vers un fichier JSON dans le répertoire des rapports
    echo -e "${YELLOW}Exécution du scan Trivy sur le répertoire : $project_dir${NC}"
    trivy fs "$project_dir" --format json --output "$REPORT_DIR/trivy_scan_report_$(basename $project_dir).json"

    # Arrêter et supprimer un conteneur TRIVY existant s'il y en a un
    echo -e "${YELLOW}Vérification et suppression du conteneur TRIVY existant...${NC}"
    docker rm -f trivy-ui || true

    # Lancer l'interface graphique Trivy UI
    echo -e "${YELLOW}Lancement du conteneur Docker avec l'interface Trivy UI...${NC}"
    docker run -d \
      -p 8070:8080 \
      --name trivy-ui \
      -e TRIVY_SERVER_URL="http://localhost:4954" \
      aquasec/trivy:latest ui

    # Vérification du statut du conteneur Docker
    if [ $? -ne 0 ]; then
        echo -e "${RED}Le scan pour $file a rencontré une erreur.${NC}"
    else
        echo -e "${GREEN}Scan terminé pour $file.${NC}"
    fi
done

# Indication de fin d'analyse
echo -e "\n${GREEN}Analyse complète terminée. Les rapports sont stockés dans le répertoire $REPORT_DIR.${NC}"
