# BbxDeployer 项目总览

BbxDeployer 是一个面向 Windows 的 .NET 桌面工具，用于把公共工具和 Unity 游戏底层库从一个源仓库覆盖同步到多个游戏仓库。

项目包含以下系统：

- 项目发现与配置：识别仓库根和 Unity 项目根，不依赖游戏文件夹名称。
- Unity 工程创建：从 Unity Hub 安装位置发现本机 Editor 版本，使用所选版本建立新的目标工程。
- 同步规划与校验：管理可勾选的目录映射，预览文件数量、体积、冲突和依赖状态。
- 文件同步：向多个目标逐一执行覆盖复制，创建缺失目录，不删除目标独有文件。
- 桌面界面：使用英文 WPF 主面板展示 Main Project 与目标项目根目录，通过左下角 Settings 管理相对项目根的转移目录。

主要功能是同步 `Tools/`、BbxCommon 源码及其 Odin Inspector 外部依赖，允许在白名单目录内部嵌套黑名单，支持将 `.gitignore` 规则导入黑名单，并自动处理 Unity `.meta` 文件。对新项目，工具会让 Unity 生成基础结构，再使用主项目的 `Packages/manifest.json` 和 `packages-lock.json` 统一包依赖。
