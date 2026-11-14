@echo off
title The Fenrir - Local LLM Server (Ollama)
echo ====================================================
echo Starting Local LLM Server for "The Fenrir"
echo Model: llama3.2:1b-instruct-q4_K_M (4-bit GGUF Quantized)
echo Endpoint: http://localhost:11434
echo Target: Integrated Graphics Shared Memory Safe
echo ====================================================

ollama run llama3.2:1b-instruct-q4_K_M
pause

