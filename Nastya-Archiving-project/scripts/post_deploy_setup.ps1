# Post-deployment setup script for Nastya Archiving Project
# This script verifies and sets up the required Python environment after deployment

Write-Host "Starting post-deployment verification and setup for Nastya Archiving Project..." -ForegroundColor Cyan

$deploymentDirectory = $PSScriptRoot | Split-Path -Parent
$pythonScriptsExist = Test-Path -Path "$deploymentDirectory\*.py"
$tessdataExists = Test-Path -Path "$deploymentDirectory\tessdata"
$scriptsExist = Test-Path -Path "$deploymentDirectory\scripts"

Write-Host "Checking deployment directory: $deploymentDirectory"
Write-Host "Python scripts exist: $pythonScriptsExist"
Write-Host "Tessdata directory exists: $tessdataExists"
Write-Host "Scripts directory exists: $scriptsExist"

# Check for Ghostscript installation
Write-Host "Checking for Ghostscript installation..." -ForegroundColor Cyan
$ghostscriptPath = $null

# Check common Ghostscript installation paths
$possiblePaths = @(
    "C:\Program Files\gs\gs*\bin\gswin64c.exe",
    "C:\Program Files (x86)\gs\gs*\bin\gswin32c.exe"
)

foreach ($path in $possiblePaths) {
    $foundPaths = Get-Item -Path $path -ErrorAction SilentlyContinue
    if ($foundPaths) {
        $ghostscriptPath = $foundPaths | Sort-Object -Property FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
        break
    }
}

if ($ghostscriptPath) {
    Write-Host "Ghostscript found at: $ghostscriptPath" -ForegroundColor Green
    # Add Ghostscript directory to PATH environment variable if not already there
    $gsDir = Split-Path -Parent $ghostscriptPath
    $currentPath = [Environment]::GetEnvironmentVariable("PATH", [EnvironmentVariableTarget]::Machine)
    if (-not $currentPath.Contains($gsDir)) {
        Write-Host "Adding Ghostscript directory to PATH..." -ForegroundColor Yellow
        try {
            [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$gsDir", [EnvironmentVariableTarget]::Machine)
            Write-Host "Ghostscript directory added to PATH successfully." -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to add Ghostscript to PATH. You may need to do this manually." -ForegroundColor Red
            Write-Host "Add this directory to your PATH environment variable: $gsDir" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "Ghostscript not found. PDF to image conversion may not work properly." -ForegroundColor Red
    Write-Host "Please install Ghostscript from https://ghostscript.com/releases/gsdnld.html" -ForegroundColor Yellow
    Write-Host "After installation, make sure the Ghostscript bin directory is in your system PATH." -ForegroundColor Yellow
}

# Verify Python installation
try {
    $pythonVersion = python --version 2>&1
    Write-Host "Python version: $pythonVersion" -ForegroundColor Green
}
catch {
    Write-Host "Python is not installed or not in PATH. Please install Python 3.8 or higher." -ForegroundColor Red
    Write-Host "Visit https://www.python.org/downloads/ to download and install Python." -ForegroundColor Yellow
}

# Verify required Python packages
Write-Host "Checking required Python packages..."
$requiredPackages = @("PyMuPDF", "pdfminer.six")
foreach ($package in $requiredPackages) {
    try {
        $checkResult = python -c "import $package; print(f'$package is installed')" 2>&1
        if ($checkResult -like "*is installed*") {
            Write-Host "$package: Installed" -ForegroundColor Green
        } else {
            Write-Host "$package: Not installed" -ForegroundColor Red
            Write-Host "Installing $package..."
            python -m pip install $package
        }
    } 
    catch {
        Write-Host "$package: Not installed" -ForegroundColor Red
        Write-Host "Installing $package..."
        python -m pip install $package
    }
}

# Verify Tesseract data files
if ($tessdataExists) {
    $arabicTraineddata = Test-Path -Path "$deploymentDirectory\tessdata\ara.traineddata"
    if ($arabicTraineddata) {
        Write-Host "Arabic language training data for Tesseract found." -ForegroundColor Green
    } else {
        Write-Host "Arabic language training data for Tesseract not found." -ForegroundColor Red
        Write-Host "Running setup_tessdata.ps1 script..."
        & "$deploymentDirectory\scripts\setup_tessdata.ps1"
    }
} else {
    Write-Host "Tessdata directory not found. Creating directory and downloading language files..." -ForegroundColor Yellow
    & "$deploymentDirectory\scripts\setup_tessdata.ps1"
}

Write-Host "Setup complete. The application should now be ready to use." -ForegroundColor Cyan
Write-Host "If you encounter issues with PDF text extraction, please run the install_python_dependencies.ps1 script manually." -ForegroundColor Yellow
Write-Host "If you see errors about Ghostscript, make sure it is properly installed and in your system PATH." -ForegroundColor Yellow