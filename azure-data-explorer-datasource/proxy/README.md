# ADX Proxy

A REST API proxy for Azure Data Explorer.

Run from pwsh:

```powershell
$GIT_ROOT = git rev-parse --show-toplevel
cd "$GIT_ROOT\azure-data-explorer-datasource\proxy"

$env:KUSTO_ENDPOINT = "https://rakirahman.westus2.kusto.windows.net"
$env:ASPNETCORE_URLS = "http://localhost:5005"
dotnet run
```

Run from bash:

```bash
GIT_ROOT=$(git rev-parse --show-toplevel)
cd "$GIT_ROOT/azure-data-explorer-datasource/proxy"

export KUSTO_ENDPOINT="https://rakirahman.westus2.kusto.windows.net"
export ASPNETCORE_URLS="http://localhost:5005"
dotnet run
```

Run from docker:

```bash
docker build -t adx-proxy .

docker run \
    -e KUSTO_ENDPOINT="https://rakirahman.westus2.kusto.windows.net" \
    -e ASPNETCORE_URLS="http://localhost:5005" \
    -p 5005:5005 \
    adx-proxy
```

In the container (e.g. from Docker Desktop), run:

```bash
wget -qO- http://localhost:5005/healthz
# Healthy
```