@echo off
chcp 65001 > nul
powershell -NoProfile -ExecutionPolicy Bypass -File manage_version.ps1 %*
