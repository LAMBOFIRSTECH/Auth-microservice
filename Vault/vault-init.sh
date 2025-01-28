VAULT_TOKEN="root"
# Initialisation de Vault et configuration du backend
export VAULT_ADDR=$VAULT_ADDR
export VAULT_TOKEN=$VAULT_TOKEN
# Authentification via token
vault login $VAULT_TOKEN
# Activer un moteur de secrets (key-value v2)
vault secrets enable -path=kv-v2 kv-v2
# Créer une politique admin
cat <<EOF > auth-policy.hcl
path "kv-v2/*" {
  capabilities = ["create", "read", "update", "delete", "list"]
}
EOF
vault policy write admin auth-policy.hcl
# Associer un token à la politique admin
vault token create -policy="admin" -field=token > admin-token.txt
echo "Token admin généré :"
cat admin-token.txt