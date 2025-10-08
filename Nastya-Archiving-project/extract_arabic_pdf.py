#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Enhanced PDF text extraction script optimized for Arabic text and RTL languages
"""

import sys
import os
import re
import json
from datetime import datetime

# Try to import required libraries
try:
    import fitz  # PyMuPDF
except ImportError:
    fitz = None

try:
    from pdfminer.high_level import extract_text as pdfminer_extract_text
    from pdfminer.layout import LAParams
except ImportError:
    pdfminer_extract_text = None

# Arabic character set for detection
ARABIC_CHAR_SET = set([chr(c) for c in range(0x0600, 0x06FF)] +  # Arabic
                      [chr(c) for c in range(0x0750, 0x077F)] +  # Arabic Supplement
                      [chr(c) for c in range(0x08A0, 0x08FF)] +  # Arabic Extended-A
                      [chr(c) for c in range(0xFB50, 0xFDFF)] +  # Arabic Presentation Forms-A
                      [chr(c) for c in range(0xFE70, 0xFEFF)])   # Arabic Presentation Forms-B

# RTL mark character
RTL_MARK = '\u200F'

def is_arabic_text(text, threshold=0.1):
    """Determine if text is likely Arabic based on character frequency"""
    if not text:
        return False
        
    # Count Arabic characters
    arabic_count = sum(1 for c in text if c in ARABIC_CHAR_SET)
    
    # Calculate percentage of Arabic characters
    ratio = arabic_count / len(text)
    
    return ratio >= threshold

def extract_with_pymupdf(file_path):
    """Extract text using PyMuPDF (Faster and better with Arabic)"""
    try:
        doc = fitz.open(file_path)
        text = ""
        
        for page_num in range(len(doc)):
            page = doc.load_page(page_num)
            # Use "text" mode to preserve proper text flow
            page_text = page.get_text("text", sort=True)
            text += page_text + "\n\n"
            
        doc.close()
        return text.strip()
    except Exception as e:
        return f"PyMuPDF extraction error: {str(e)}"

def extract_with_pdfminer(file_path):
    """Extract text using pdfminer.six (Better with complex layouts)"""
    try:
        # Use Arabic-friendly parameters
        laparams = LAParams(
            char_margin=1.0,
            line_margin=0.5,
            boxes_flow=0.5,
            detect_vertical=True,
            all_texts=True
        )
        
        return pdfminer_extract_text(file_path, laparams=laparams)
    except Exception as e:
        return f"PDFMiner extraction error: {str(e)}"

def format_rtl_text(text):
    """Apply RTL formatting to text with proper wrapping"""
    if not text:
        return text
        
    # Add RTL mark to beginning of each line
    lines = text.split('\n')
    formatted_lines = []
    
    for line in lines:
        if line and not line.startswith(RTL_MARK):
            formatted_line = RTL_MARK + line
        else:
            formatted_line = line
            
        # Remove any LEFT-TO-RIGHT MARK characters
        formatted_line = formatted_line.replace('\u200E', '')
        
        formatted_lines.append(formatted_line)
    
    return '\n'.join(formatted_lines)

def fix_arabic_punctuation(text):
    """Fix common issues with Arabic punctuation and spacing"""
    if not text:
        return text
        
    # Fix punctuation spacing
    text = text.replace(" :", ":")
    text = text.replace(" .", ".")
    text = text.replace(" ،", "،")
    text = text.replace(" ؟", "؟")
    
    # Ensure spaces after punctuation
    text = text.replace(".", ". ")
    text = text.replace("،", "، ")
    text = text.replace(":", ": ")
    text = text.replace("؟", "؟ ")
    
    # Clean up double spaces
    while "  " in text:
        text = text.replace("  ", " ")
        
    return text

def extract_text_from_pdf(file_path, prefer_method=None):
    """Extract text from PDF file with RTL support"""
    start_time = datetime.now()
    method_used = "unknown"
    text = ""
    error = None
    
    try:
        # Define extraction order based on preference
        methods = []
        
        if prefer_method == "pymupdf" and fitz:
            methods = [("pymupdf", extract_with_pymupdf), 
                      ("pdfminer", extract_with_pdfminer)]
        elif prefer_method == "pdfminer" and pdfminer_extract_text:
            methods = [("pdfminer", extract_with_pdfminer), 
                      ("pymupdf", extract_with_pymupdf)]
        elif fitz:
            methods = [("pymupdf", extract_with_pymupdf), 
                      ("pdfminer", extract_with_pdfminer)]
        elif pdfminer_extract_text:
            methods = [("pdfminer", extract_with_pdfminer)]
        
        # Try methods in order
        for method_name, extract_func in methods:
            if method_name == "pymupdf" and not fitz:
                continue
            if method_name == "pdfminer" and not pdfminer_extract_text:
                continue
                
            text = extract_func(file_path)
            
            # If we got text and it doesn't look like an error message
            if text and not text.startswith(("PyMuPDF extraction error", "PDFMiner extraction error")):
                method_used = method_name
                break
            
            # If it's an error message
            if text.startswith(("PyMuPDF extraction error", "PDFMiner extraction error")):
                error = text
                text = ""
        
        # If no text was extracted
        if not text and not error:
            error = "Failed to extract text with any available method"
        
        # Determine if text is RTL (Arabic)
        is_rtl = is_arabic_text(text)
        
        # Apply RTL formatting if needed
        if is_rtl:
            # Add RTL mark at beginning of text
            if not text.startswith(RTL_MARK):
                text = RTL_MARK + text
                
            # Format text for RTL display
            text = format_rtl_text(text)
            
            # Fix Arabic punctuation
            text = fix_arabic_punctuation(text)
        
        # Clean up text
        text = re.sub(r'\s+', ' ', text).strip()  # Normalize whitespace
        text = text.replace('\0', '')             # Remove null bytes
        
        processing_time = (datetime.now() - start_time).total_seconds()
        
        # Prepare response
        result = {
            "text": text,
            "isRightToLeft": is_rtl,
            "extractionMethod": method_used,
            "processingTimeMs": int(processing_time * 1000),
            "error": error
        }
        
        return result
        
    except Exception as e:
        return {
            "text": "",
            "isRightToLeft": False,
            "extractionMethod": "failed",
            "processingTimeMs": 0,
            "error": str(e)
        }

def main():
    import argparse
    
    parser = argparse.ArgumentParser(description='Extract text from PDF file with RTL support')
    parser.add_argument('file', help='PDF file path')
    parser.add_argument('--method', choices=['pymupdf', 'pdfminer'], help='Preferred extraction method')
    parser.add_argument('--output', help='Output file path (default: stdout)')
    
    args = parser.parse_args()
    
    if not os.path.isfile(args.file):
        print(json.dumps({"error": f"File not found: {args.file}"}))
        sys.exit(1)
    
    result = extract_text_from_pdf(args.file, args.method)
    
    if args.output:
        with open(args.output, 'w', encoding='utf-8') as f:
            f.write(result["text"])
    else:
        print(json.dumps(result, ensure_ascii=False))

if __name__ == "__main__":
    main()