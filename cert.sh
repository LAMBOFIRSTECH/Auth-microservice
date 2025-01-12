!#/bin/bash
openssl genrsa -out ca.key 2048
openssl req -x509 -new -nodes -key ca.key -sha256 -days 365 -out ca.crt -subj "/CN=Redis-CA"
openssl genrsa -out redis-server.key 2048
openssl req -new -key redis-server.key -out redis-server.csr -subj "/CN=Redis-Server"
openssl x509 -req -in redis-server.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out redis-server.crt -days 365 -sha256
openssl genrsa -out redis-client.key 2048
openssl req -new -key redis-client.key -out redis-client.csr -subj "/CN=Redis-Client"
openssl x509 -req -in redis-client.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out redis-client.crt -days 365 -sha256
sudo openssl pkcs12 -export -out redis-client.pfx -inkey redis-client.key -in redis-client.crt
