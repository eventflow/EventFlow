# Up containers
docker-compose --compatibility pull
docker-compose --compatibility up -d

# Wait for containers to become healthy
while ($true) {
    $status = (. docker ps --format="{{.ID}}" | ForEach-Object { . docker inspect --format="{{.State.Health.Status}}" $_ })
    $count = $status.where{$_ -ne "healthy"}.Count
    if ($count -eq 0) {
        break
    }

    Write-Host "Containers not ready $count"
    . docker ps --format="{{.ID}}" | ForEach-Object { . docker inspect --format="{{ .Name }}: {{ .State.Health.Status }}" $_ } | ForEach-Object { Write-Host $_ }
    Start-Sleep -s 2
}

Write-Host "All containers are ready"
