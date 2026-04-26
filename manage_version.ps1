# Script para Gerenciamento de Versão e Build (.NET MAUI)
# Salve como manage_version.ps1 na raiz do projeto

$projFile = Get-ChildItem *.csproj | Select-Object -First 1
$manifestPath = "Platforms/Android/AndroidManifest.xml"
$plistPath = "Platforms/iOS/Info.plist"

if (-not (Test-Path $projFile)) { 
    Write-Host "Erro: Arquivo .csproj não encontrado!" -ForegroundColor Red
    exit 
}

function Get-CurrentVersion {
    $manifest = Get-Content $manifestPath -Raw
    $versionName = ([regex]'android:versionName="([^"]+)"').Match($manifest).Groups[1].Value
    $versionCode = ([regex]'android:versionCode="([^"]+)"').Match($manifest).Groups[1].Value
    return @{ Name = $versionName; Code = $versionCode }
}

function Update-VersionFiles($newName, $newCode) {
    # Atualizar AndroidManifest.xml
    $content = Get-Content $manifestPath -Raw
    $content = $content -replace 'android:versionName="[^"]+"', "android:versionName=`"$newName`""
    $content = $content -replace 'android:versionCode="[^"]+"', "android:versionCode=`"$newCode`""
    Set-Content $manifestPath $content

    # Atualizar Info.plist
    $content = Get-Content $plistPath -Raw
    $content = $content -replace '(?s)(<key>CFBundleShortVersionString</key>\s*<string>)[^<]+(</string>)', "${1}$newName${2}"
    $content = $content -replace '(?s)(<key>CFBundleVersion</key>\s*<string>)[^<]+(</string>)', "${1}$newCode${2}"
    Set-Content $plistPath $content

    # Atualizar .csproj
    $content = Get-Content $projFile -Raw
    $content = $content -replace '<ApplicationDisplayVersion>[^<]+</ApplicationDisplayVersion>', "<ApplicationDisplayVersion>$newName</ApplicationDisplayVersion>"
    $content = $content -replace '<ApplicationVersion>[^<]+</ApplicationVersion>', "<ApplicationVersion>$newCode</ApplicationVersion>"
    Set-Content $projFile $content
    
    Write-Host "`nSucesso! Atualizado para Versão $newName (Build $newCode)" -ForegroundColor Green
}

function Show-Menu {
    $v = Get-CurrentVersion
    Clear-Host
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " PROJETO: $($projFile.BaseName)" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " Versão Atual: $($v.Name)"
    Write-Host " Build Atual:  $($v.Code)"
    Write-Host "----------------------------------------"
    Write-Host "1 - Incrementar Versão e Build Number"
    Write-Host "2 - Incrementar Build Number"
    Write-Host "3 - Publish Archive for iOS"
    Write-Host "q - Sair"
    Write-Host "----------------------------------------"
    
    $choice = Read-Host "Escolha uma opção"
    
    switch ($choice) {
        "1" {
            $parts = $v.Name -split '\.'
            if ($parts.Length -ge 1) {
                $parts[-1] = [int]$parts[-1] + 1
                $newName = $parts -join '.'
                $newCode = [int]$v.Code + 1
                Update-VersionFiles $newName $newCode
            }
            pause
        }
        "2" {
            $newCode = [int]$v.Code + 1
            Update-VersionFiles $v.Name $newCode
            pause
        }
        "3" {
            Write-Host "`nIniciando Publish para iOS (Release)..." -ForegroundColor Yellow
            dotnet publish -f net10.0-ios -c Release /p:ArchiveOnBuild=true
            pause
        }
        "q" { return }
        default { 
            Write-Host "Opção inválida!" -ForegroundColor Red
            Start-Sleep -Seconds 1
        }
    }
    Show-Menu
}

Show-Menu
