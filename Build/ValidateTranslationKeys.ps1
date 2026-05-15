# ValidateTranslationKeys.ps1
# 1. Detecta chaves duplicadas em todos os .resx do projeto (erro de build)
# 2. Verifica se todas as chaves usadas em {safe:Translate Chave} existem no .resx base (erro de build)

param(
    [string]$ProjectDir,
    [string]$ResxFile
)

$ErrorActionPreference = "Stop"
$errors = [System.Collections.Generic.List[string]]::new()
$projectDirResolved = (Resolve-Path $ProjectDir).Path

# ── 1. Duplicatas em todos os .resx ──────────────────────────────────────────
$resxFiles = Get-ChildItem -Path $ProjectDir -Recurse -Filter "*.resx" |
    Where-Object { $_.FullName -notmatch '[/\\](obj|bin)[/\\]' }

foreach ($rf in $resxFiles) {
    $content = Get-Content $rf.FullName -Raw -Encoding UTF8
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $rel = $rf.FullName.Replace($projectDirResolved, "").TrimStart('\','/')
    foreach ($m in [regex]::Matches($content, '<data name="([^"]+)"')) {
        $key = $m.Groups[1].Value
        if (-not $seen.Add($key)) {
            $line = ($content.Substring(0, $m.Index) -split "`n").Count
            $errors.Add("$($rel)($line): error TRANS002: Chave duplicada no .resx: '$key'")
        }
    }
}

# ── 2. Chaves faltantes no .resx base ────────────────────────────────────────
$resx = [xml](Get-Content $ResxFile -Encoding UTF8)
$resxKeys = $resx.root.data | Where-Object { $_.name -notmatch '^(Name|Color|Bitmap|Icon)\d+$' } | ForEach-Object { $_.name }
$resxSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($k in $resxKeys) { $resxSet.Add($k) | Out-Null }

$xamlFiles = Get-ChildItem -Path $ProjectDir -Recurse -Filter "*.xaml" |
    Where-Object { $_.FullName -notmatch '[/\\](obj|bin)[/\\]' -and -not $_.PSIsContainer }

foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $rel = $file.FullName.Replace($projectDirResolved, "").TrimStart('\','/')
    foreach ($m in [regex]::Matches($content, '\{safe:Translate\s+([\wÀ-ɏ]+)\}')) {
        $key = $m.Groups[1].Value
        if (-not $resxSet.Contains($key)) {
            $line = ($content.Substring(0, $m.Index) -split "`n").Count
            $errors.Add("$($rel)($line): error TRANS001: Chave de tradução ausente no .resx: '$key'")
        }
    }
}

# ── Resultado ─────────────────────────────────────────────────────────────────
if ($errors.Count -gt 0) {
    foreach ($e in $errors) { Write-Host $e }
    Write-Host ""
    $missing = ($errors | Where-Object { $_ -match 'TRANS001' }).Count
    $dupes   = ($errors | Where-Object { $_ -match 'TRANS002' }).Count
    if ($missing -gt 0) { Write-Host "TRANS001: $missing chave(s) ausente(s). Adicione ao Localization.resx antes de compilar." }
    if ($dupes   -gt 0) { Write-Host "TRANS002: $dupes chave(s) duplicada(s). Remova as duplicatas dos arquivos .resx." }
    exit 1
}

Write-Host "ValidateTranslationKeys: OK - $($resxSet.Count) chaves, sem faltantes, sem duplicatas."
exit 0
