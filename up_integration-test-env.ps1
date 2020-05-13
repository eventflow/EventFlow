# Up containers
docker-compose --compatibility -f docker-compose.ci.yml pull
docker-compose --compatibility -f docker-compose.ci.yml up -d

# Install curl
cinst curl -y --no-progress
sal curl (Join-Path $env:ChocolateyInstall "bin\curl.exe") -O AllScope

# Set connection url to environment variable
# RabbitMQ
$env:RABBITMQ_URL = "amqp://guest:guest@localhost:5672"
# Elasticsearch
$env:ELASTICSEARCH_URL = "http://localhost:9200"
# Event Store
$env:EVENTSTORE_URL = "tcp://admin:changeit@localhost:1113"

# Health checks
# Event Store
curl --connect-timeout 60 --retry 5 -sL "http://localhost:2113"
# Elasticsearch
curl --connect-timeout 60 --retry 5 -sL "http://localhost:9200"
# RabbitMQ
curl --connect-timeout 60 --retry 5 -sL "http://localhost:15672"
