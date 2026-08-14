---
name: project-overview-config
description: 项目类型理解变化时更新项目总览配置。
---

# Project Overview Config

当你发现当前项目类型发生改变，或者你对项目的理解比 `AutoDoc/ProjectOverview.md` 中的 `ProjectDesc` 更深刻，并且现有描述已经不符合当前项目状态时，使用本 skill。

## 更新目标

维护 `AutoDoc/ProjectOverview.md`，保持其中两项配置准确：

1. `ProjectNamespace`：当前项目编写 C# 业务代码时使用的项目级 namespace。
2. `ProjectDesc`：用用户的语言说明当前项目的类型和概况；中文不超过 150 字，英文不超过 100 词。

## 更新规则

1. 只在描述与当前项目状态不符时更新，不因措辞偏好反复改写。
2. 判断项目级 namespace 时，优先看 `Assets/Scripts/` 下业务代码的顶层 namespace；不要把底层框架、第三方库或编辑器扩展 namespace 当作项目 namespace。
3. `ProjectDesc` 只写当前已经能从项目代码、配置或 AutoDoc 文档确认的事实。
4. 如果项目已经从单一战斗原型扩展为新的类型，或者主要玩法、核心系统、目标平台发生明确变化，应同步改写 `ProjectDesc`。
5. 更新后检查 `ProjectNamespace` 与 `ProjectDesc` 两项仍然存在，且字数限制满足要求。
