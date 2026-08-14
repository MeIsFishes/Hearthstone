# design_doc_writer 项目扩展

本扩展只增加当前项目的设计文档规则，不扩大 `design_doc_writer` 在底层 TOML 中声明的读写范围。

创建或更新 `AutoDoc/Design/` 下的敌机、我方舰船设计文档时，必须先读取并遵循 [单位设计文档项目 Skill](../skills/unit-design-docs/SKILL.md)。敌机与我方舰船同时出现时，分别按该 skill 指定的格式处理。
