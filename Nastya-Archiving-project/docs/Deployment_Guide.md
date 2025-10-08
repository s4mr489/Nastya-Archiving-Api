# Deployment Guide for Nastya Archiving Project

This guide provides instructions for properly deploying the Nastya Archiving Project with all required dependencies.

## Prerequisites

- .NET 9 Runtime
- Python 3.8 or higher
- Ghostscript (for PDF processing)
- IIS (for web deployment) or other web server

## Deployment Options

### 1. Visual Studio Publish

1. Open the solution in Visual Studio
2. Right-click on the project in Solution Explorer and select "Publish"
3. Choose your publish profile (Folder or IIS)
4. Click "Publish"

The project is configured to include all Python scripts, tessdata directory, and scripts directory in the publish output.

### 2. dotnet CLI Publish

Run one of the following commands:

```
# For folder deployment
dotnet publish -c Release -o ./publish

# For self-contained deployment
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

## Post-Deployment Setup

After deploying the application, you need to ensure all dependencies are properly set up:

1. Navigate to the deployed application directory
2. Go to the `scripts` folder
3. Run `post_deploy_setup.bat`

This script will:
- Verify Python installation
- Check and install required Python packages (PyMuPDF, pdfminer.six)
- Verify and set up Tesseract OCR language data for Arabic text recognition
- Check for Ghostscript installation

## Required Dependencies

### 1. Python and Packages

Python is used for advanced PDF text extraction. The application requires:
- Python 3.8+
- PyMuPDF package
- pdfminer.six package

See `docs/PDF_Text_Extraction_Setup_Guide.md` for detailed setup instructions.

### 2. Tesseract OCR Data

Tesseract is used for OCR (Optical Character Recognition) to extract text from images. The application needs:
- Arabic language files in the `tessdata` directory

These will be automatically set up by the post-deployment script.

### 3. Ghostscript

**Important:** Ghostscript is required for PDF-to-image conversion when performing OCR on image-based PDFs.

To install Ghostscript:
1. Download the installer from https://ghostscript.com/releases/gsdnld.html
2. Run the installer and follow the prompts
3. Make sure to add Ghostscript to your system PATH during installation

See `docs/Ghostscript_Installation_Guide.md` for detailed instructions.

## Manual Setup (if needed)

If the automatic setup doesn't work, you can perform these steps manually:

1. Install Python 3.8 or higher
2. Install required Python packages:
   ```
   pip install PyMuPDF pdfminer.six
   ```
3. Ensure Tesseract language data is installed:
   - Run the `scripts\setup_tessdata.ps1` script
   - Or manually download Arabic language data to `tessdata\ara.traineddata`
4. Install Ghostscript and ensure it's in the system PATH

## Troubleshooting

### PDF Text Extraction Issues

If you encounter errors related to PDF processing or "FailedToExecuteCommand gswin64c.exe", it's likely a Ghostscript issue:

1. Check if Ghostscript is installed:
   - Run `gswin64c --version` in Command Prompt
   - If not found, install Ghostscript
2. Verify Ghostscript is in PATH:
   - Navigate to your API endpoint `/api/values/ghostscript-check`
   - This will show if Ghostscript is properly installed and accessible
3. If needed, manually add Ghostscript to PATH:
   - Typical path is `C:\Program Files\gs\gs9.XX\bin` (where 9.XX is the version number)

### Python Environment Issues

To verify that your Python environment is set up correctly:

1. Run the application
2. Navigate to the `/api/values/python-check` endpoint
3. This will check Python installation and required packages

## Environment Verification Endpoints

The application provides several diagnostic endpoints:

- `/api/values/python-check` - Verifies Python environment
- `/api/values/ghostscript-check` - Verifies Ghostscript installation
- `/api/values/python-install-packages` - Attempts to install required Python packages

These endpoints can help diagnose issues after deployment.