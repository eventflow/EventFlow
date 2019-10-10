# Switch to linux containers
[Environment]::SetEnvironmentVariable("LCOW_SUPPORTED", "1", "Machine")
Restart-Service docker

# Up containers
docker-compose -f docker-compose.ci.yml pull
docker-compose -f docker-compose.ci.yml up -d

# Install curl
cinst curl -y --no-progress
sal curl (Join-Path $env:ChocolateyInstall "bin\curl.exe") -O AllScope

# Set connection url to environment variable
# RabbitMQ
$rabbitmq_ip = Get-Container-Ip rabbitmq-ef
$env:RABBITMQ_URL = "amqp://guest:guest@$localhost:5672"
# Elasticsearch
$elasticsearch_ip = Get-Container-Ip elasticsearch-ef
$env:ELASTICSEARCH_URL = "http://localhost:9200"
# Event Store
$eventstore_ip = Get-Container-Ip eventstore-ef
$env:EVENTSTORE_URL = "tcp://admin:changeit@$localhost:1113"

# Helth check
# Event Store
curl --connect-timeout 60 --retry 5 -sL "http://${eventstore_ip}:2113"
# Elasticsearch
curl --connect-timeout 60 --retry 5 -sL "http://${elasticsearch_ip}:9200"
# RabbitMQ
curl --connect-timeout 60 --retry 5 -sL "http://${rabbitmq_ip}:15672"