# Authentication
Pour la gestion des authentifications basique et Jwt 
Et communication avec le serveur de validation du token JWT dans l'api de base
Intégration à RabbitMQ
docker-compose avec la bd redis pour l'image docker mettre en place Nexus pour la gestion des images

squ_56280fdf200e8344dcfbd7e6cfb8650d674da8df
Points Clés à Vérifier
Authentification avec HashiCorp Vault :

Configurez un mécanisme d’authentification sécurisé pour le serveur d’autorisation (par exemple, un token Vault ou une méthode basée sur Kubernetes, approle, etc.).
Rotation des clés :

Si les clés doivent être régulièrement renouvelées, assurez-vous que Vault met à jour la clé publique. Configurez votre serveur d’autorisation pour récupérer régulièrement la clé publique.
Permissions dans Vault :
Configurez les autorisations Vault pour que seul le serveur d’autorisation puisse lire le secret contenant la clé publique :

Rajouter un container docker pour les tests unitaires avec testContainers