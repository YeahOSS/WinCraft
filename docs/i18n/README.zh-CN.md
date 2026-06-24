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
可从仓库的 Releases 页面下载发布产物。

当前提供的产物线如下：

| 产物 | 适用场景 | 原始系统直接可用范围 | 说明 |
| --- | --- | --- | --- |
| `WinCraft-Legacy.exe` | 较旧的 Windows 系统 | Windows Vista 与 Windows 7 | 基于 `.NET Framework 3.0` 的旧系统兼容线 |
| `WinCraft-Standard.exe` | 较新的 Windows 系统 | Windows 8、Windows 8.1、Windows 10、Windows 11 | 基于 `.NET Framework 4.5` 的标准线 |
| `WinCraft-Full-Setup.exe` | 仅依赖系统内置框架，同时尽量走最快启动路径 | Windows Vista 到 Windows 11 | 同时打包 `Standard` 与 `Legacy` 两条 loose-file 运行线；NSIS 安装器会自动选线，并按当前用户安装且不要求管理员权限 |

## 杀毒软件说明
由于 WinCraft 会修改 Windows 系统配置并调用系统 API，部分杀毒软件或安全产品可能出现误报——在以旧版框架编译的版本上，由于传统框架的启发式检测规则更严格，误报风险更高。

如果下载后的文件被拦截、隔离或删除，请先将 WinCraft 所在目录或可执行文件加入白名单后再运行。

## 从源码构建
构建与发布流程说明见 [publish/README.md](../../publish/README.md)。

## 当前状态
目前仓库主要完成了产品基础设施搭建：
- 面向 `.NET Framework 3.0` 与 `.NET Framework 4.5` 的 SDK 风格多目标工程
- 用于补齐不同框架 API 差异的兼容层
- Legacy 与 Standard 两条产物线的单文件打包流程
- 为后续正式发布准备的本地版本与打 tag 流程
