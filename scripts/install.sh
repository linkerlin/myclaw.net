#!/bin/bash
# MyClaw.NET 一键安装脚本 (Linux/macOS)
# 用法: curl -sSL https://raw.githubusercontent.com/your-org/myclaw.net/main/scripts/install.sh | bash

set -e

# 配置
REPO="your-org/myclaw.net"
INSTALL_DIR="${INSTALL_DIR:-$HOME/.local/bin}"
VERSION="${VERSION:-latest}"

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 日志函数
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检测平台
detect_platform() {
    local os arch
    
    # 检测操作系统
    case "$(uname -s)" in
        Linux*)     os="linux" ;;
        Darwin*)    os="osx" ;;
        CYGWIN*|MINGW*|MSYS*) 
            log_error "Windows 系统请使用 PowerShell 安装脚本"
            exit 1
            ;;
        *)          
            log_error "不支持的操作系统: $(uname -s)"
            exit 1
            ;;
    esac
    
    # 检测架构
    case "$(uname -m)" in
        x86_64|amd64)   arch="x64" ;;
        arm64|aarch64)  arch="arm64" ;;
        armv7l)         arch="arm" ;;
        *)              
            log_error "不支持的架构: $(uname -m)"
            exit 1
            ;;
    esac
    
    echo "${os}-${arch}"
}

# 检查依赖
check_dependencies() {
    local deps=("curl" "chmod" "mkdir")
    
    for dep in "${deps[@]}"; do
        if ! command -v "$dep" &> /dev/null; then
            log_error "缺少依赖: $dep"
            exit 1
        fi
    done
}

# 下载二进制
download_binary() {
    local platform="$1"
    local version="$2"
    local install_dir="$3"
    local binary_name="myclaw"
    
    local version_tag
    if [ "$version" = "latest" ]; then
        version_tag="latest/download"
    else
        version_tag="download/$version"
    fi
    
    local url="https://github.com/${REPO}/releases/${version_tag}/myclaw-${platform}"
    local output_path="${install_dir}/${binary_name}"
    
    log_info "下载 MyClaw.NET ${version} for ${platform}..."
    log_info "URL: ${url}"
    
    # 创建安装目录
    if [ ! -d "$install_dir" ]; then
        log_info "创建安装目录: ${install_dir}"
        mkdir -p "$install_dir"
    fi
    
    # 下载
    if ! curl -fsSL --progress-bar "$url" -o "$output_path"; then
        log_error "下载失败，请检查网络连接或版本号是否正确"
        exit 1
    fi
    
    # 添加执行权限
    chmod +x "$output_path"
    
    log_success "下载完成: ${output_path}"
}

# 验证安装
verify_installation() {
    local install_dir="$1"
    local binary_path="${install_dir}/myclaw"
    
    if [ ! -f "$binary_path" ]; then
        log_error "安装验证失败: 找不到二进制文件"
        exit 1
    fi
    
    # 尝试获取版本
    if "${binary_path}" --version &> /dev/null; then
        local version
        version=$("${binary_path}" --version 2>/dev/null || echo "unknown")
        log_success "MyClaw.NET ${version} 安装成功!"
    else
        log_warning "无法验证版本，但二进制文件已安装"
    fi
}

# 检查 PATH
check_path() {
    local install_dir="$1"
    
    case ":$PATH:" in
        *":${install_dir}:"*)
            return 0
            ;;
    esac
    
    log_warning "${install_dir} 不在 PATH 中"
    echo ""
    echo "请将以下行添加到你的 shell 配置文件中:"
    echo "    export PATH=\"\$PATH:${install_dir}\""
    echo ""
    echo "例如:"
    echo "    echo 'export PATH=\"\$PATH:${install_dir}\"' >> ~/.bashrc"
    echo "    source ~/.bashrc"
}

# 打印使用说明
print_usage() {
    echo ""
    echo "=========================================="
    echo "MyClaw.NET 安装完成!"
    echo "=========================================="
    echo ""
    echo "使用说明:"
    echo "  myclaw --help          显示帮助信息"
    echo "  myclaw status          查看系统状态"
    echo "  myclaw onboard         初始化配置"
    echo ""
    echo "MCP 服务:"
    echo "  myclaw mcp             启动 MCP 服务"
    echo ""
    echo "更多信息:"
    echo "  https://github.com/${REPO}"
    echo "=========================================="
}

# 主函数
main() {
    echo "=========================================="
    echo "MyClaw.NET 安装脚本"
    echo "=========================================="
    echo ""
    
    # 检测平台
    log_info "检测平台..."
    PLATFORM=$(detect_platform)
    log_success "检测到平台: ${PLATFORM}"
    
    # 检查依赖
    log_info "检查依赖..."
    check_dependencies
    log_success "依赖检查通过"
    
    # 下载
    download_binary "$PLATFORM" "$VERSION" "$INSTALL_DIR"
    
    # 验证
    verify_installation "$INSTALL_DIR"
    
    # 检查 PATH
    check_path "$INSTALL_DIR"
    
    # 打印使用说明
    print_usage
}

# 处理命令行参数
while [[ $# -gt 0 ]]; do
    case $1 in
        --version|-v)
            VERSION="$2"
            shift 2
            ;;
        --dir|-d)
            INSTALL_DIR="$2"
            shift 2
            ;;
        --help|-h)
            echo "MyClaw.NET 安装脚本"
            echo ""
            echo "用法:"
            echo "  install.sh [选项]"
            echo ""
            echo "选项:"
            echo "  -v, --version <版本>    指定版本 (默认: latest)"
            echo "  -d, --dir <目录>        安装目录 (默认: ~/.local/bin)"
            echo "  -h, --help              显示此帮助"
            echo ""
            echo "环境变量:"
            echo "  INSTALL_DIR             安装目录"
            echo "  VERSION                 版本号"
            exit 0
            ;;
        *)
            log_error "未知选项: $1"
            exit 1
            ;;
    esac
done

# 运行主函数
main
