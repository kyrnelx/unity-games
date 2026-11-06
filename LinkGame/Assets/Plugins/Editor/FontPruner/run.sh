#!/bin/bash

# 1. 自动切换当前工作目录到 run.sh 所在的文件夹（相当于 Windows 的 cd /d %~dp0）
cd "$(dirname "$0")"

# 2. 调用 Python3 执行裁剪脚本
python3 font_pruner.py

# 3. 运行完成后等待回车，防止终端窗口自动关闭（相当于 Windows 的 pause）
echo ""
read -p "按回车键（Enter）继续..."

# Mac 和 Linux 默认出于安全考虑，新创建的 .sh 脚本没有“可执行”权限。你需要赋予它执行权限：
# 打开 Mac/Linux 终端（Terminal），执行以下命令（只需执行一次）：
# 切换到 FontPruner 目录
# cd 你项目的路径/Assets/Plugins/Editor/FontPruner
# 给 run.sh 赋予可执行权限
# chmod +x run.sh