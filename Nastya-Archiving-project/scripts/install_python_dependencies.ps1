# Script to install required Python dependencies for Nastya-Archiving-project PDF text extraction
# This script installs PyMuPDF and pdfminer.six, which are required by the ValuesController.cs

Write-Host "======================================================="
Write-Host "    Installing Python dependencies for PDF extraction"
Write-Host "======================================================="
Write-Host ""

# Check if Python is installed
try {
    $pythonVersion = python --version 2>&1
    Write-Host "Found Python: $pythonVersion"
}
catch {
    Write-Host "Error: Python is not installed or not in PATH." -ForegroundColor Red
    Write-Host "Please install Python from https://www.python.org/downloads/" -ForegroundColor Red
    Write-Host "Make sure to check 'Add Python to PATH' during installation." -ForegroundColor Red
    exit 1
}

# Check if pip is available
try {
    $pipVersion = python -m pip --version 2>&1
    Write-Host "Found pip: $pipVersion"
}
catch {
    Write-Host "Error: pip is not available." -ForegroundColor Red
    Write-Host "Trying to install pip..." -ForegroundColor Yellow
    
    try {
        python -m ensurepip
        $pipVersion = python -m pip --version 2>&1
        Write-Host "Successfully installed pip: $pipVersion" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to install pip. Please install it manually." -ForegroundColor Red
        exit 1
    }
}

# Function to install a package if not already installed
function Install-PythonPackage($packageName) {
    Write-Host ""
    Write-Host "Checking for $packageName..." -NoNewline
    
    $packageInstalled = python -c "import importlib.util; print(importlib.util.find_spec('$packageName') is not None)" 2>&1
    
    if ($packageInstalled -eq "True") {
        Write-Host " Already installed." -ForegroundColor Green
        
        # Get version
        if ($packageName -eq "fitz") {
            $version = python -c "import fitz; print(f'Version: {fitz.__version__}')" 2>&1
            Write-Host "PyMuPDF $version"
        }
        elseif ($packageName -eq "pdfminer") {
            $version = python -c "from pdfminer import __version__; print(f'Version: {__version__}')" 2>&1
            Write-Host "pdfminer.six $version"
        }
    }
    else {
        Write-Host " Not found. Installing..." -ForegroundColor Yellow
        
        $installCmd = ""
        if ($packageName -eq "fitz") {
            $installCmd = "pip install PyMuPDF"
        }
        elseif ($packageName -eq "pdfminer") {
            $installCmd = "pip install pdfminer.six"
        }
        
        if ($installCmd -ne "") {
            Write-Host "Running: $installCmd"
            Invoke-Expression $installCmd
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "$packageName installed successfully." -ForegroundColor Green
            }
            else {
                Write-Host "Failed to install $packageName." -ForegroundColor Red
                exit 1
            }
        }
    }
}

# Install required packages
Install-PythonPackage "fitz"      # PyMuPDF
Install-PythonPackage "pdfminer"  # pdfminer.six

# Create a simple test script to verify the installation
$testScriptPath = Join-Path $PSScriptRoot "test_pdf_extraction.py"

$testScript = @"
#!/usr/bin/env python
# Test script for PDF extraction dependencies

import sys
print("Testing PDF extraction dependencies...")

try:
    import fitz
    print("? PyMuPDF (fitz) is installed. Version:", fitz.__version__)
except ImportError as e:
    print("? PyMuPDF (fitz) import failed:", str(e))
    sys.exit(1)

try:
    from pdfminer import high_level
    from pdfminer import __version__
    print("? pdfminer.six is installed. Version:", __version__)
except ImportError as e:
    print("? pdfminer.six import failed:", str(e))
    sys.exit(1)

print("\nAll required packages are installed correctly!")
print("The PDF extraction functionality in ValuesController.cs should work now.")
"@

Set-Content -Path $testScriptPath -Value $testScript

Write-Host ""
Write-Host "======================================================="
Write-Host "    Testing the installation"
Write-Host "======================================================="

try {
    $testResult = python $testScriptPath
    foreach ($line in $testResult) {
        Write-Host $line
    }
}
catch {
    Write-Host "Error running test script:" -ForegroundColor Red
    Write-Host $_ -ForegroundColor Red
}

Write-Host ""
Write-Host "======================================================="
Write-Host "    Installation Summary"
Write-Host "======================================================="
Write-Host ""
Write-Host "To extract text from Arabic PDFs, the application uses:"
Write-Host "1. PyMuPDF (fitz) - For PDF processing"
Write-Host "2. pdfminer.six - For text extraction"
Write-Host ""
Write-Host "These dependencies should now be installed in your Python environment."
Write-Host "If you encounter any issues, try running this script as Administrator."
Write-Host ""
Write-Host "For more information, check the documentation or contact support."