# WinCraft

[![GitHub Release](https://img.shields.io/github/v/release/YeahOSS/WinCraft?style=flat)](https://github.com/YeahOSS/WinCraft/releases) [![GitHub Downloads](https://img.shields.io/github/downloads/YeahOSS/WinCraft/total?style=flat)](https://github.com/YeahOSS/WinCraft/releases) [![GitHub Stars](https://img.shields.io/github/stars/YeahOSS/WinCraft?style=flat)](https://github.com/YeahOSS/WinCraft/stargazers) [![License](https://img.shields.io/badge/license-MIT-green?style=flat)](../../LICENSE) [![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=flat)](https://github.com/YeahOSS/WinCraft) [![.NET](https://img.shields.io/badge/.NET-3.0%20%7C%204.5-512BD4?style=flat)](https://dotnet.microsoft.com)

<br>
<p align="center">
  <img src="../../assets/social-preview.png" alt="WinCraft" width="800">
</p>

[English](../../README.md) | 简体中文

WinCraft 是一个面向 Windows 系统配置优化与日常使用体验改善的工具箱项目。

当前项目仍处于基础框架搭建阶段。仓库已经具备多目标构建、发布流程与兼容层，但面向终端用户的功能还在持续补充中。

## 📜 项目由来

WinCraft 延续了 [ContextMenuManager](https://github.com/BluePointLilac/ContextMenuManager)
的工作。我因丢失原账户的 2FA 凭据无法继续维护，故迁移至新账户并以更广泛的定位重新出发。

> 😤 GitHub 的 2FA 把黑客挡在门外，也顺手把
> [原作者](https://github.com/BluePointLilac/ContextMenuManager/commits/master) 挡在了门外。
> 好在提交历史还在——那是非 AI 时代的荣耀勋章。

| 原项目 | 现项目 |
| --- | --- |
| <img src="https://raw.githubusercontent.com/BluePointLilac/ContextMenuManager/master/ContextMenuManager/Properties/AppIcon.ico" width="16" height="16" style="vertical-align: middle"> ContextMenuManager | <img src="../../assets/app.ico" width="16" height="16" style="vertical-align: middle"> WinCraft |
| BluePointLilac（2FA 丢失） | YeahOSS |
| 仅上下文菜单 | 更全面的 Windows 工具箱 |
| WinForms | WPF — 更现代更美观 |

## 🚧 预告功能
- [ ] 上下文菜单管理
- [ ] 文件关联管理
- [ ] 文件资源管理器优化与整理
- [ ] 更多用于改善日常体验的 Windows 配置优化能力

## 📥 下载说明

| 平台 | 下载 |
| --- | --- |
| GitHub | [Releases](https://github.com/YeahOSS/WinCraft/releases) |
| Gitee（镜像） | [Releases](https://gitee.com/YeahOSS/WinCraft/releases) |

### 安装包（推荐）

| 格式 | 文件 | 适用对象 |
| --- | --- | --- |
| 安装程序 | [WinCraft-Setup.exe](https://github.com/YeahOSS/WinCraft/releases/latest/download/WinCraft-Setup.exe) | 个人用户，交互式安装 |
| MSI 包 | [WinCraft-Setup.msi](https://github.com/YeahOSS/WinCraft/releases/latest/download/WinCraft-Setup.msi) | 企业部署，组策略分发 |

部署命令见 [docs/installer-guide.md](../../docs/installer-guide.md)。

- **自动检测 .NET Framework 版本。** 安装时读取注册表：.NET 4.5+ 使用 `net45` 线，旧系统回退 `net30` 线。
- **启动更快。** 安装版文件直接存放于磁盘，无需 LZMA 解压，且附带优化过的运行时配置。

### 便携版

| 格式 | 文件 | 目标框架 | 适用系统 |
| --- | --- | --- | --- |
| 标准版 | [WinCraft-Standard.exe](https://github.com/YeahOSS/WinCraft/releases/latest/download/WinCraft-Standard.exe) | .NET Framework 4.5 | Windows 8 / 8.1 / 10 / 11 |
| 旧版 | [WinCraft-Legacy.exe](https://github.com/YeahOSS/WinCraft/releases/latest/download/WinCraft-Legacy.exe) | .NET Framework 3.0 | Windows Vista / 7 |

> [!WARNING]
> 由于 WinCraft 会修改 Windows 系统配置并调用系统 API，部分杀毒软件或安全产品可能出现误报——在以旧版框架编译的版本上，由于传统框架的启发式检测规则更严格，误报风险更高。
>
> 运行前请先将 WinCraft 所在目录或可执行文件加入安全软件白名单。

## 🔨 从源码构建
构建与发布流程说明见 [publish/README.md](../../publish/README.md)。

## 🤝 致谢

WinCraft 建立在以下开源项目的基础之上：

- [7-Zip LZMA SDK](https://www.7-zip.org/sdk.html) — Igor Pavlov（公共领域）— 高效的 LZMA 压缩算法
- [Theraot](https://github.com/theraot/Theraot) — 为 `net30` 回填缺失的 .NET API
- [CsWin32](https://github.com/microsoft/CsWin32) — Microsoft — 源码生成的 Win32 P/Invoke
- [NSIS](https://nsis.sourceforge.io/) — Nullsoft — 灵活的 Windows 安装器
- [WiX Toolset](https://wixtoolset.org/) — .NET Foundation — Windows Installer XML (MSI) 打包

[![Star History](https://api.star-history.com/svg?repos=YeahOSS/WinCraft&type=Date)](https://star-history.com/#YeahOSS/WinCraft&Date)

