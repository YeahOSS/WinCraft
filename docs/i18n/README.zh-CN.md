# WinCraft

[English](../../README.md) | 简体中文

WinCraft 是一个面向 Windows 系统配置优化与日常使用体验改善的工具箱项目。

当前项目仍处于基础框架搭建阶段。仓库已经具备多目标构建、发布流程与兼容层，但面向终端用户的功能还在持续补充中。

## 预告功能
- 上下文菜单管理
- 文件关联管理
- 文件资源管理器优化与整理
- 更多用于改善日常体验的 Windows 配置优化能力

## 下载说明
可从仓库的 [Releases](https://github.com/YeahOSS/WinCraft/releases) 页面下载发布产物。

### 安装包（推荐） — `WinCraft-Setup.exe`

- **自动检测 .NET Framework 版本。** 安装时读取注册表：.NET 4.5+ 使用 `net45` 线，旧系统回退 `net30` 线。
- **启动性能更好。** 安装版构建不包含 overlay 解压代码，体积更小、启动路径更短。

### 便携版 — `WinCraft-Standard.exe` / `WinCraft-Legacy.exe`

适合不需要安装器、或需要针对特定框架线部署的用户：

| 产物 | 目标框架 | 适用系统 |
| --- | --- | --- |
| `WinCraft-Standard.exe` | .NET Framework 4.5 | Windows 8 / 8.1 / 10 / 11 |
| `WinCraft-Legacy.exe` | .NET Framework 3.0 | Windows Vista / 7 |

## 杀毒软件说明
由于 WinCraft 会修改 Windows 系统配置并调用系统 API，部分杀毒软件或安全产品可能出现误报——在以旧版框架编译的版本上，由于传统框架的启发式检测规则更严格，误报风险更高。

如果下载后的文件被拦截、隔离或删除，请先将 WinCraft 所在目录或可执行文件加入白名单后再运行。

## 从源码构建
构建与发布流程说明见 [publish/README.md](../../publish/README.md)。

## 当前状态
目前仓库主要完成了产品基础设施搭建：
- 面向 `.NET Framework 3.0` 与 `.NET Framework 4.5` 的 SDK 风格多目标工程
- 用于补齐不同框架 API 差异的兼容层
- PE overlay 单文件打包流程（单文件版）
- NSIS 安装器，自动适配系统运行线（推荐分发方式）
- 为后续正式发布准备的本地版本与打 tag 流程
