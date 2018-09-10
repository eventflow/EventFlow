# functions
Function Get-Container-Ip($containername)
{
	docker inspect -f "{{ .NetworkSettings.Networks.nat.IPAddress }}" $containername
}
# end functions

# Up containers
docker-compose -f docker-compose.ci.yml up -d

# Set connection url to environment variable
# RabbitMQ
$hostip = Get-Container-Ip rabbitmq-ef
$env:RABBITMQ_URL = "amqp://guest:guest@${hostip}:5672"
# Elasticsearch
$hostip = Get-Container-Ip elasticsearch-ef
$env:ELASTICSEARCH_URL = "http://${hostip}:9200"
# Event Store
$hostip = Get-Container-Ip eventstore-ef
$env:EVENTSTORE_URL = "tcp://admin:changeit@${hostip}:1113"