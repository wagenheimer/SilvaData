# ValidateTranslationKeys.ps1
# Verifica se todas as chaves usadas em {safe:Translate Chave} existem no .resx
# Retorna exit code 1 se houver chaves faltantes (causa erro de build)

param(
    [string]$ProjectDir,
    [string]$ResxFile
)

$ErrorActionPreference = "Stop"

# Coleta todas as chaves do .resx
$resx = [xml](Get-Content $ResxFile -Encoding UTF8)
$resxKeys = $resx.root.data | Where-Object { $_.name -notmatch '^(Name|Color|Bitmap|Icon)\d+$' } | ForEach-Object { $_.name }
$resxSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($k in $resxKeys) { $resxSet.Add($k) | Out-Null }

# Coleta todas as chaves usadas nos XAMLs via {safe:Translate Chave}
$xamlFiles = Get-ChildItem -Path $ProjectDir -Recurse -Filter "*.xaml" |
    Where-Object { $_.FullName -notmatch '[/\\](obj|bin)[/\\]' -and -not $_.PSIsContainer }
$errors = [System.Collections.Generic.List[string]]::new()

foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $matches = [regex]::Matches($content, '\{safe:Translate\s+([\wÀ-ɏ]+)\}')
    foreach ($m in $matches) {
        $key = $m.Groups[1].Value
        if (-not $resxSet.Contains($key)) {
            # Descobrir número da linha aproximado
            $line = ($content.Substring(0, $m.Index) -split "`n").Count
            $rel = $file.FullName.Replace((Resolve-Path $ProjectDir).Path, "").TrimStart('\','/')
            $errors.Add("$($rel)($line): error TRANS001: Chave de tradução ausente no .resx: '$key'")
        }
    }
}

if ($errors.Count -gt 0) {
    foreach ($e in $errors) { Write-Host $e }
    Write-Host ""
    Write-Host "TRANS001: $($errors.Count) chave(s) de tradução ausente(s). Adicione ao arquivo Localization.resx antes de compilar."
    exit 1
}

Write-Host "ValidateTranslationKeys: OK - todas as $($resxSet.Count) chaves validadas."
exit 0
