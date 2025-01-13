# Authentifications
Pour la gestion des authentifications basique et Jwt 


# TAF
## Creer un médiator entre les services RedisCacheTokenService et RedisCacheService
    > - Avant de vouloir rafraichir le token d'un utilisateur, 
    > - if no cachekey with externalAPi data in redis delete token session user 

    {
  "component": {
    "key": "Authentifications",
    "name": "Authentifications",
    "qualifier": "TRK",
    "measures": [
      {
        "metric": "coverage",
        "value": "0.0",
        "bestValue": false
      },
      {
        "metric": "complexity",
        "value": "131"
      },
      {
        "metric": "code_smells",
        "value": "43",
        "bestValue": false
      },
      {
        "metric": "ncloc",
        "value": "952"
      }
    ]
  }
}
