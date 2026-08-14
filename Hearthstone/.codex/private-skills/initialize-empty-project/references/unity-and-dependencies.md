# Unity 与依赖基线

## 目录

1. Unity 根校验
2. 明确依赖的 Unity 包
3. 自动补包流程
4. BbxCommon 与外部资产
5. asmdef 策略
6. 编译检查

## 1. Unity 根校验

项目根至少应包含：

- `Assets/`
- `Packages/manifest.json`
- `ProjectSettings/ProjectVersion.txt`

缺少任一关键标记时，先确认工作目录。不要由本 skill 创建 Unity 项目根、`manifest.json` 或 ProjectSettings；让用户通过 Unity Hub/Editor 创建正确版本的项目后再继续。

当前 BbxCommon 基线来自 Unity `2022.2.7f1c1` 工程。遇到不同 Unity 大版本时，仍先使用本基线做冲突检查，但不要强制覆盖已有包版本；应在目标 Unity 中验证兼容性。

## 2. 明确依赖的 Unity 包

从当前 `BbxCommon.asmdef` 的实际引用、BbxCommon 源码和已解析的 `packages-lock.json` 可确认以下直接依赖：

| manifest 依赖 | 基线版本 | 直接证据 |
|---|---:|---|
| `com.unity.entities` | `1.0.0-pre.65` | 引用 `Unity.Entities`、`Unity.Transforms`；ECS API 和 System 直接使用 |
| `com.unity.textmeshpro` | `3.0.6` | asmdef 引用 `Unity.TextMeshPro`；UI 代码直接使用 `TMPro` |
| `com.unity.ugui` | `1.0.0` | UI 代码直接使用 `UnityEngine.UI` 和 `UnityEngine.EventSystems`；TextMeshPro 也依赖 UGUI |

`com.unity.entities` 会由 Unity Package Manager 传递安装与该版本匹配的：

- `com.unity.collections`；
- `com.unity.mathematics`；
- `com.unity.burst`；
- `com.unity.serialization`；
- `com.unity.scriptablebuildpipeline`；
- 其它 Entities 声明的依赖。

BbxCommon asmdef 虽直接引用 Collections 和 Mathematics 程序集，但不要把这些传递包再次写成顶层 manifest 版本，否则可能锁住与 Entities 不兼容的组合。以 `packages-lock.json` 的解析结果验证它们已安装。

以下包不是 BbxCommon 基线必需项，不要默认添加：

- Input System；当前框架占位流程不依赖它；
- AI Navigation、Timeline、Visual Scripting、Cinemachine；
- Unity Test Framework；只有决定创建测试程序集时再添加；
- IDE 集成包；由用户的编辑器选择决定。

## 3. 自动补包流程

依赖清单位于 `assets/required-unity-packages.json`。先预览：

```powershell
python .codex/private-skills/initialize-empty-project/scripts/ensure-unity-packages.py --project-root . --dry-run
```

没有冲突时执行：

```powershell
python .codex/private-skills/initialize-empty-project/scripts/ensure-unity-packages.py --project-root .
```

脚本行为：

1. 只读取和更新 `Packages/manifest.json` 的 `dependencies`；
2. 只添加缺失包；
3. 已存在且版本相同则保持不变；
4. 已存在但版本不同则返回 `version-conflict` 和退出码 2，整个 manifest 不写入；
5. 不创建或修改 `packages-lock.json`；
6. 使用临时文件替换 manifest，避免写入半截 JSON。

实际执行后让 Unity 打开项目并等待 Package Manager 解析。由 Unity 自动生成/更新 `packages-lock.json`，再检查 Entities、Collections、Mathematics、Burst、TextMeshPro 和 UGUI 都已解析。

不要在初始化过程中顺手升级到“最新版本”。本 skill 保存的是当前 BbxCommon 已验证基线；版本升级是独立任务。

## 4. BbxCommon 与外部资产

至少检查：

- `Assets/Scripts/BbxCommon/BbxCommon.asmdef`
- `Assets/Scripts/BbxCommon/CrossLibrary/CrossLibrary.asmdef`
- `Assets/Scripts/BbxCommon/ExternalLibrary/UniTask/Runtime/UniTask.asmdef`
- `Assets/Scripts/BbxCommon/GameFramework/GameEngineBase.cs`
- `Assets/Scripts/BbxCommon/GameFramework/GameStage.cs`
- `Assets/Scripts/BbxCommon/Api/EcsApi.cs`
- `Assets/Scripts/BbxCommon/Ui/Mvc/UiControllerBase.cs`

当前框架的依赖来源：

| 依赖 | 来源 | 初始化处理 |
|---|---|---|
| BbxCommon | 项目框架源码 | 缺失时询问导入来源，不仿写 |
| CrossLibrary | BbxCommon 源码目录 | 不通过 UPM 安装 |
| UniTask | BbxCommon `ExternalLibrary` 目录 | 不通过 UPM 重复安装 |
| Odin Inspector | `Assets/Plugins/Sirenix/Odin Inspector` 外部资产 | 当前 BbxCommon 源码直接使用；缺失时停止并要求用户导入，manifest 脚本无法安装 |

目录存在但编译失败不算框架可用。不要从另一个项目复制零散 BbxCommon/Odin 文件拼装依赖。

## 5. asmdef 策略

默认占位模板创建业务 asmdef：

```json
{
  "name": "<ProjectNamespace>",
  "rootNamespace": "<ProjectNamespace>",
  "references": [
    "BbxCommon",
    "CrossLibrary",
    "Unity.Entities",
    "Unity.Collections",
    "Unity.TextMeshPro"
  ]
}
```

模板资产包含完整 asmdef 字段，此处只展示关键引用。

业务 asmdef 必须声明业务源码实际需要的**直接程序集依赖**。Unity asmdef 引用不是业务层可以依赖的传递引用：即使 `BbxCommon.asmdef` 已引用 `CrossLibrary` 或 `Unity.Collections`，业务代码通过 BbxCommon 的公开基类、接口、字段或方法签名触达这些类型时，业务 asmdef 仍可能需要显式引用对应程序集。默认占位模板已验证需要：

- `BbxCommon`；
- `CrossLibrary`；
- `Unity.Entities`；
- `Unity.Collections`；
- `Unity.TextMeshPro`。

不要只根据 `using` 行判断依赖。还要检查继承链、泛型约束、方法参数/返回值、字段类型和属性类型。模板或业务代码新增 `UnityEngine.UI` 等其它程序集类型时，同步补充其直接 asmdef 引用，例如 `Unity.ugui`。

如果近空项目已有业务 asmdef，检查和补齐现有文件，不创建第二份。如果现有项目明确使用 Assembly-CSharp，可跳过业务 asmdef，但要记录这是用户/项目已有选择。

禁止复制其他项目的程序集 GUID。若当前 Unity 不能按程序集名称解析，使用 Unity Inspector 选择当前项目中的实际程序集。

## 6. 编译检查

按顺序检查：

1. Unity Package Manager 完成解析且 Console 没有包错误；
2. BbxCommon、CrossLibrary、UniTask 和 Odin 编译；
3. 按业务源码与公开类型链核对业务 asmdef 的全部直接程序集依赖；默认模板至少包含 BbxCommon、CrossLibrary、Unity.Entities、Unity.Collections 和 Unity.TextMeshPro；
4. GameEngine/Stage 编译；
5. ECS Component/System 编译；
6. UiScene/View/Controller 编译。

遇到错误先停止扩建：

- 框架类型不可见：修包或 asmdef，不复制类型；
- 依赖程序集中的类型/namespace 不可见：先确认缺失的是业务 asmdef 直接引用，不要因为依赖包已安装就假设程序集也已可见；
- 同名类型重复：检查旧占位结构和程序集边界；
- Editor 类型进入 Runtime：拆 Editor 程序集或移除引用；
- Entities API 不匹配：核对基线包与当前 BbxCommon，不按最新 API 猜改；
- namespace 错误：统一根 namespace，不用全局 namespace 临时绕过。
