#!/bin/bash

# manage.sh - Script de gerenciamento de versão para macOS/Linux
# Salve na raiz do projeto e dê permissão de execução: chmod +x manage.sh

PROJ_FILE=$(ls *.csproj | head -n 1)
MANIFEST_PATH="Platforms/Android/AndroidManifest.xml"
PLIST_PATH="Platforms/iOS/Info.plist"

if [ ! -f "$PROJ_FILE" ]; then
    echo "Erro: Arquivo .csproj nao encontrado!"
    exit 1
fi

get_version() {
    VERSION_NAME=$(grep -o 'android:versionName="[^"]*"' "$MANIFEST_PATH" | cut -d'"' -f2)
    VERSION_CODE=$(grep -o 'android:versionCode="[^"]*"' "$MANIFEST_PATH" | cut -d'"' -f2)
}

update_files() {
    local NEW_NAME=$1
    local NEW_CODE=$2

    # AndroidManifest.xml
    sed -i '' "s/android:versionName=\"[^\"]*\"/android:versionName=\"$NEW_NAME\"/" "$MANIFEST_PATH"
    sed -i '' "s/android:versionCode=\"[^\"]*\"/android:versionCode=\"$NEW_CODE\"/" "$MANIFEST_PATH"

    # Info.plist (usando perl para lidar com regex complexo em macOS)
    perl -i -pe "BEGIN{undef $/;} s/(<key>CFBundleShortVersionString<\/key>\s*<string>)[^<]*(<\/string>)/\${1}$NEW_NAME\${2}/g" "$PLIST_PATH"
    perl -i -pe "BEGIN{undef $/;} s/(<key>CFBundleVersion<\/key>\s*<string>)[^<]*(<\/string>)/\${1}$NEW_CODE\${2}/g" "$PLIST_PATH"

    # .csproj
    sed -i '' "s/<ApplicationDisplayVersion>[^<]*<\/ApplicationDisplayVersion>/<ApplicationDisplayVersion>$NEW_NAME<\/ApplicationDisplayVersion>/" "$PROJ_FILE"
    sed -i '' "s/<ApplicationVersion>[^<]*<\/ApplicationVersion>/<ApplicationVersion>$NEW_CODE<\/ApplicationVersion>/" "$PROJ_FILE"

    echo "Sucesso! Atualizado para Versao $NEW_NAME (Build $NEW_CODE)"
}

get_codesign_info() {
    CODESIGN_KEY=$(grep -o '<CodesignKey>[^<]*</CodesignKey>' "$PROJ_FILE" | sed 's|<CodesignKey>||;s|</CodesignKey>||')
    CODESIGN_PROV=$(grep -o '<CodesignProvision>[^<]*</CodesignProvision>' "$PROJ_FILE" | sed 's|<CodesignProvision>||;s|</CodesignProvision>||')
}

execute_action() {
    local choice=$1
    case $choice in
        1)
            IFS='.' read -ra ADDR <<< "$VERSION_NAME"
            LAST_INDEX=$((${#ADDR[@]} - 1))
            ADDR[$LAST_INDEX]=$((ADDR[$LAST_INDEX] + 1))
            NEW_NAME=$(IFS='.'; echo "${ADDR[*]}")
            NEW_CODE=$((VERSION_CODE + 1))
            update_files "$NEW_NAME" "$NEW_CODE"
            if [ -z "$ACTION" ]; then read -p "Pressione Enter para continuar..."; fi
            ;;
        2)
            NEW_CODE=$((VERSION_CODE + 1))
            update_files "$VERSION_NAME" "$NEW_CODE"
            if [ -z "$ACTION" ]; then read -p "Pressione Enter para continuar..."; fi
            ;;
        3)
            get_codesign_info
            echo "Iniciando Publish para iOS (Release)..."
            CMD="dotnet publish -f net10.0-ios -c Release -r ios-arm64 /p:ArchiveOnBuild=true"
            if [ ! -z "$CODESIGN_KEY" ]; then CMD="$CMD /p:CodesignKey=\"$CODESIGN_KEY\""; fi
            if [ ! -z "$CODESIGN_PROV" ]; then CMD="$CMD /p:CodesignProvision=\"$CODESIGN_PROV\""; fi
            echo "Executando: $CMD"
            eval $CMD
            if [ -z "$ACTION" ]; then read -p "Pressione Enter para continuar..."; fi
            ;;
        q)
            exit 0
            ;;
        *)
            if [ -z "$ACTION" ]; then
                echo "Opcao invalida!"
                sleep 1
            fi
            ;;
    esac
}

ACTION=$1
if [ ! -z "$ACTION" ]; then
    get_version
    execute_action $ACTION
    exit 0
fi

while true; do
    get_version
    clear
    echo "========================================"
    echo " PROJETO: ${PROJ_FILE%.*}"
    echo "========================================"
    echo " Versao Atual: $VERSION_NAME"
    echo " Build Atual:  $VERSION_CODE"
    echo "----------------------------------------"
    echo "1 - Incrementar Versao e Build Number"
    echo "2 - Incrementar Build Number"
    echo "3 - Publish Archive for iOS"
    echo "q - Sair"
    echo "----------------------------------------"
    read -p "Escolha uma opcao: " choice
    execute_action $choice
done
