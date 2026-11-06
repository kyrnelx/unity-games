import os
import re
import sys
from fontTools import subset

# ==================== 🛠️ 可选配置区 ====================

# 1. 是否自动扫描 Unity 项目 (Assets/ 目录下的 Prefab/场景等)
ENABLE_PROJECT_SCAN = True

# 2. 自定义文本文件路径 (可选)
CUSTOM_TEXT_FILE = ""

# ========================================================

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ASSETS_DIR = os.path.abspath(os.path.join(SCRIPT_DIR, "../../../"))

RAW_FONTS_DIR = os.path.join(SCRIPT_DIR, "raw_fonts")
OUTPUT_FONT_PATH = os.path.join(ASSETS_DIR, "Res", "Fonts", "msyh_compressed.ttf")

COMMON_CHARS_FILE = os.path.join(SCRIPT_DIR, "3500_common.txt")
if not os.path.exists(COMMON_CHARS_FILE):
    COMMON_CHARS_FILE = os.path.join(SCRIPT_DIR, "3500_common")


def read_file_safe(filepath):
    """安全读取文件，自动兼容 UTF-8、UTF-8-SIG 和 GB18030/GBK 编码"""
    encodings = ['utf-8-sig', 'utf-8', 'gb18030', 'gbk']
    for enc in encodings:
        try:
            with open(filepath, "r", encoding=enc) as f:
                return f.read()
        except Exception:
            continue
    try:
        with open(filepath, "r", encoding="utf-8", errors="ignore") as f:
            return f.read()
    except Exception:
        return ""


def extract_chars_from_text(text):
    r"""从文本中提取汉字，包含标准 Unicode 汉字及 Unity \uXXXX 格式转义字符"""
    chars = set()
    if not text:
        return chars

    # 1. 扩大的 CJK 汉字及标点正则 (包含常用字、扩展A区、全角标点)
    cjk_pattern = re.compile(r'[\u4e00-\u9fa5\u3400-\u4dbf\u3000-\u303f\uff00-\uffef]')

    # 提取直接写入的中文
    chars.update(cjk_pattern.findall(text))

    # 2. 匹配并解析 Unity YAML 中的 Unicode 转义格式 (例如: \u4f60\u597d)
    unicode_escapes = re.findall(r'\\u([0-9a-fA-F]{4})', text)
    for hex_str in unicode_escapes:
        try:
            code = int(hex_str, 16)
            ch = chr(code)
            # 如果转义出的字符属于 CJK 或非 ASCII 字符，加入字库
            if cjk_pattern.match(ch) or ord(ch) > 127:
                chars.add(ch)
        except ValueError:
            pass

    return chars


def find_source_font():
    """自动在 raw_fonts 文件夹中寻找源字体文件"""
    if not os.path.exists(RAW_FONTS_DIR):
        os.makedirs(RAW_FONTS_DIR, exist_ok=True)
        return None

    for file in os.listdir(RAW_FONTS_DIR):
        if file.lower().endswith(('.ttf', '.otf', '.ttc')):
            return os.path.join(RAW_FONTS_DIR, file)
    return None


def collect_characters():
    """收集需要保留的字符"""
    chars = set()

    # A. 基础 ASCII 可打印字符 (数字、英文字母、常用基础标点)
    for i in range(32, 127):
        chars.add(chr(i))

    # B. 读取 3500 常用字库文件
    if os.path.exists(COMMON_CHARS_FILE):
        content = read_file_safe(COMMON_CHARS_FILE)
        if content:
            content_clean = re.sub(r'\s+', '', content)
            chars.update(content_clean)
            print(f"[1/3] 已成功加载常用字库: {os.path.basename(COMMON_CHARS_FILE)} (共包含 {len(content_clean)} 个字)")
        else:
            print(f"[警告] 常用字库文件读取为空或失败: {COMMON_CHARS_FILE}")
    else:
        print(f"[警告] 未找到常用字库文件: {COMMON_CHARS_FILE}")

    # C. 读取自定义文本文件
    if CUSTOM_TEXT_FILE and os.path.exists(CUSTOM_TEXT_FILE):
        content = read_file_safe(CUSTOM_TEXT_FILE)
        if content:
            custom_chars = extract_chars_from_text(content)
            chars.update(custom_chars)
            print(f"[+] 已加载自定义文本文件: {CUSTOM_TEXT_FILE} (提取到 {len(custom_chars)} 个中文字符)")

    # D. 扫描 Unity 项目 Assets 目录
    if ENABLE_PROJECT_SCAN:
        print(f"[2/3] 正在扫描项目 Assets 目录: {ASSETS_DIR}")
        target_exts = ('.prefab', '.unity', '.json', '.csv', '.txt', '.asset', '.xml')

        scanned_count = 0
        found_in_project = set()

        for root, _, files in os.walk(ASSETS_DIR):
            if SCRIPT_DIR in os.path.abspath(root):
                continue

            for file in files:
                if file.lower().endswith(target_exts):
                    filepath = os.path.join(root, file)
                    scanned_count += 1
                    text = read_file_safe(filepath)
                    extracted = extract_chars_from_text(text)
                    found_in_project.update(extracted)

        chars.update(found_in_project)
        print(f"      已扫描 {scanned_count} 个项目文件，从项目中提取到 {len(found_in_project)} 个非 ASCII/中文字符。")
    else:
        print(f"[2/3] 已跳过扫描 Unity 项目 (ENABLE_PROJECT_SCAN = False)")

    chinese_samples = [c for c in chars if ord(c) > 127]
    sample_str = "".join(chinese_samples[:30])
    print(f"\n      最终搜集到 {len(chars)} 个唯一字符（其中非 ASCII 字符 {len(chinese_samples)} 个）。")
    print(f"      中文/非 ASCII 字符抽取样本: [{sample_str}...]\n")

    return "".join(chars)


def prune_font(source_path, output_path, text_string):
    """执行字体裁剪"""
    print(f"[3/3] 正在执行字体裁剪...")
    print(f"      源字体文件: {os.path.basename(source_path)}")

    options = subset.Options()
    options.layout_features = ["*"]
    options.glyph_names = True
    options.font_number = 0

    try:
        font = subset.load_font(source_path, options)
        subsetter = subset.Subsetter(options=options)
        subsetter.populate(text=text_string)
        subsetter.subset(font)

        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        subset.save_font(font, output_path, options)

        orig_mb = os.path.getsize(source_path) / (1024 * 1024)
        new_kb = os.path.getsize(output_path) / 1024

        print("\n" + "=" * 45)
        print(" 🎉 字体裁剪成功！")
        print(f" 原始字体大小: {orig_mb:.2f} MB")
        print(f" 裁剪后大小:   {new_kb:.2f} KB")
        print(f" 输出文件路径: {output_path}")
        print("=" * 45 + "\n")

    except Exception as e:
        print(f"\n[错误] 字体裁剪失败: {e}\n")


if __name__ == "__main__":
    source_font = find_source_font()
    if not source_font:
        print(f"\n[错误] 在 raw_fonts 目录未找到任何字体文件！(.ttf / .otf / .ttc)")
        sys.exit(1)

    all_chars = collect_characters()
    prune_font(source_font, OUTPUT_FONT_PATH, all_chars)