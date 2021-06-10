Write-Host "Up containers"
docker-compose --compatibility -f docker-compose.ci.yml pull
docker-compose --compatibility -f docker-compose.ci.yml up -d

Write-Host "Set connection url to environment variable"
# RabbitMQ
$env:RABBITMQ_URL = "amqp://guest:guest@localhost:5672"
# Elasticsearch
$env:ELASTICSEARCH_URL = "http://localhost:9200"
# Event Store
$env:EVENTSTORE_URL = "tcp://admin:changeit@localhost:1113"

Write-Host "Health checks - Event Store"
curl --connect-timeout 60 --retry 5 -sL "http://localhost:2113"
Write-Host "Health checks - Elasticsearch"
curl --connect-timeout 60 --retry 5 -sL "http://localhost:9200"
Write-Host "Health checks - RabbitMQ"
curl --connect-timeout 60 --retry 5 -sL "http://localhost:15672"
