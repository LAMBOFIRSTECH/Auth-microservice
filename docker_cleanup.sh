#!/bin/bash

if [ -z "$1" ]; then
    echo "Veuillez saisir le nom du container"
    exit 1
fi
target_container=$1
output=$(docker ps -a --format "{{.Status}}|{{.Names}}|{{.Image}}")
found=false
image_to_remove=""
SONAR_PROJECT_KEY=$(cat pom.xml | grep '<sonar.projectKey>' | sed 's/.*<sonar.projectKey>\(.*\)<\/sonar.projectKey>.*/\1/')
FILE_PATH="../../../../../${SONAR_PROJECT_KEY}_index.txt"
colors() {
    RED="\033[0;31m"
    GREEN="\033[0;32m"
    YELLOW="\033[1;33m"
    CYAN="\033[1;36m"

    NC="\033[0m"
    printf "${!1}${2} ${NC}\n"
}
colors "GREEN" "####### Préparation du déploiement du container $target_container ############"
while IFS="|" read -r status name image; do
    if [[ "$name" == "$target_container" ]]; then
        found=true
        colors "YELLOW" "Vérification du status du Containeur docker $name."
        if [[ "$status" != Up ]]; then
            docker rm -f "$name"
            if [ $? -eq 0 ]; then
                colors "GREEN" "Conteneur $name supprimé avec succès."
            else
                echo "Erreur lors de la suppression du conteneur $name."
                exit 1
            fi
            docker rmi "$image"
            if [ $? -eq 0 ]; then
                colors "GREEN" "l'image $image supprimée avec succès."
            else
                echo "Erreur lors de la suppression de l'image $image."
                exit 1
            fi
            colors "RED" "Echec du pipeline: le container docker $name n'a pas pu démarrer"
            echo
            colors "YELLOW" "Décrémentation de la valeur du fichier de compteur de une unité"
            current_value=$(cat "$FILE_PATH")
            new_value=$((current_value - 1))
            if [[ "$new_value" < 0 ]]; then
                colors "YELLOW"  "Revoir la valeur du compteur dans le fichier /home/gitlab-runner/<nom_du projet>_index.txt"
                colors "YELLOW"  "sa valeur est un entier positif"
                exit 1
            fi
            echo "$new_value" >"$FILE_PATH"
            exit 1
        fi
    fi
done <<<"$output"
