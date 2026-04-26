@echo off
chcp 65001 > nul
powershell -ExecutionPolicy Bypass -File .\manage_version.ps1
