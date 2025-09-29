# PowerShell script to download and set up Tesseract data files for Arabic language

# Create tessdata directory if it doesn't exist
$tessdataPath = Join-Path $PSScriptRoot "..\tessdata"
if (-Not (Test-Path $tessdataPath)) {
    Write-Host "Creating tessdata directory at: $tessdataPath"
    New-Item -ItemType Directory -Path $tessdataPath -Force | Out-Null
}

# URLs for Arabic language data
$arabicDataUrl = "https://github.com/tesseract-ocr/tessdata/raw/main/ara.traineddata"
$arabicScriptUrl = "https://github.com/tesseract-ocr/tessdata/raw/main/script/Arabic.traineddata"

# Download files
try {
    $arabicDataPath = Join-Path $tessdataPath "ara.traineddata"
    if (-Not (Test-Path $arabicDataPath)) {
        Write-Host "Downloading Arabic language data..."
        Invoke-WebRequest -Uri $arabicDataUrl -OutFile $arabicDataPath
        Write-Host "Downloaded: $arabicDataPath"
    } else {
        Write-Host "Arabic language data already exists at: $arabicDataPath"
    }
    
    $arabicScriptPath = Join-Path $tessdataPath "Arabic.traineddata"
    if (-Not (Test-Path $arabicScriptPath)) {
        Write-Host "Downloading Arabic script data..."
        Invoke-WebRequest -Uri $arabicScriptUrl -OutFile $arabicScriptPath
        Write-Host "Downloaded: $arabicScriptPath"
    } else {
        Write-Host "Arabic script data already exists at: $arabicScriptPath"
    }
    
    Write-Host "Tessdata setup complete."
    
} catch {
    Write-Host "Error downloading Tesseract data files: $_"
    exit 1
}

# Verify the files were downloaded correctly
if ((Test-Path $arabicDataPath) -and (Test-Path $arabicScriptPath)) {
    Write-Host "Tesseract Arabic language data is ready."
} else {
    Write-Host "Error: Some required files are missing."
    exit 1
}