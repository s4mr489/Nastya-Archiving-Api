# Ghostscript Installation Guide

This guide explains how to install Ghostscript, which is required for proper PDF processing in the Nastya Archiving Project.

## Why Ghostscript is Needed

The Nastya Archiving Project uses ImageMagick for converting PDF files to images before performing OCR (Optical Character Recognition). ImageMagick depends on Ghostscript for PDF processing.

## Installation Steps

### For Windows

1. **Download Ghostscript**:
   - Go to the official Ghostscript download page: https://ghostscript.com/releases/gsdnld.html
   - Download the latest stable release for your system (64-bit is recommended)

2. **Install Ghostscript**:
   - Run the downloaded installer
   - Follow the installation prompts
   - Make sure to select "Add Ghostscript to the system PATH for all users" during installation

3. **Verify Installation**:
   - Open Command Prompt
   - Type `gswin64c --version` (or `gswin32c --version` for 32-bit)
   - You should see the Ghostscript version number

### For Linux (Ubuntu/Debian)

```bash
sudo apt-get update
sudo apt-get install ghostscript
```

Verify installation:
```bash
gs --version
```

### For macOS

```bash
brew update
brew install ghostscript
```

Verify installation:
```bash
gs --version
```

## Troubleshooting

If you encounter the following error:
```
FailedToExecuteCommand `"gswin64c.exe" -q -dQUIET -dSAFER -dBATCH -dNOPAUSE -dNOPROMPT...
```

This means that ImageMagick cannot find Ghostscript. Try these solutions:

1. **Ensure Ghostscript is in the PATH**:
   - Check if the Ghostscript bin directory is in your system PATH
   - Typical location is `C:\Program Files\gs\gs9.XX\bin` (where 9.XX is the version number)

2. **Restart the Application**:
   - After installing Ghostscript, restart your web server or application

3. **Manually Set the Path**:
   - If you're running in IIS, you might need to modify the application pool to include the Ghostscript path
   - For development, you can set the PATH environment variable before running the application

## Alternative Solutions

If you still encounter issues with Ghostscript, the application includes a fallback mechanism that attempts to process PDFs without Ghostscript, but with potentially lower quality results.

## For Server Administrators

When deploying to a production server, make sure to:

1. Install Ghostscript on the server
2. Add Ghostscript bin directory to the system PATH
3. Ensure the application process/user has permissions to execute Ghostscript

If you're using containerized deployment (like Docker), make sure your container image includes Ghostscript.