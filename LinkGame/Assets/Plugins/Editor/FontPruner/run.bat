@echo off
:: 自动将工作目录切换到 run.bat 所在的文件夹
cd /d "%~dp0"

:: 执行 Python 脚本
python font_pruner.py

pause