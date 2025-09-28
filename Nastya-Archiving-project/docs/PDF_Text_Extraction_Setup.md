# PDF Text Extraction Setup Guide

This document provides instructions for setting up and troubleshooting the PDF text extraction feature in the Nastya-Archiving-project.

## Overview

The ValuesController in this project provides functionality to extract text from PDF files, with special support for Arabic text. This is accomplished through a combination of:

1. C# code in the ValuesController.cs file
2. A Python script (extract_arabic_pdf.py) that handles the actual PDF processing
3. Python libraries (PyMuPDF and pdfminer.six) that provide the text extraction capabilities

## Requirements

- Python 3.7 or higher installed and available in PATH
- Required Python packages:
  - PyMuPDF (also known as 'fitz')
  - pdfminer.six

## Installation Instructions

### 1. Install Python

If Python is not already installed:

- Download and install Python from [python.org](https://www.python.org/downloads/)
- **Important:** During installation, check "Add Python to PATH"
- Verify installation by opening a command prompt and typing:
  ```
  python --version
  ```

### 2. Install Required Python Packages

#### Automatic Installation

Run the provided PowerShell script to automatically install all required dependencies:

```powershell
.\scripts\install_python_dependencies.ps1
```

#### Manual Installation

If you prefer to install the packages manually:

```bash
# Install PyMuPDF
pip install PyMuPDF

# Install pdfminer.six
pip install pdfminer.six
```

### 3. Verify Installation

To verify that all dependencies are installed correctly:

```bash
# Check PyMuPDF
python -c "import fitz; print(f'PyMuPDF version: {fitz.__version__}')"

# Check pdfminer.six
python -c "from pdfminer import __version__; print(f'pdfminer.six version: {__version__}')"
```

## Configuration

The PDF extraction feature is configured to work out-of-the-box once the dependencies are installed. The ValuesController looks for the Python script in these locations (in order):

1. In the application's base directory (`AppDomain.CurrentDomain.BaseDirectory`)
2. In the current working directory (`Directory.GetCurrentDirectory()`)
3. Using a relative path (assuming the script is in the PATH)

## Troubleshooting

### Common Issues

1. **"Python is not installed or not in PATH"**
   - Solution: Install Python and make sure it's added to your PATH environment variable

2. **"PyMuPDF is not installed. Install with: pip install PyMuPDF"**
   - Solution: Run `pip install PyMuPDF`

3. **"pdfminer.six is not installed. Install with: pip install pdfminer.six"**
   - Solution: Run `pip install pdfminer.six`

4. **"Script not found in base directory"**
   - Solution: Make sure the `extract_arabic_pdf.py` file is in the application's directory

### Advanced Troubleshooting

If you continue to experience issues:

1. Check the Python script path:
   - The script tries to find the Python executable using the `where` command (Windows) or `which` command (Linux/macOS)
   - You can manually specify the Python path in the configuration if needed

2. Check for errors in the Python script:
   - The ValuesController captures and logs any errors from the Python script
   - Check the application logs for more details

3. Try running the Python script directly:
   ```bash
   python extract_arabic_pdf.py test.pdf
   ```

## Support

If you encounter any issues that aren't resolved by the steps above, please contact the development team with the following information:

1. Your operating system version
2. Python version (`python --version`)
3. Installed packages (`pip list`)
4. Any error messages from the logs

---

## For Developers

### How the PDF Extraction Works

1. The user uploads a PDF file to the `/api/values/extract` or `/api/values/extract-python` endpoint
2. The ValuesController saves the file to a temporary location
3. If the endpoint is `extract-python`, the controller calls the Python script directly
4. The Python script processes the PDF using either PyMuPDF or pdfminer.six
5. The extracted text is returned to the controller and sent back to the client

### Adding New Extraction Capabilities

To extend the PDF extraction functionality:

1. Modify the `extract_arabic_pdf.py` script to include new extraction methods
2. Update the ValuesController.cs file to call these new methods as needed
3. Update this documentation to reflect any changes in requirements or usage