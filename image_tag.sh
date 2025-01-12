#!/bin/bash

# Fichier pour stocker la valeur actuelle de `i`
SONAR_PROJECT_KEY=$(ls *.sln | sed -E 's/\.sln$//')
INDEX_FILE="../../../../../${SONAR_PROJECT_KEY}_index.txt"
 
if [ ! -f "$INDEX_FILE" ]; then
  echo 0 > "$INDEX_FILE"
fi

# Exécuter le script sonar_report.sh et vérifier le code de retour 
./sonar_report.sh 
if [ $? != 0 ]; then
  echo "Échec de connexion au registry de GitLab"
  exit 1
fi
# i=$(cat "$INDEX_FILE")
i=$(cat "$INDEX_FILE" | sed 's/^0*//')


major=$((i / 25 + 1))  
minor=$(( (i / 5) % 5 + 1 ))  
patch=$(( i % 5 + 1 ))  

# Vérifier si on dépasse les limites et ajuster `major`
if [ $major -gt 5 ]; then
  echo "Limite de tags atteinte on recommencer à 1.1.1"
  i=0
fi

# Affichage du tag
echo  "\$tag=${major}.${minor}.${patch}"

((i++))
echo $i > "$INDEX_FILE"

# On va le use dans gitlab-ci 
echo "$tag"