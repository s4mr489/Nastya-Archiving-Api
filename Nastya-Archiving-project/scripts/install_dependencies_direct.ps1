# Direct Python Installation and Setup for Nastya Archiving Project
# This script will:
# 1. Check for Python installations in common paths
# 2. Install required Python packages
# 3. Set up the necessary environment

Write-Host "Starting Python dependency installation for Nastya Archiving Project..." -ForegroundColor Cyan

# Define common Python installation paths to check
$pythonPaths = @(
    "python",
    "python3",
    "C:\Python39\python.exe",
    "C:\Python310\python.exe",
    "C:\Python311\python.exe",
    "C:\Python312\python.exe",
    "C:\Program Files\Python39\python.exe",
    "C:\Program Files\Python310\python.exe",
    "C:\Program Files\Python311\python.exe",
    "C:\Program Files\Python312\python.exe",
    "C:\Program Files (x86)\Python39\python.exe",
    "C:\Program Files (x86)\Python310\python.exe",
    "C:\Program Files (x86)\Python311\python.exe",
    "C:\Program Files (x86)\Python312\python.exe"
)

$pythonFound = $false
$pythonPath = $null
$pythonVersion = $null

# Check each path for a valid Python installation
Write-Host "Checking for Python installations..." -ForegroundColor Yellow
foreach ($path in $pythonPaths) {
    try {
        Write-Host "Trying $path..." -ForegroundColor Gray
        $pythonVersionOutput = & $path --version 2>&1
        
        if ($LASTEXITCODE -eq 0 -and $pythonVersionOutput -match "Python") {
            $pythonPath = $path
            $pythonVersion = $pythonVersionOutput
            $pythonFound = $true
            Write-Host "Found Python: $pythonVersionOutput at $pythonPath" -ForegroundColor Green
            break
        }
    }
    catch {
        # Continue checking other paths
    }
}

if (-not $pythonFound) {
    Write-Host "Python not found in common locations!" -ForegroundColor Red
    Write-Host "Would you like to download and install Python 3.11.7? (y/n)" -ForegroundColor Yellow
    $installPython = Read-Host
    
    if ($installPython -eq "y") {
        $pythonInstallerUrl = "https://www.python.org/ftp/python/3.11.7/python-3.11.7-amd64.exe"
        $pythonInstallerPath = "$env:TEMP\python-installer.exe"
        
        Write-Host "Downloading Python 3.11.7..." -ForegroundColor Yellow
        
        try {
            Invoke-WebRequest -Uri $pythonInstallerUrl -OutFile $pythonInstallerPath
            Write-Host "Download complete. Installing Python..." -ForegroundColor Yellow
            
            # Install Python with PATH option enabled and pip installed
            Start-Process -FilePath $pythonInstallerPath -ArgumentList "/quiet", "InstallAllUsers=1", "PrependPath=1", "Include_pip=1" -Wait
            
            Write-Host "Python installation completed. Please restart this script to continue." -ForegroundColor Green
            exit
        }
        catch {
            Write-Host "Failed to download or install Python: $_" -ForegroundColor Red
            Write-Host "Please download and install Python manually from: https://www.python.org/downloads/" -ForegroundColor Yellow
            Write-Host "Make sure to check 'Add Python to PATH' during installation." -ForegroundColor Yellow
            exit 1
        }
    }
    else {
        Write-Host "Please install Python manually from: https://www.python.org/downloads/" -ForegroundColor Yellow
        Write-Host "Make sure to check 'Add Python to PATH' during installation." -ForegroundColor Yellow
        exit 1
    }
}

# Install required Python packages
Write-Host "Installing required Python packages..." -ForegroundColor Yellow

# Upgrade pip first
Write-Host "Upgrading pip..." -ForegroundColor Gray
try {
    & $pythonPath -m pip install --upgrade pip
    Write-Host "Pip upgraded successfully." -ForegroundColor Green
}
catch {
    Write-Host "Error upgrading pip: $_" -ForegroundColor Red
    Write-Host "Continuing with package installation..." -ForegroundColor Yellow
}

# Install PyMuPDF
Write-Host "Installing PyMuPDF..." -ForegroundColor Gray
try {
    & $pythonPath -m pip install PyMuPDF
    Write-Host "PyMuPDF installed successfully." -ForegroundColor Green
}
catch {
    Write-Host "Error installing PyMuPDF: $_" -ForegroundColor Red
}

# Install pdfminer.six
Write-Host "Installing pdfminer.six..." -ForegroundColor Gray
try {
    & $pythonPath -m pip install pdfminer.six
    Write-Host "pdfminer.six installed successfully." -ForegroundColor Green
}
catch {
    Write-Host "Error installing pdfminer.six: $_" -ForegroundColor Red
}

# Verify installations
Write-Host "Verifying installations..." -ForegroundColor Yellow

$packagesToVerify = @(
    @{Name = "PyMuPDF"; ImportName = "fitz"},
    @{Name = "pdfminer.six"; ImportName = "pdfminer"}
)
$allInstalled = $true

foreach ($package in $packagesToVerify) {
    $testCmd = "try: import $($package.ImportName); print('$($package.Name) is installed'); except ImportError: print('$($package.Name) is NOT installed')"
    $result = & $pythonPath -c $testCmd
    
    if ($result -like "*is installed*") {
        Write-Host "$($package.Name): Installed" -ForegroundColor Green
    }
    else {
        Write-Host "$($package.Name): NOT installed" -ForegroundColor Red
        $allInstalled = $false
    }
}

# Update PATH environment variable if Python directory is not in PATH
$pythonDir = Split-Path -Parent $pythonPath
$currentPath = [Environment]::GetEnvironmentVariable("PATH", [EnvironmentVariableTarget]::Machine)
if (-not $currentPath.Contains($pythonDir)) {
    try {
        Write-Host "Adding Python directory to system PATH..." -ForegroundColor Yellow
        [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$pythonDir", [EnvironmentVariableTarget]::Machine)
        Write-Host "Python added to system PATH. You may need to restart your application or system." -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to add Python to system PATH: $_" -ForegroundColor Red
        Write-Host "Please add this directory to your PATH manually: $pythonDir" -ForegroundColor Yellow
    }
}

# Final message
if ($allInstalled) {
    Write-Host "All required Python packages have been successfully installed!" -ForegroundColor Green
    
    # Create a simple environment file that the application can check
    $envInfo = @{
        PythonPath = $pythonPath
        PythonVersion = $pythonVersion
        PyMuPDFInstalled = $true
        PdfminerInstalled = $true
        InstallationDate = [DateTime]::Now.ToString("o")
    } | ConvertTo-Json

    $envFilePath = Join-Path -Path $PSScriptRoot -ChildPath "..\python_env_info.json"
    $envInfo | Out-File -FilePath $envFilePath
    Write-Host "Environment information saved to: $envFilePath" -ForegroundColor Cyan
}
else {
    Write-Host "Some packages could not be installed. Please review the output and try again." -ForegroundColor Yellow
    Write-Host "You may need to run this script as administrator." -ForegroundColor Yellow
}

# Create a bat version for easier execution
$batScriptPath = Join-Path -Path $PSScriptRoot -ChildPath "install_dependencies_direct.bat"
$scriptPath = $MyInvocation.MyCommand.Path
$scriptContent = "@echo off`r`necho Running Python dependency installer...`r`npowershell.exe -ExecutionPolicy Bypass -File `"$scriptPath`"`r`npause"
[System.IO.File]::WriteAllText($batScriptPath, $scriptContent)

Write-Host "A .bat file has been created for easier execution: $batScriptPath" -ForegroundColor Cyan
Write-Host "Setup complete!" -ForegroundColor Cyan