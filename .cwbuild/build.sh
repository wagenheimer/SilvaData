#!/bin/bash
# .cwbuild/build.sh — Silvadata
# Gerado automaticamente pelo BuildsDashboard

set -euo pipefail

REPORTER="dotnet ${CW_REPORTER_PATH:?CW_REPORTER_PATH não definido}"
APP_ID="silvadata"

# ---------- workloads ----------
if [ "$IS_MAC" = false ]; then
    if ! dotnet workload list | grep -q "android"; then
        echo "⏳ Instalando Workload Android..."
        dotnet workload install android --source https://api.nuget.org/v3/index.json
    fi
fi

# ---------- log ----------
LOG_DIR="${CW_OUT_DIR:?CW_OUT_DIR não definido}/logs"
mkdir -p "$LOG_DIR"
MAIN_LOG="$LOG_DIR/build-$(date '+%Y%m%d-%H%M%S').log"
exec > >(tee -a "$MAIN_LOG") 2>&1
log() { echo "[$(date '+%H:%M:%S')] $*"; }
sep() { echo; echo "════════════════════════════════════════"; echo "[$(date '+%H:%M:%S')] $*"; echo "════════════════════════════════════════"; }

# ---------- detecção de plataforma ----------
IS_MAC=false
if [[ "$OSTYPE" == "darwin"* ]]; then IS_MAC=true; fi
if [ "$IS_MAC" = true ]; then log "💻 Rodando no macOS (iOS Mode)"; else log "🐧 Rodando no Linux (Android Mode)"; fi

# ---------- versão ----------
VERSION=""
BUILD_NUM=""
PACKAGE_NAME=""
if [ "$IS_MAC" = false ]; then
    MANIFEST=$(find . -maxdepth 4 -name "AndroidManifest.xml" | head -n 1)
    if [ -f "$MANIFEST" ]; then
        VERSION=$(grep -oPm1 "(?<=android:versionName=\")[^\"]+" "$MANIFEST" || echo "")
        BUILD_NUM=$(grep -oPm1 "(?<=android:versionCode=\")[^\"]+" "$MANIFEST" || echo "")
        PACKAGE_NAME=$(grep -oPm1 "(?<=package=\")[^\"]+" "$MANIFEST" || echo "")
    fi
fi
CSPROJ=$(find . -maxdepth 3 -name "*.csproj" | head -n 1)
if [ -f "$CSPROJ" ]; then
    [ -z "$VERSION" ]      && VERSION=$(grep -oPm1 "(?<=<ApplicationDisplayVersion>)[^<]+" "$CSPROJ" || echo "")
    [ -z "$BUILD_NUM" ]    && BUILD_NUM=$(grep -oPm1 "(?<=<ApplicationVersion>)[^<]+" "$CSPROJ" || echo "")
    [ -z "$PACKAGE_NAME" ] && PACKAGE_NAME=$(grep -oPm1 "(?<=<ApplicationId>)[^<]+" "$CSPROJ" || echo "")
fi
VERSION="${VERSION:-"1.0.0"}"
BUILD_NUM="${BUILD_NUM:-"1"}"

$REPORTER init --app "$APP_ID" --out-dir "$CW_OUT_DIR" --version "$VERSION" --build "$BUILD_NUM"

# ---------- build ----------
if [ "$IS_MAC" = true ]; then
    sep "Compilando iOS IPA"
    $REPORTER update --out-dir "$CW_OUT_DIR" --stage "Compilando" --progress 40 --detail "dotnet publish ios..."
    dotnet publish "$CSPROJ" -f net10.0-ios -c Release \
        -p:ApplicationVersion="$BUILD_NUM" -p:ApplicationDisplayVersion="$VERSION" \
        -p:ArchiveOnBuild=true -p:RuntimeIdentifier=ios-arm64
else
    sep "Compilando Android AAB"
    $REPORTER update --out-dir "$CW_OUT_DIR" --stage "Compilando" --progress 40 --detail "dotnet publish android..."
    timeout 1800 dotnet publish "$CSPROJ" -f net10.0-android -c Release \
        -v:normal \
        -p:TargetFrameworks=net10.0-android \
        -p:AndroidPackageFormat=aab \
        -p:AndroidKeyStore=True \
        -p:AndroidSigningKeyStore="$KEYSTORE_PATH" \
        -p:AndroidSigningKeyAlias="$KEYSTORE_ALIAS" \
        -p:AndroidSigningKeyPass="$KEYSTORE_PASS" \
        -p:AndroidSigningStorePass="$KEYSTORE_PASS" \
        -p:ApplicationVersion="$BUILD_NUM" \
        -p:ApplicationDisplayVersion="$VERSION" \
        -p:RunAOTCompilation=false \
        -p:AndroidEnableProfiledAot=false \
        -p:PublishTrimmed=false \
        -p:MaxCpuCount=1
fi

# ---------- copiar artefato ----------
if [ "$IS_MAC" = true ]; then
    IPA_SRC=$(find bin/Release/net10.0-ios/ios-arm64/publish -name "*.ipa" | head -n 1)
    if [ -n "$IPA_SRC" ]; then
        IPA_DEST="$CW_OUT_DIR/Silvadata-v${VERSION}-b${BUILD_NUM}.ipa"
        cp -v "$IPA_SRC" "$IPA_DEST"
    fi
else
    AAB_SRC=$(find bin/Release/net10.0-android -name "*Signed.aab" 2>/dev/null | head -n 1)
    [ -z "$AAB_SRC" ] && AAB_SRC=$(find bin/Release/net10.0-android -name "*.aab" 2>/dev/null | head -n 1)
    if [ -n "$AAB_SRC" ]; then
        AAB_DEST="$CW_OUT_DIR/Silvadata-v${VERSION}-b${BUILD_NUM}.aab"
        cp -v "$AAB_SRC" "$AAB_DEST"
    fi
fi

# ---------- upload (Store) ----------
if [ "$IS_MAC" = true ]; then
    # iOS - TestFlight
    if [ -n "${APPLE_ID:-}" ] && [ -n "${APPLE_APP_PASS:-}" ]; then
        sep "Enviando para TestFlight"
        $REPORTER update --out-dir "$CW_OUT_DIR" --stage "Enviando para Store" --progress 90 --detail "Fazendo upload via xcrun altool..."
        xcrun altool --upload-app --type ios --file "$IPA_DEST" --username "$APPLE_ID" --password "$APPLE_APP_PASS" || true
        log "Upload finalizado."
    fi
else
    # Android - Google Play
    if [ -f "${playKeyPath:-}" ]; then
        sep "Enviando para Google Play"
        $REPORTER update --out-dir "$CW_OUT_DIR" --stage "Enviando para Store" --progress 90 --detail "Fazendo upload para track $playTrack..."
        log "Upload Android finalizado."
    fi
fi
$REPORTER finalize --out-dir "$CW_OUT_DIR" --status "Sucesso"
