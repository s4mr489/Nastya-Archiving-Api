# Arabic PDF Text Extraction Setup Guide

This document provides instructions for setting up the Python dependencies required for Arabic PDF text extraction.

## Prerequisites

- Python 3.7+ installed on the server
- Administrative access to install Python packages

## Automatic Setup

The easiest way to set up is to run the provided installation script:

### For Windows:

1. Open Command Prompt or PowerShell as Administrator
2. Navigate to the application directory
3. Run:
   ```
   scripts\install_dependencies_direct.bat
   ```

### For Linux/macOS:

1. Open Terminal
2. Navigate to the application directory
3. Run:
   ```
   chmod +x scripts/install_dependencies_direct.sh
   ./scripts/install_dependencies_direct.sh
   ```

## Manual Setup

If the automatic setup doesn't work, you can manually install the required dependencies:

1. Open Command Prompt or Terminal
2. Install the required Python packages:
   ```
   python -m pip install --upgrade PyMuPDF pdfminer.six
   ```

3. Create directory and copy the Python script:
   ```
   mkdir -p D:\NastyaArchive
   copy extract_arabic_pdf.py D:\NastyaArchive\
   ```

## Verify Installation

To verify that the installation was successful:

1. Open the web application
2. Go to the API endpoint: `/api/values/python-check`
3. This should display information about the Python environment and installed packages

## Troubleshooting

If you encounter issues:

1. Verify Python is installed and in your PATH
   ```
   python --version
   ```

2. Check if the Python packages are installed:
   ```
   python -c "import fitz; print('PyMuPDF version:', fitz.__version__)"
   python -c "from pdfminer import __version__; print('pdfminer version:', __version__)"
   ```

3. Verify the script exists at the expected location:
   ```
   dir D:\NastyaArchive\extract_arabic_pdf.py
   ```

4. Use the built-in diagnostic endpoint:
   ```
   /api/values/python-prepare
   ```
   
This will automatically attempt to set up the Python environment and script.