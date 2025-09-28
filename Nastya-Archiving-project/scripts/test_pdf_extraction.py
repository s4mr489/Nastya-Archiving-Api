#!/usr/bin/env python
# test_pdf_extraction.py - Test script for PDF extraction in Nastya-Archiving-project

import sys
import os
import argparse

def check_dependencies():
    """Check if required dependencies are installed."""
    dependencies_ok = True
    
    print("Checking required dependencies:")
    
    # Check PyMuPDF
    try:
        import fitz
        print(f"? PyMuPDF (fitz) is installed - Version: {fitz.__version__}")
    except ImportError as e:
        print(f"? PyMuPDF not installed: {e}")
        print("  Install with: pip install PyMuPDF")
        dependencies_ok = False
    
    # Check pdfminer.six
    try:
        from pdfminer import high_level
        from pdfminer import __version__
        print(f"? pdfminer.six is installed - Version: {__version__}")
    except ImportError as e:
        print(f"? pdfminer.six not installed: {e}")
        print("  Install with: pip install pdfminer.six")
        dependencies_ok = False
    
    return dependencies_ok

def extract_with_pymupdf(pdf_path):
    """Extract text using PyMuPDF."""
    import fitz
    
    print(f"Extracting text from {pdf_path} using PyMuPDF...")
    try:
        doc = fitz.open(pdf_path)
        text = ""
        
        print(f"PDF has {len(doc)} pages")
        for i, page in enumerate(doc):
            page_text = page.get_text()
            text += page_text
            print(f"  Page {i+1}: extracted {len(page_text)} characters")
        
        return text
    except Exception as e:
        print(f"Error with PyMuPDF: {e}")
        return None

def extract_with_pdfminer(pdf_path):
    """Extract text using pdfminer.six."""
    from pdfminer.high_level import extract_text
    
    print(f"Extracting text from {pdf_path} using pdfminer.six...")
    try:
        text = extract_text(pdf_path)
        print(f"  Extracted {len(text)} characters")
        return text
    except Exception as e:
        print(f"Error with pdfminer: {e}")
        return None

def main():
    parser = argparse.ArgumentParser(description="Test PDF extraction functionality")
    parser.add_argument("pdf_path", nargs="?", help="Path to PDF file to test extraction")
    args = parser.parse_args()
    
    # Check dependencies
    if not check_dependencies():
        print("\n? Some dependencies are missing. Please install them and try again.")
        sys.exit(1)
    
    # If no PDF path provided, just check dependencies and exit
    if not args.pdf_path:
        print("\n? All dependencies installed correctly!")
        print("To test extraction with a PDF file, run: python test_pdf_extraction.py path/to/your/file.pdf")
        sys.exit(0)
    
    # Verify the PDF file exists
    if not os.path.isfile(args.pdf_path):
        print(f"? Error: File not found: {args.pdf_path}")
        sys.exit(1)
    
    # Test extraction with both methods
    print("\nTesting extraction methods:")
    
    # Test PyMuPDF
    pymupdf_text = extract_with_pymupdf(args.pdf_path)
    if pymupdf_text:
        print(f"? PyMuPDF extraction successful - {len(pymupdf_text)} characters extracted")
        print("-" * 40)
        print(pymupdf_text[:500] + ("..." if len(pymupdf_text) > 500 else ""))
        print("-" * 40)
    else:
        print("? PyMuPDF extraction failed")
    
    print()  # Separator
    
    # Test pdfminer.six
    pdfminer_text = extract_with_pdfminer(args.pdf_path)
    if pdfminer_text:
        print(f"? pdfminer.six extraction successful - {len(pdfminer_text)} characters extracted")
        print("-" * 40)
        print(pdfminer_text[:500] + ("..." if len(pdfminer_text) > 500 else ""))
        print("-" * 40)
    else:
        print("? pdfminer.six extraction failed")
    
    # Compare results
    if pymupdf_text and pdfminer_text:
        if len(pymupdf_text) > len(pdfminer_text):
            print(f"\nPyMuPDF extracted {len(pymupdf_text) - len(pdfminer_text)} more characters than pdfminer.six")
        elif len(pdfminer_text) > len(pymupdf_text):
            print(f"\npdfminer.six extracted {len(pdfminer_text) - len(pymupdf_text)} more characters than PyMuPDF")
        else:
            print("\nBoth methods extracted the same number of characters")
    
    # Overall results
    if pymupdf_text or pdfminer_text:
        print("\n? PDF text extraction test completed successfully!")
        print("The PDF extraction functionality in ValuesController.cs should work correctly.")
    else:
        print("\n? Both extraction methods failed. Check that your PDF contains extractable text.")
        print("If it's a scanned document or image-based PDF, OCR may be needed.")

if __name__ == "__main__":
    main()