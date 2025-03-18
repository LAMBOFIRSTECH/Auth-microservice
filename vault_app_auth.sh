#!/bin/bash
# ============================================================================
# Récupération des crédentials pour l'authentification de chaque micro-service
# ============================================================================

colors() {
    RED="\033[0;31m"
    GREEN="\033[0;32m"
    YELLOW="\033[1;33m"
    CYAN="\033[1;36m"
    NC="\033[0m" # Réinitialisation
    printf "${!1}${2} ${NC}\n"
}
if [ ! -f .env ]; then
    colors "RED" "Erreur : Fichier .env non trouvé. Veuillez configurer les variables nécessaires."
    exit 1
fi
source .env
#Récupérer vault host dans appsettings.json
check_server=$(curl -s -L -o /dev/null -w "%{http_code}" "$VAULT_HOST_URL")
if [[ "$check_server" != "200" && "$check_server" != "302" ]]; then
    colors "RED" "Erreur : Le serveur Hashiccorp Vault est inaccessible. Code HTTP: $check_server"
    exit 1
fi

# sudo groupadd vault-access  # Créer un groupe
# sudo usermod -aG vault-access gitlab-runner  # Ajouter gitlab-runner au groupe
# sudo usermod -aG vault-access root  # S'assurer que root est aussi dans le groupe
# sudo chown -R root:vault-access /tmp/vault-token/  voir la script pour la configuration de Vault
# sudo chmod -R 750 /tmp/vault-token/ Mettre ça dans le service

### Pour le role id
rootToken=$(cat /tmp/vault-token/token.txt)
role_id=$(curl -s --header "X-Vault-Token: $rootToken" $VAULT_HOST_URL/v1/auth/approle/role/dotnet-role/role-id | jq -r '.data.role_id')
echo $role_id

### Pour le secret id
secret_id=$(curl -s --header "X-Vault-Token: $rootToken" \
    --request POST \
    --data '{"role_id": "$role_id"}' \
    $VAULT_HOST_URL/v1/auth/approle/role/dotnet-role/secret-id | jq -r '.data.secret_id')
# On supprime le token.txt si vault hashicorp est down
file_config=$(ls | grep '^appsettings' | head -n 1)
jq --arg role_id "$role_id" --arg secret_id "$secret_id" \
    '.HashiCorp.AppRole.RoleId = $role_id | .HashiCorp.AppRole.SecretId = $secret_id' \
    "$file_config" | sponge "$file_config"
exit 0