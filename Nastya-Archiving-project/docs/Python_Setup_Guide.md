# Setting up Python for PDF Text Extraction

This guide will help you set up Python and the necessary packages for PDF text extraction in the Nastya Archiving application.

## Prerequisites

- Windows operating system
- Administrator privileges
- Internet connection

## Automatic Setup

The easiest way to set up Python for the application is to use our automatic setup script:

1. Open the application and navigate to the API endpoints section
2. Call the `/api/values/python-check` endpoint to check if Python is already installed
3. If Python is not found, run the automatic installer by right-clicking on `scripts/install_dependencies_direct.bat` and selecting "Run as administrator"
4. After the installation completes, restart the application

## Manual Setup

If the automatic setup doesn't work, you can follow these steps to set up Python manually:

### 1. Install Python

1. Download Python 3.8 or higher from [python.org](https://www.python.org/downloads/)
2. Run the installer
3. **Important**: Check "Add Python to PATH" during installation
4. Complete the installation

### 2. Install Required Packages

Open Command Prompt or PowerShell as administrator and run:

```
python -m pip install --upgrade pip
python -m pip install PyMuPDF
python -m pip install pdfminer.six
```

### 3. Verify Installation

To verify that everything is installed correctly:

1. Open Command Prompt or PowerShell
2. Run the following commands:

```
python --version
python -c "import fitz; print('PyMuPDF is installed')"
python -c "import pdfminer; print('pdfminer.six is installed')"
```

If all commands run without errors, the setup is complete.

## Troubleshooting

### Python Not Found

If you see the error "The system cannot find the file specified" when running Python commands, it means Python is not in your PATH. Try:

- Reinstalling Python with the "Add Python to PATH" option checked
- Manually adding Python to your PATH environment variable

### Package Installation Failures

If you see errors installing packages, try:

1. Running Command Prompt or PowerShell as administrator
2. Using the `--user` flag: `python -m pip install --user PyMuPDF`
3. If behind a proxy, set the proxy environment variables

### Application Cannot Find Python

If the application cannot find Python even though it's installed:

1. Make sure Python is in the system PATH
2. Try specifying the full path to Python in the application settings
3. Check the application logs for specific error messages

## Need More Help?

If you're still having issues, please contact support with:

1. The output from `/api/values/environment-check` endpoint
2. The Python version you have installed
3. The specific error messages you're seeing
