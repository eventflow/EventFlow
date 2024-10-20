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

function Invoke-WebRequestWithRetry {
    param (
        [string]$Uri,
        [int]$TimeoutSec = 360,
        [int]$RetryIntervalSec = 5,
        [int]$MaxRetries = 24
    )

    $retries = 0
    while ($retries -lt $MaxRetries) {
        try {
            Invoke-WebRequest -Uri $Uri -TimeoutSec $TimeoutSec -UseBasicParsing
            return
        } catch {
            Write-Host "Request to $Uri failed. Retrying in $RetryIntervalSec seconds..."
            Start-Sleep -Seconds $RetryIntervalSec
            $retries++
        }
    }
    throw "Failed to connect to $Uri after $MaxRetries retries."
}

# Stop any exsiting containers
Invoke-Call -ScriptBlock { docker ps -q | % { docker stop $_ } } -ErrorAction Stop
Invoke-Call -ScriptBlock { docker container prune } -ErrorAction Stop

# Up containers
Invoke-Call -ScriptBlock { docker compose --compatibility -f docker-compose.yml pull } -ErrorAction Stop
Invoke-Call -ScriptBlock { docker compose --compatibility -f docker-compose.yml up --force-recreate -d } -ErrorAction Stop

# Set connection url to environment variable
# RabbitMQ
$env:RABBITMQ_URL = "amqp://guest:guest@localhost:5672"
# Elasticsearch
$env:ELASTICSEARCH_URL = "http://localhost:9200"
# Event Store
$env:EVENTSTORE_URL = "tcp://admin:changeit@localhost:1113"

# Health checks
# EventStore
Invoke-WebRequestWithRetry -Uri "http://localhost:2113"

# Elasticsearch
Invoke-WebRequestWithRetry -Uri "http://localhost:9200"

# RabbitMQ
Invoke-WebRequestWithRetry -Uri "http://localhost:15672"
