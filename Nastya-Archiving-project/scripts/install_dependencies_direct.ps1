# This script installs Python dependencies directly to the environment used by the application
# It finds the Python executable used by the application and installs packages there

# Get the path to Python used by the application
$pythonPath = ""

# First check if we can find it from the application's logs or environment
$appDir = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
$logFile = Join-Path $appDir "logs\debug.log"

if (Test-Path $logFile) {
    Write-Host "Checking log file for Python path..."
    $pythonMatches = Select-String -Path $logFile -Pattern "Selected Python executable: (.*)" -AllMatches
    if ($pythonMatches -and $pythonMatches.Matches.Count -gt 0) {
        $pythonPath = $pythonMatches.Matches[$pythonMatches.Matches.Count - 1].Groups[1].Value
        Write-Host "Found Python path in logs: $pythonPath"
    }
}

# If we couldn't find it, try to use the system Python
if (-not $pythonPath -or -not (Test-Path $pythonPath)) {
    Write-Host "Trying to find Python in PATH..."
    try {
        $pythonPath = (Get-Command python -ErrorAction SilentlyContinue).Source
        if (-not $pythonPath) {
            $pythonPath = (Get-Command python3 -ErrorAction SilentlyContinue).Source
        }
    }
    catch {}
}

# If we still don't have it, ask the user
if (-not $pythonPath -or -not (Test-Path $pythonPath)) {
    Write-Host "Could not automatically detect Python path."
    $pythonPath = Read-Host "Please enter the full path to python.exe"
}

# Check if the Python path exists
if (-not (Test-Path $pythonPath)) {
    Write-Host "Error: Python executable not found at: $pythonPath" -ForegroundColor Red
    Write-Host "Please install Python and try again." -ForegroundColor Red
    exit 1
}

Write-Host "Using Python executable: $pythonPath" -ForegroundColor Green
Write-Host "Getting Python version..."
$pythonVersion = & $pythonPath --version

if ($pythonVersion) {
    Write-Host "Python version: $pythonVersion" -ForegroundColor Green
} else {
    Write-Host "Warning: Could not determine Python version." -ForegroundColor Yellow
}

# Get site-packages location
Write-Host "Getting site-packages location..."
$sitePackages = & $pythonPath -c "import site; print(site.getsitepackages()[0])"
Write-Host "Site packages directory: $sitePackages" -ForegroundColor Green

# Install required packages
Write-Host "`nInstalling PyMuPDF package..." -ForegroundColor Cyan
& $pythonPath -m pip install --upgrade PyMuPDF

Write-Host "`nInstalling pdfminer.six package..." -ForegroundColor Cyan
& $pythonPath -m pip install --upgrade pdfminer.six

# Verify installations
Write-Host "`nVerifying installations..." -ForegroundColor Cyan

$pyMuPDFResult = & $pythonPath -c "try: import fitz; print(f'PyMuPDF {fitz.__version__} is installed'); print(f'Location: {fitz.__file__}') except ImportError as e: print('PyMuPDF is NOT installed: ' + str(e))" 2>&1
$pdfminerResult = & $pythonPath -c "try: from pdfminer import __version__; print(f'pdfminer.six {__version__} is installed') except ImportError as e: print('pdfminer.six is NOT installed: ' + str(e))" 2>&1

Write-Host $pyMuPDFResult
Write-Host $pdfminerResult

# Create a test script in the temp directory to verify the installation
$testScriptPath = [System.IO.Path]::GetTempFileName() + ".py"
@"
import sys
print("Python executable: " + sys.executable)
print("Python version: " + sys.version)

try:
    import fitz
    print("PyMuPDF version: " + fitz.__version__)
except ImportError as e:
    print("PyMuPDF import error: " + str(e))

try:
    from pdfminer.high_level import extract_text
    from pdfminer import __version__
    print("pdfminer.six version: " + __version__)
except ImportError as e:
    print("pdfminer.six import error: " + str(e))
"@ | Set-Content -Path $testScriptPath

Write-Host "`nRunning test script..." -ForegroundColor Cyan
& $pythonPath $testScriptPath

# Copy the Python script to the correct location
Write-Host "`nCopying extract_arabic_pdf.py to the application directories..." -ForegroundColor Cyan

# Source script path in the scripts directory
$sourceScriptPath = Join-Path $appDir "Nastya-Archiving-project\extract_arabic_pdf.py"

# Target paths - copy to multiple locations to ensure it's found
$targetPaths = @(
    (Join-Path $appDir "Nastya-Archiving-project\bin\Debug\net9.0\extract_arabic_pdf.py"),
    (Join-Path $appDir "Nastya-Archiving-project\extract_arabic_pdf.py")
)

foreach ($targetPath in $targetPaths) {
    try {
        Copy-Item -Path $sourceScriptPath -Destination $targetPath -Force
        Write-Host "  Copied script to $targetPath" -ForegroundColor Green
    } catch {
        Write-Host "  Failed to copy to $targetPath: $_" -ForegroundColor Yellow
    }
}

Write-Host "`nInstallation complete. Please restart your application and try the PDF extraction again." -ForegroundColor Green

# Clean up
Remove-Item $testScriptPath -Force -ErrorAction SilentlyContinue