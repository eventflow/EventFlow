# Up containers
docker-compose --compatibility -f Scripts/docker-compose.windows.yml pull
docker-compose --compatibility -f Scripts/docker-compose.windows.yml up -d

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
# RabbitMQ
echo "Checking connection to RabbitMQ"
curl --connect-timeout 360 --retry 5 -sL "http://localhost:15672"
echo "RabbitMQ running"

# EventStore
echo "Checking connection to EventStore"
curl --connect-timeout 360 --retry 5 -sL "http://localhost:2113"
echo "EventStore running"

# Elasticsearch
echo "Checking connection to Elasticsearch"
curl --connect-timeout 360 --retry 5 -sL "http://localhost:9200"
echo "Elasticsearch running"
