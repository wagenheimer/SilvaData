# ValidateTranslationKeys.ps1
# Uso normal (build):  pwsh -NonInteractive -File ValidateTranslationKeys.ps1 -ProjectDir . -ResxFile ...
# Uso interativo:      pwsh -File ValidateTranslationKeys.ps1 -ProjectDir . -ResxFile ... -Fix
#
# Erros emitidos:
#   TRANS001 - chave usada em {safe:Translate X} não existe no .resx base
#   TRANS002 - chave duplicada em algum .resx (build falha; use -Fix para resolver interativamente)

param(
    [string]$ProjectDir,
    [string]$ResxFile,
    [switch]$Fix
)

$ErrorActionPreference = "Stop"
$errors = [System.Collections.Generic.List[string]]::new()
$projectDirResolved = (Resolve-Path $ProjectDir).Path

# ── helpers de cor ────────────────────────────────────────────────────────────
function Write-Header($msg) { Write-Host "`n$msg" -ForegroundColor Cyan }
function Write-Ok($msg)     { Write-Host $msg -ForegroundColor Green }
function Write-Warn($msg)   { Write-Host $msg -ForegroundColor Yellow }
function Write-Err($msg)    { Write-Host $msg -ForegroundColor Red }

# ── 1. Duplicatas em todos os .resx ──────────────────────────────────────────
$resxFiles = Get-ChildItem -Path $ProjectDir -Recurse -Filter "*.resx" |
    Where-Object { $_.FullName -notmatch '[/\\](obj|bin)[/\\]' }

# Coleta duplicatas: { resxPath -> { key -> [valores] } }
$dupeMap = [ordered]@{}

foreach ($rf in $resxFiles) {
    $content = Get-Content $rf.FullName -Raw -Encoding UTF8
    $rel = $rf.FullName.Replace($projectDirResolved, "").TrimStart('\','/')

    # Extrai todos os blocos <data name="..."><value>...</value></data>
    $blocks = [regex]::Matches($content, '(?s)<data name="([^"]+)"[^>]*>\s*<value>(.*?)</value>')
    $keyValues = [ordered]@{} # key -> list of values

    foreach ($b in $blocks) {
        $key = $b.Groups[1].Value
        $val = $b.Groups[2].Value.Trim()
        if (-not $keyValues.Contains($key)) { $keyValues[$key] = [System.Collections.Generic.List[string]]::new() }
        $keyValues[$key].Add($val)
    }

    foreach ($key in $keyValues.Keys) {
        if ($keyValues[$key].Count -gt 1) {
            $line = ($content.Substring(0, ($content.IndexOf("name=""$key"""))) -split "`n").Count
            $errors.Add("$($rel)($line): error TRANS002: Chave duplicada no .resx: '$key'")
            if (-not $dupeMap.Contains($rf.FullName)) { $dupeMap[$rf.FullName] = [ordered]@{} }
            $dupeMap[$rf.FullName][$key] = $keyValues[$key]
        }
    }
}

# ── 2. Chaves faltantes no .resx base ────────────────────────────────────────
$resxXml = [xml](Get-Content $ResxFile -Encoding UTF8)
$resxKeys = $resxXml.root.data | Where-Object { $_.name -notmatch '^(Name|Color|Bitmap|Icon)\d+$' } | ForEach-Object { $_.name }
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

# ── Sem erros ─────────────────────────────────────────────────────────────────
if ($errors.Count -eq 0) {
    Write-Ok "ValidateTranslationKeys: OK - $($resxSet.Count) chaves, sem faltantes, sem duplicatas."
    exit 0
}

# ── Exibe erros ───────────────────────────────────────────────────────────────
$missingCount = ($errors | Where-Object { $_ -match 'TRANS001' }).Count
$dupesCount   = ($errors | Where-Object { $_ -match 'TRANS002' }).Count

foreach ($e in $errors) { Write-Err $e }
Write-Host ""

if ($missingCount -gt 0) {
    Write-Err "TRANS001: $missingCount chave(s) ausente(s). Adicione ao Localization.resx antes de compilar."
}
if ($dupesCount -gt 0) {
    Write-Warn "TRANS002: $dupesCount chave(s) duplicada(s) encontrada(s)."
    Write-Warn "  Chaves duplicadas no .resx fazem o ResourceManager retornar um valor imprevisível"
    Write-Warn "  (depende da ordem de leitura e pode variar entre Debug e Release/AOT)."
    Write-Warn "  Execute o script com -Fix para resolver interativamente:"
    Write-Warn "  pwsh -File Build\ValidateTranslationKeys.ps1 -ProjectDir . -ResxFile Resources\Localization\Localization.resx -Fix"
}

# ── Modo -Fix: resolve duplicatas interativamente ─────────────────────────────
if ($Fix -and $dupesCount -gt 0) {
    Write-Header "=== MODO -Fix: resolução interativa de duplicatas ==="

    foreach ($resxPath in $dupeMap.Keys) {
        $rel = $resxPath.Replace($projectDirResolved, "").TrimStart('\','/')
        Write-Header "Arquivo: $rel"

        foreach ($key in $dupeMap[$resxPath].Keys) {
            $values = $dupeMap[$resxPath][$key]
            Write-Host ""
            Write-Warn "  Chave duplicada: '$key' ($($values.Count) ocorrências)"
            Write-Host ""

            for ($i = 0; $i -lt $values.Count; $i++) {
                Write-Host "  [$($i+1)] `"$($values[$i])`""
            }
            Write-Host "  [s] Manter PRIMEIRA ocorrência e remover as demais"
            Write-Host "  [i] Ignorar (não alterar este arquivo agora)"
            Write-Host ""

            $choice = $null
            while ($null -eq $choice) {
                $input = Read-Host "  Escolha [1-$($values.Count) / s / i]"
                $input = $input.Trim().ToLower()
                if ($input -eq 's') {
                    $choice = 0  # índice 0 = primeira
                } elseif ($input -eq 'i') {
                    $choice = -1
                } elseif ($input -match '^\d+$') {
                    $idx = [int]$input - 1
                    if ($idx -ge 0 -and $idx -lt $values.Count) { $choice = $idx }
                }
                if ($null -eq $choice) { Write-Warn "  Opção inválida, tente novamente." }
            }

            if ($choice -ge 0) {
                $keepValue = $values[$choice]

                # Reescreve o arquivo mantendo só a ocorrência escolhida
                $content = Get-Content $resxPath -Raw -Encoding UTF8
                $first = $true
                $content = [regex]::Replace($content, "(?s)<data name=""$([regex]::Escape($key))""[^>]*>.*?</data>", {
                    param($m)
                    if ($first) {
                        $first = $false
                        # Substitui o valor pelo escolhido
                        return [regex]::Replace($m.Value, '(?s)<value>.*?</value>', "<value>$keepValue</value>")
                    }
                    return ''  # remove ocorrências extras
                })
                [System.IO.File]::WriteAllText($resxPath, $content, [System.Text.Encoding]::UTF8)
                Write-Ok "  → Mantido: `"$keepValue`""
            } else {
                Write-Warn "  → Ignorado."
            }
        }
    }

    Write-Header "=== Revalidando após correções... ==="
    & $MyInvocation.MyCommand.Path -ProjectDir $ProjectDir -ResxFile $ResxFile
    exit $LASTEXITCODE
}

exit 1
