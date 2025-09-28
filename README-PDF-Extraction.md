# Arabic PDF Text Extraction

## Python Dependencies Setup

To use the Python-based PDF text extraction endpoint, you need to install Python and the following Python packages:

```bash
pip install PyMuPDF pdfminer.six
```

These libraries provide better support for extracting Arabic text from PDF files than the default .NET libraries.

## Using the API

### C# OCR Endpoint
```
POST /api/values/extract
```
- Upload a PDF file using the `pdfFile` form parameter
- This endpoint uses built-in .NET libraries (PdfPig + Tesseract OCR)

### Python-based Extraction Endpoint
```
POST /api/values/extract-python
```
- Upload a PDF file using the `pdfFile` form parameter
- This endpoint uses the Python script with PyMuPDF and pdfminer.six for better Arabic text extraction

### Diagnostic Endpoint
```
GET /api/values/python-check
```
- This endpoint checks if Python and required libraries are properly installed
- It will detect all Python installations available in your system PATH
- Tests each Python installation for the required packages
- Recommends the best Python environment to use
- Use this to troubleshoot issues with the Python-based extraction

### Response Format
```json
{
  "text": "Extracted Arabic text...",
  "rtl": true,
  "source": "python",
  "textLength": 1234
}
```

## How it Works

1. The uploaded PDF is saved to a temporary file
2. The system automatically detects the best available Python installation
3. The Python script extracts text using specialized libraries optimized for Arabic text
4. The text is processed to ensure correct RTL (right-to-left) display
5. The temporary file is deleted after processing

## Troubleshooting

If the Python extraction isn't working properly, follow these steps:

1. Call the diagnostic endpoint `GET /api/values/python-check` first to check your Python environment
2. Make sure Python is installed and available in your system PATH
3. Install the required packages using: `pip install PyMuPDF pdfminer.six`
4. Verify the `extract_arabic_pdf.py` script exists in the application root directory
   - If the script doesn't exist in the expected location, the diagnostic endpoint will try to create it

### Common Issues:

1. **Multiple Python Installations**: The system will automatically try to find the best Python installation with the required packages.
   - If needed, you can specify a specific Python path by editing the controller code.

2. **Missing Libraries**: The error message will tell you which Python packages are missing. Install them with:
   ```bash
   pip install PyMuPDF pdfminer.six
   ```

3. **Script Not Found**: Make sure the `extract_arabic_pdf.py` file is in the application root directory or in the current working directory.

4. **Permission Issues**: Ensure the web application has permission to execute Python and access temporary files.

5. **Empty Response**: Check the console logs for detailed error messages. This often indicates a problem with the Python script execution or path configuration.

## Installation for Development Environment

1. Install Python (3.8 or newer recommended)
2. Install required packages:
   ```bash
   pip install PyMuPDF pdfminer.six
   ```
3. Verify the installation with the diagnostic endpoint
4. Test with a small PDF file first

## Installation for Production Environment

1. Install Python on the server
2. Install required packages globally or in a virtual environment:
   ```bash
   pip install PyMuPDF pdfminer.six
   ```
3. Make sure the application pool/service account has permission to execute Python
4. Deploy the `extract_arabic_pdf.py` script to the application root directory
5. Verify with the diagnostic endpoint after deployment