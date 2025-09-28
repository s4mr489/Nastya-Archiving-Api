@echo off
echo =====================================================
echo     Installing Python dependencies for PDF extraction
echo =====================================================
echo.

REM Check if Python is installed
python --version 2>NUL
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Python is not installed or not in PATH.
    echo Please install Python from https://www.python.org/downloads/
    echo Make sure to check 'Add Python to PATH' during installation.
    echo.
    pause
    exit /b 1
)

echo Found Python. Checking for required packages...

REM Install PyMuPDF if not already installed
echo.
echo Checking for PyMuPDF...
python -c "import fitz; print('PyMuPDF is already installed.')" 2>NUL
if %ERRORLEVEL% NEQ 0 (
    echo Installing PyMuPDF...
    pip install PyMuPDF
    if %ERRORLEVEL% NEQ 0 (
        echo Failed to install PyMuPDF.
        pause
        exit /b 1
    )
    echo PyMuPDF installed successfully.
) else (
    python -c "import fitz; print('Version:', fitz.__version__)"
)

REM Install pdfminer.six if not already installed
echo.
echo Checking for pdfminer.six...
python -c "from pdfminer import high_level; print('pdfminer.six is already installed.')" 2>NUL
if %ERRORLEVEL% NEQ 0 (
    echo Installing pdfminer.six...
    pip install pdfminer.six
    if %ERRORLEVEL% NEQ 0 (
        echo Failed to install pdfminer.six.
        pause
        exit /b 1
    )
    echo pdfminer.six installed successfully.
) else (
    python -c "from pdfminer import __version__; print('Version:', __version__)"
)

echo.
echo =====================================================
echo     Testing the installation
echo =====================================================

REM Create a test script in the current directory
echo import sys > test_pdf_extraction.py
echo print("Testing PDF extraction dependencies...") >> test_pdf_extraction.py
echo. >> test_pdf_extraction.py
echo try: >> test_pdf_extraction.py
echo     import fitz >> test_pdf_extraction.py
echo     print("? PyMuPDF (fitz) is installed. Version:", fitz.__version__) >> test_pdf_extraction.py
echo except ImportError as e: >> test_pdf_extraction.py
echo     print("? PyMuPDF (fitz) import failed:", str(e)) >> test_pdf_extraction.py
echo     sys.exit(1) >> test_pdf_extraction.py
echo. >> test_pdf_extraction.py
echo try: >> test_pdf_extraction.py
echo     from pdfminer import high_level >> test_pdf_extraction.py
echo     from pdfminer import __version__ >> test_pdf_extraction.py
echo     print("? pdfminer.six is installed. Version:", __version__) >> test_pdf_extraction.py
echo except ImportError as e: >> test_pdf_extraction.py
echo     print("? pdfminer.six import failed:", str(e)) >> test_pdf_extraction.py
echo     sys.exit(1) >> test_pdf_extraction.py
echo. >> test_pdf_extraction.py
echo print("\nAll required packages are installed correctly!") >> test_pdf_extraction.py
echo print("The PDF extraction functionality in ValuesController.cs should work now.") >> test_pdf_extraction.py

REM Run the test script
python test_pdf_extraction.py

REM Clean up the test script
del test_pdf_extraction.py

echo.
echo =====================================================
echo     Installation Summary
echo =====================================================
echo.
echo To extract text from Arabic PDFs, the application uses:
echo 1. PyMuPDF (fitz) - For PDF processing
echo 2. pdfminer.six - For text extraction
echo.
echo These dependencies should now be installed in your Python environment.
echo If you encounter any issues, try running this script as Administrator.
echo.
echo For more information, check the documentation in the docs folder.
echo.

pause