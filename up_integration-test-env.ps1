$ErrorActionPreference = "Stop"

function Invoke-Call {
    param (
        [scriptblock]$ScriptBlock,
        [string]$ErrorAction = $ErrorActionPreference
    )
    & @ScriptBlock
    if (($lastexitcode -ne 0) -and $ErrorAction -eq "Stop") {
        exit $lastexitcode
    }
}

# Up containers
Invoke-Call -ScriptBlock { docker compose --compatibility -f docker-compose.ci.yml pull } -ErrorAction Stop
Invoke-Call -ScriptBlock { docker compose --compatibility -f docker-compose.ci.yml up -d } -ErrorAction Stop

# Set connection url to environment variable
# RabbitMQ
$env:RABBITMQ_URL = "amqp://guest:guest@localhost:5672"
# Elasticsearch
$env:ELASTICSEARCH_URL = "http://localhost:9200"
# Event Store
$env:EVENTSTORE_URL = "tcp://admin:changeit@localhost:1113"

# Health checks
# EventStore
Invoke-WebRequest -Uri "http://localhost:2113" -TimeoutSec 60 -RetryIntervalSec 5 -Method Get -UseBasicParsing

# Elasticsearch
Invoke-WebRequest -Uri "http://localhost:9200" -TimeoutSec 60 -RetryIntervalSec 5 -Method Get -UseBasicParsing

# RabbitMQ
Invoke-WebRequest -Uri "http://localhost:15672" -TimeoutSec 60 -RetryIntervalSec 5 -Method Get -UseBasicParsing
