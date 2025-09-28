@echo off
echo ============================================================
echo    Installing Python Dependencies for PDF Text Extraction
echo ============================================================
echo.

REM Try to find Python path from environment
set PYTHON_PATH=

REM Try standard Python command
python --version >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Found Python in PATH
    set PYTHON_PATH=python
    goto :PYTHON_FOUND
)

REM Try python3 command
python3 --version >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Found Python3 in PATH
    set PYTHON_PATH=python3
    goto :PYTHON_FOUND
)

REM Try specific Windows Store Python path
if exist "%LOCALAPPDATA%\Microsoft\WindowsApps\python.exe" (
    echo Found Python in Windows Apps folder
    set "PYTHON_PATH=%LOCALAPPDATA%\Microsoft\WindowsApps\python.exe"
    goto :PYTHON_FOUND
)

REM Try other common installation paths
set PYTHON_PATHS=^
C:\Python312\python.exe^
C:\Python311\python.exe^
C:\Python310\python.exe^
C:\Python39\python.exe^
C:\Python38\python.exe^
C:\Program Files\Python312\python.exe^
C:\Program Files\Python311\python.exe^
C:\Program Files\Python310\python.exe^
C:\Program Files\Python39\python.exe^
C:\Program Files\Python38\python.exe^
C:\Program Files (x86)\Python312\python.exe^
C:\Program Files (x86)\Python311\python.exe^
C:\Program Files (x86)\Python310\python.exe^
C:\Program Files (x86)\Python39\python.exe^
C:\Program Files (x86)\Python38\python.exe

for %%p in (%PYTHON_PATHS%) do (
    if exist "%%p" (
        echo Found Python at: %%p
        set "PYTHON_PATH=%%p"
        goto :PYTHON_FOUND
    )
)

echo Could not find Python automatically.
echo Please enter the full path to python.exe:
set /p PYTHON_PATH="> "

if not exist "%PYTHON_PATH%" (
    echo Error: Python not found at specified path.
    echo Please install Python from https://www.python.org/
    pause
    exit /b 1
)

:PYTHON_FOUND
echo Using Python: %PYTHON_PATH%
"%PYTHON_PATH%" --version

echo.
echo Installing required packages...
echo.
echo 1. Installing PyMuPDF...
"%PYTHON_PATH%" -m pip install --upgrade PyMuPDF

echo.
echo 2. Installing pdfminer.six...
"%PYTHON_PATH%" -m pip install --upgrade pdfminer.six

echo.
echo Verifying installations...
"%PYTHON_PATH%" -c "try: import fitz; print(f'PyMuPDF {fitz.__version__} is installed'); print(f'Location: {fitz.__file__}') except ImportError as e: print('PyMuPDF is NOT installed: ' + str(e))"
"%PYTHON_PATH%" -c "try: from pdfminer import __version__; print(f'pdfminer.six {__version__} is installed') except ImportError as e: print('pdfminer.six is NOT installed: ' + str(e))"

echo.
echo Copying extract_arabic_pdf.py to application directories...
set SCRIPT_PATH=%~dp0..\extract_arabic_pdf.py
set TARGET_PATH1=%~dp0..\bin\Debug\net9.0\extract_arabic_pdf.py
set TARGET_PATH2=%~dp0..\extract_arabic_pdf.py

if exist "%SCRIPT_PATH%" (
    copy /Y "%SCRIPT_PATH%" "%TARGET_PATH1%" >nul 2>&1
    if %ERRORLEVEL% EQU 0 (
        echo Copied script to: %TARGET_PATH1%
    ) else (
        echo Failed to copy script to: %TARGET_PATH1%
    )
    
    copy /Y "%SCRIPT_PATH%" "%TARGET_PATH2%" >nul 2>&1
    if %ERRORLEVEL% EQU 0 (
        echo Copied script to: %TARGET_PATH2%
    ) else (
        echo Failed to copy script to: %TARGET_PATH2%
    )
) else (
    echo WARNING: Could not find source script at: %SCRIPT_PATH%
)

echo.
echo Creating test script...
set TEST_SCRIPT=%TEMP%\pdf_test_script.py
echo import sys > "%TEST_SCRIPT%"
echo print("Python executable: " + sys.executable) >> "%TEST_SCRIPT%"
echo print("Python version: " + sys.version) >> "%TEST_SCRIPT%"
echo. >> "%TEST_SCRIPT%"
echo try: >> "%TEST_SCRIPT%"
echo     import fitz >> "%TEST_SCRIPT%"
echo     print("PyMuPDF version: " + fitz.__version__) >> "%TEST_SCRIPT%"
echo except ImportError as e: >> "%TEST_SCRIPT%"
echo     print("PyMuPDF import error: " + str(e)) >> "%TEST_SCRIPT%"
echo. >> "%TEST_SCRIPT%"
echo try: >> "%TEST_SCRIPT%"
echo     from pdfminer.high_level import extract_text >> "%TEST_SCRIPT%"
echo     from pdfminer import __version__ >> "%TEST_SCRIPT%"
echo     print("pdfminer.six version: " + __version__) >> "%TEST_SCRIPT%"
echo except ImportError as e: >> "%TEST_SCRIPT%"
echo     print("pdfminer.six import error: " + str(e)) >> "%TEST_SCRIPT%"

echo Running test script...
"%PYTHON_PATH%" "%TEST_SCRIPT%"

echo.
echo ============================================================
echo    Installation Complete
echo ============================================================
echo.
echo Please restart your application and try the PDF extraction again.
echo.

pause