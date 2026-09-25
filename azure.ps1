npm run build

$stage = Join-Path (Get-Location) '.azure-package'
$zip = Join-Path (Get-Location) 'EventCalendar-azure-deploy.zip'
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }

dotnet publish EventCalendar.API/EventCalendar.API.csproj `
  --configuration Release --output $stage

$wwwroot = Join-Path $stage 'wwwroot'
if (Test-Path -LiteralPath $wwwroot) { Remove-Item -LiteralPath $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Path $wwwroot | Out-Null
Copy-Item -Path (Join-Path (Get-Location) 'dist\*') -Destination $wwwroot -Recurse -Force

$developmentSettings = Join-Path $stage 'appsettings.Development.json'
if (Test-Path -LiteralPath $developmentSettings) {
  Remove-Item -LiteralPath $developmentSettings -Force
}

$data = Join-Path $stage 'Data'
if (Test-Path -LiteralPath $data) { Remove-Item -LiteralPath $data -Recurse -Force }
New-Item -ItemType Directory -Path $data | Out-Null
Copy-Item -LiteralPath (Join-Path (Get-Location) 'EventCalendar.API\Data\events.json') `
  -Destination (Join-Path $data 'events.json') -Force

if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal