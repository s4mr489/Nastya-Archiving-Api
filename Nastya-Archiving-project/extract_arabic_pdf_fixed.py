#!/usr/bin/env python
# -*- coding: utf-8 -*-

import sys
import os
import re
import io
from typing import List, Dict, Any
import argparse
import traceback

# Set stdout to use UTF-8 encoding
sys.stdout.reconfigure(encoding='utf-8', errors='backslashreplace')

def print_debug(msg):
    """Print debug message to stderr (won't affect output captured by C#)"""
    sys.stderr.write(f"DEBUG: {msg}\n")
    sys.stderr.flush()

try:
    import fitz  # PyMuPDF
    PYMUPDF_AVAILABLE = True
    print_debug("PyMuPDF is available")
except ImportError:
    PYMUPDF_AVAILABLE = False
    print_debug("PyMuPDF is NOT available")

try:
    from pdfminer.high_level import extract_text
    PDFMINER_AVAILABLE = True
    print_debug("pdfminer is available")
except ImportError:
    PDFMINER_AVAILABLE = False
    print_debug("pdfminer is NOT available")

def extract_with_pymupdf(pdf_path: str) -> str:
    """Extract text from PDF using PyMuPDF (better for Arabic)"""
    if not PYMUPDF_AVAILABLE:
        return "PyMuPDF is not installed. Install with: pip install PyMuPDF"
    
    text = ""
    try:
        print_debug(f"Opening PDF with PyMuPDF: {pdf_path}")
        doc = fitz.open(pdf_path)
        print_debug(f"PDF opened successfully, pages: {len(doc)}")
        
        for page_num in range(len(doc)):
            page = doc[page_num]
            # Get text with specific parameters to improve Arabic extraction
            page_text = page.get_text("text", sort=True, flags=fitz.TEXT_PRESERVE_LIGATURES | 
                                 fitz.TEXT_PRESERVE_WHITESPACE | 
                                 fitz.TEXT_DEHYPHENATE)
            text += page_text
            text += "\n\n"
            print_debug(f"Page {page_num+1} processed, extracted {len(page_text)} chars")
        
        doc.close()
    except Exception as e:
        error_text = f"Error extracting text with PyMuPDF: {str(e)}\n{traceback.format_exc()}"
        print_debug(error_text)
        text = error_text
    
    return text

def extract_with_pdfminer(pdf_path: str) -> str:
    """Extract text from PDF using pdfminer (backup method)"""
    if not PDFMINER_AVAILABLE:
        return "pdfminer.six is not installed. Install with: pip install pdfminer.six"
    
    try:
        print_debug(f"Extracting text with pdfminer: {pdf_path}")
        text = extract_text(pdf_path)
        print_debug(f"pdfminer extracted {len(text)} chars")
        return text
    except Exception as e:
        error_text = f"Error extracting text with pdfminer: {str(e)}\n{traceback.format_exc()}"
        print_debug(error_text)
        return error_text

def process_arabic_text(text: str) -> str:
    """Process Arabic text to improve readability and fix common issues"""
    if not text:
        return "No text was extracted from the document."
    
    # Remove excessive whitespace
    text = re.sub(r'\s+', ' ', text)
    
    # Fix common Arabic character substitution issues
    replacements = {
        '•': '',  # Remove bullet points that confuse Arabic text
        '?': '',  # Remove unknown characters
    }
    
    for old, new in replacements.items():
        text = text.replace(old, new)
    
    # Split into lines and add RTL mark to lines with Arabic
    lines = text.split('\n')
    processed_lines = []
    
    for line in lines:
        line = line.strip()
        if not line:
            processed_lines.append('')
            continue
            
        # If line contains Arabic characters, add RTL mark
        if re.search(r'[\u0600-\u06FF]', line):
            line = '\u200F' + line  # RTL mark
        
        processed_lines.append(line)
    
    return '\n'.join(processed_lines)

def main():
    try:
        # Handle encoding setup
        # Force UTF-8 output
        if hasattr(sys.stdout, 'buffer'):
            # Binary output support
            sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='backslashreplace')
        
        # Parse command line arguments
        parser = argparse.ArgumentParser(description='Extract Arabic text from PDF')
        parser.add_argument('pdf_path', help='Path to PDF file')
        parser.add_argument('--method', choices=['pymupdf', 'pdfminer', 'both'], 
                          default='both', help='Extraction method to use')
        
        args = parser.parse_args()
        
        print_debug(f"Script started with arguments: {args}")
        
        if not os.path.isfile(args.pdf_path):
            error_msg = f"Error: File not found: {args.pdf_path}"
            print_debug(error_msg)
            print(error_msg, file=sys.stderr)
            sys.exit(1)
        
        extracted_text = ""
        
        # Try PyMuPDF first (better for Arabic)
        if args.method in ['pymupdf', 'both']:
            extracted_text = extract_with_pymupdf(args.pdf_path)
            
        # If PyMuPDF failed or returned little text, try pdfminer as backup
        if (not extracted_text or len(extracted_text.strip()) < 100) and args.method in ['pdfminer', 'both']:
            extracted_text = extract_with_pdfminer(args.pdf_path)
        
        # If both methods failed, return a diagnostic message
        if not extracted_text or len(extracted_text.strip()) < 20:
            print_debug("Both extraction methods produced little or no text")
            # Check if file is really a PDF
            with open(args.pdf_path, 'rb') as f:
                header = f.read(4)
                if header != b'%PDF':
                    extracted_text = f"File does not appear to be a valid PDF. First bytes: {header}"
                else:
                    extracted_text = "Failed to extract meaningful text from this PDF. It might be an image-only PDF or have content protection."
        
        # Process the extracted text for better Arabic readability
        processed_text = process_arabic_text(extracted_text)
        print_debug(f"Final processed text length: {len(processed_text)}")
        
        # Write output to a temporary file to avoid encoding issues with print()
        try:
            # First try using UTF-8 print
            print(processed_text, flush=True)
        except UnicodeEncodeError:
            # If that fails, try a more robust approach - write to a file
            print_debug("Direct print failed, using file output method")
            temp_output = os.path.join(os.path.dirname(args.pdf_path), "extracted_text.txt")
            with open(temp_output, 'w', encoding='utf-8') as f:
                f.write(processed_text)
            print(f"TEXT_SAVED_TO:{temp_output}")
            
    except Exception as e:
        error_text = f"Error in main function: {str(e)}\n{traceback.format_exc()}"
        print_debug(error_text)
        print(error_text, file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()