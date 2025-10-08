@echo off
echo Running post-deployment setup script for Nastya Archiving Project...
powershell -ExecutionPolicy Bypass -File "%~dp0post_deploy_setup.ps1"
pause