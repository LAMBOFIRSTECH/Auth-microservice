# Authentifications
Pour la gestion des authentifications basique et Jwt 


# TAF
## Creer un médiator entre les services RedisCacheTokenService et RedisCacheService
    > - Avant de vouloir rafraichir le token d'un utilisateur, 
    > - if no cachekey with externalAPi data in redis delete token session user 