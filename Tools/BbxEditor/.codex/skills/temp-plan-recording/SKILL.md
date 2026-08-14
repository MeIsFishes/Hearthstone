---
name: temp-plan-recording
description: 当任务需要分步执行、分阶段确认或用户要求记录步骤时，使用 TempPlan 记录执行过程。
---

# TempPlan Recording

当用户或适用规则要求分步执行、分阶段执行、在步骤之间确认，或明确要求记录执行步骤时，先创建 TempPlan，再开始实质性工作。

## TempPlan 路径

1. TempPlan 文件必须创建在项目根目录的 `AutoDoc/Temp/` 下。
2. 文件名可根据任务自行命名，推荐使用能说明任务的英文短横线文件名，例如 `AutoDoc/Temp/update-program-doc-temp-plan.md`。
3. 如果 `AutoDoc/Temp/` 不存在，先创建该目录。

## 记录格式

1. 将计划步骤写成 Markdown 检查清单。
2. 步骤描述应尽量完整，不要为了简短而省略关键条件、目标文件或确认点。
3. 每完成一步，立即更新 TempPlan：
   - 将该步骤从 `[ ]` 改为 `[x]`。
   - 在步骤下方补充简短总结，说明完成了什么。
4. 每完成一步并更新 TempPlan 后，立即执行项目根目录下的 `AutoDoc/CleanupTempDocs.bat`。
5. 如果下一步存在歧义、缺少必要信息，或需要用户做决定，在执行下一步前先询问用户。
6. 除非用户或适用规则明确允许，不要一次执行多个需要逐步确认的步骤。

## 插入新步骤

当执行到某一步时，如果用户、skill 或项目规则要求插入新的步骤：

1. 将新步骤插入到当前步骤之后。
2. 保持后续步骤顺序不丢失。
3. 如果原计划使用编号文本，更新后续步骤编号。

## 清理临时文档

每次完成一个步骤并更新 TempPlan 后，都必须执行项目根目录下的 `AutoDoc/CleanupTempDocs.bat`，用于清理 `AutoDoc/Temp/` 中过多的临时文档。
