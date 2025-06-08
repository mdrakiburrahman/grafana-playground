# ADX Proxy

> TODO

Run from PowerShell:

```powershell
$GIT_ROOT = git rev-parse --show-toplevel
cd "$GIT_ROOT\azure-data-explorer-datasource\proxy"

$env:KUSTO_ENDPOINT = "https://rakirahman.westus2.kusto.windows.net"
$env:ASPNETCORE_URLS = "http://localhost:5005"
dotnet run
```