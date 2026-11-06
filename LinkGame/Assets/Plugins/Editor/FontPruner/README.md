这是一份为你的 **`FontPruner` 字体裁剪工具** 定制的完整说明文档（你可以直接将其保存为 `README.md` 或团队内部的使用文档）。

---

# 📦 FontPruner - 字体裁剪与轻量化工具使用文档

## 📖 1. 工具简介
**FontPruner** 是一个轻量级、零引擎依赖的字体子集化（Subset）与裁剪工具。

中文字体库（如微软雅黑）体积通常在 15MB~30MB 左右，直接打包进项目会导致应用包体过大、内存开销极高。本工具利用 Python `fontTools` 核心库，**仅提取项目实际用到的文字与常用字**，将字体文件体积大幅压缩至 **800KB ~ 1.5MB** 左右。

### ✨ 核心特性
* **无 Unity 引擎依赖**：外部独立运行，无需等待 Unity 编译，几秒钟即可完成裁剪。
* **智能路径识别**：自动相对路径计算，移到任何电脑或路径下均可直接运行。
* **智能字体抓取**：自动提取 `raw_fonts/` 目录下的 `.ttf` / `.otf` / `.ttc` 字体。
* **灵活裁剪模式**：支持“3500常用字裁剪”、“自定义词表裁剪”以及“自动扫描 Unity 项目 Prefab/场景字符”。
* **兼容 TTC 集合包**：自动处理 `.ttc` 字体多字轨索引问题。

---

## 📁 2. 目录结构

工具文件存放在 Unity 项目的 `Assets/Plugins/Editor/FontPruner/` 路径下：

```text
Assets/
├── Res/
│   └── Fonts/                   <-- [输出] 裁剪后的瘦身字体存放在此 (msyh_compressed.ttf)
└── Plugins/
    └── Editor/
        └── FontPruner/          <-- [工具主目录]
            ├── raw_fonts/       <-- [输入] 放置原始大字体文件 (如 msyh.ttf)
            ├── 3500_common      <-- [基础字库] 国家标准 3500 常用字+常用标点
            ├── font_pruner.py   <-- [核心脚本] Python 裁剪与扫描逻辑
            └── run.bat          <-- [执行入口] Windows 一键运行批处理脚本
```

---

## 🛠️ 3. 环境准备

运行本工具需要电脑安装有 Python 环境及 `fonttools` 库。

1. **安装 Python 3.x**（建议 Python 3.8 及以上）。
2. **安装依赖库**（打开 CMD 命令行运行）：
   ```bash
   pip install fonttools brotli
   ```

---

## ⚙️ 4. 配置说明 (`font_pruner.py`)

在 `font_pruner.py` 脚本最顶部，提供了灵活的配置开关：

```python
# ==================== 🛠️ 可选配置区 ====================

# 1. 是否自动扫描 Unity 项目 (Assets/ 目录下的 Prefab/场景/Json 等)
#    False = 关闭项目扫描（速度极快，只根据 3500 常用字裁剪）
#    True  = 开启自动扫描项目所有 UI 文本（防止生僻字遗漏）
ENABLE_PROJECT_SCAN = False

# 2. 自定义额外文本文件路径 (可选)
#    如果有特定的本地化语言表或自定义字表，可填写路径（如 "./my_words.txt"），留空 "" 则不使用
CUSTOM_TEXT_FILE = ""

# ========================================================
```

---

## 🚀 5. 使用步骤（快速上手）

### 第一步：放入原始字体
将你需要裁剪的原始大字体（例如 `msyh.ttf` 或 `msyh.ttc`）复制放到：
`Assets/Plugins/Editor/FontPruner/raw_fonts/` 目录下。

### 第二步：选择运行模式（可选）
* **快速模式（推荐）**：默认 `ENABLE_PROJECT_SCAN = False`，仅根据常用 3500 字 + 基础符号裁剪，几乎满足 95% 的游戏 UI 需求，生成速度最快。
* **全扫描模式**：若怕项目中有生僻字，将 `ENABLE_PROJECT_SCAN` 改为 `True`，脚本会自动递归扫描整个 `Assets/` 目录下的 Prefab 和 Scene 文件。

### 第三步：双击运行
在 Windows 资源管理器中打开 `Assets/Plugins/Editor/FontPruner/` 目录，**双击 `run.bat`**。

控制台将打印输出类似如下日志：
```text
[1/3] 已加载常用字库文件: 3500_common
[2/3] 已跳过扫描 Unity 项目 (ENABLE_PROJECT_SCAN = False)
      最终搜集到 3624 个唯一字符。
[3/3] 正在执行字体裁剪...
      源字体: msyh.ttf

=============================================
 🎉 字体裁剪成功！
 原始字体大小: 18.25 MB
 裁剪后大小:   980.50 KB
 输出文件路径: Assets/Res/Fonts/msyh_compressed.ttf
=============================================
```

---

## 📜 6. 核心脚本源码参考

### 6.1 运行脚本 (`run.bat`)
```cmd
@echo off
:: 自动将工作目录切换到 run.bat 所在的文件夹，防止路径找不到
cd /d "%~dp0"

:: 执行 Python 裁剪逻辑
python font_pruner.py

pause
```

### 6.2 核心逻辑脚本 (`font_pruner.py`)
```python
import os
import re
import sys
from fontTools import subset

# ==================== 可选配置区 ====================
ENABLE_PROJECT_SCAN = False  # 是否开启项目 Prefab/场景扫描
CUSTOM_TEXT_FILE = ""        # 自定义额外文本路径
# ==================================================

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ASSETS_DIR = os.path.abspath(os.path.join(SCRIPT_DIR, "../../../"))

RAW_FONTS_DIR = os.path.join(SCRIPT_DIR, "raw_fonts")
OUTPUT_FONT_PATH = os.path.join(ASSETS_DIR, "Res", "Fonts", "msyh_compressed.ttf")

COMMON_CHARS_FILE = os.path.join(SCRIPT_DIR, "3500_common.txt")
if not os.path.exists(COMMON_CHARS_FILE):
    COMMON_CHARS_FILE = os.path.join(SCRIPT_DIR, "3500_common")


def find_source_font():
    if not os.path.exists(RAW_FONTS_DIR):
        os.makedirs(RAW_FONTS_DIR, exist_ok=True)
        return None
    for file in os.listdir(RAW_FONTS_DIR):
        if file.lower().endswith(('.ttf', '.otf', '.ttc')):
            return os.path.join(RAW_FONTS_DIR, file)
    return None


def collect_characters():
    chars = set()
    # 基础 ASCII
    for i in range(32, 127):
        chars.add(chr(i))

    # 读取常用字
    if os.path.exists(COMMON_CHARS_FILE):
        try:
            with open(COMMON_CHARS_FILE, "r", encoding="utf-8", errors="ignore") as f:
                chars.update(f.read())
            print(f"[1/3] 已加载常用字库: {os.path.basename(COMMON_CHARS_FILE)}")
        except Exception as e:
            print(f"[警告] 读取常用字库失败: {e}")

    # 读取自定义文本
    if CUSTOM_TEXT_FILE and os.path.exists(CUSTOM_TEXT_FILE):
        try:
            with open(CUSTOM_TEXT_FILE, "r", encoding="utf-8", errors="ignore") as f:
                chars.update(f.read())
            print(f"[+] 已加载自定义文本文件: {CUSTOM_TEXT_FILE}")
        except Exception as e:
            print(f"[警告] 读取自定义文本文件失败: {e}")

    # 项目自动扫描
    if ENABLE_PROJECT_SCAN:
        print(f"[2/3] 正在扫描项目 Assets 目录: {ASSETS_DIR}")
        target_exts = ('.prefab', '.unity', '.json', '.csv', '.txt', '.asset', '.xml')
        chinese_pattern = re.compile(r'[\u4e00-\u9fa5\u3000-\u303f\uff00-\uffef]')
        scanned_count = 0
        for root, _, files in os.walk(ASSETS_DIR):
            if SCRIPT_DIR in os.path.abspath(root):
                continue
            for file in files:
                if file.lower().endswith(target_exts):
                    filepath = os.path.join(root, file)
                    scanned_count += 1
                    try:
                        with open(filepath, "r", encoding="utf-8", errors="ignore") as f:
                            chars.update(chinese_pattern.findall(f.read()))
                    except Exception:
                        pass
        print(f"      已扫描 {scanned_count} 个项目文件。")
    else:
        print(f"[2/3] 已跳过扫描 Unity 项目 (ENABLE_PROJECT_SCAN = False)")

    print(f"      最终搜集到 {len(chars)} 个唯一字符。")
    return "".join(chars)


def prune_font(source_path, output_path, text_string):
    print(f"[3/3] 正在执行字体裁剪...")
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
        print(f" 输出文件路径: Assets/Res/Fonts/{os.path.basename(output_path)}")
        print("=" * 45 + "\n")
    except Exception as e:
        print(f"\n[错误] 字体裁剪失败: {e}\n")


if __name__ == "__main__":
    source_font = find_source_font()
    if not source_font:
        print(f"\n[错误] 在 raw_fonts 目录未找到任何字体文件！")
        sys.exit(1)

    all_chars = collect_characters()
    prune_font(source_font, OUTPUT_FONT_PATH, all_chars)
```

---

## ❓ 7. 常见问题排查 (FAQ)

#### Q1: 运行提示 `python: can't open file ... No such file or directory`
* **原因**：运行 `.bat` 时工作目录未切到当前文件夹。
* **解决**：确保 `run.bat` 第一行写有 `cd /d "%~dp0"`。

#### Q2: 玩家输入姓名或特殊生僻字显示方块乱码（缺字）怎么办？
* **解决**：在 Unity 的 TextMeshPro 或 UGUI 字体组件上，设置 `Fallback Font Assets`（备用字体），挂载系统默认字体（如苹方/思源黑体）作为后备渲染。

#### Q3: 为什么裁剪出来的字体在 Unity 里无法替换？
* **解决**：裁剪生成的 `.ttf` 会覆盖在 `Assets/Res/Fonts/msyh_compressed.ttf`。如果是 TextMeshPro 项目，请选中该字体，右键 `Create > TextMeshPro > Font Asset` 重新生成 TMP 字体资源。