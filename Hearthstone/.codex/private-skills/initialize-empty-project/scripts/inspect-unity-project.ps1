[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectRoot = "."
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RelativePathText {
    param(
        [string]$Root,
        [string]$FullName
    )

    if ($FullName.StartsWith($Root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $FullName.Substring($Root.Length).TrimStart('\', '/')
    }

    return $FullName
}

function Get-RegexCount {
    param(
        [string]$Text,
        [string]$Pattern
    )

    if ([string]::IsNullOrEmpty($Text)) {
        return 0
    }

    return [System.Text.RegularExpressions.Regex]::Matches(
        $Text,
        $Pattern,
        [System.Text.RegularExpressions.RegexOptions]::Multiline
    ).Count
}

$resolvedRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path.TrimEnd('\', '/')
$assetsPath = Join-Path $resolvedRoot "Assets"
$packagesManifestPath = Join-Path $resolvedRoot "Packages/manifest.json"
$projectVersionPath = Join-Path $resolvedRoot "ProjectSettings/ProjectVersion.txt"
$scriptsPath = Join-Path $assetsPath "Scripts"

$hasAssets = Test-Path -LiteralPath $assetsPath -PathType Container
$hasManifest = Test-Path -LiteralPath $packagesManifestPath -PathType Leaf
$hasProjectVersion = Test-Path -LiteralPath $projectVersionPath -PathType Leaf
$isUnityProject = $hasAssets -and $hasManifest -and $hasProjectVersion

$allScriptFiles = @()
if (Test-Path -LiteralPath $scriptsPath -PathType Container) {
    $allScriptFiles = @(Get-ChildItem -LiteralPath $scriptsPath -Recurse -File -Filter "*.cs")
}

$excludedBusinessPathPattern = '(?i)(^|[\\/])(BbxCommon|Plugins|ThirdParty|ExternalLibrary|Generated|Packages)([\\/]|$)'
$businessScriptFiles = @(
    $allScriptFiles | Where-Object {
        $relativePath = Get-RelativePathText -Root $resolvedRoot -FullName $_.FullName
        $relativePath -notmatch $excludedBusinessPathPattern -and
        $_.Name -notmatch '(?i)(\.g|\.generated)\.cs$'
    }
)

$businessSourceParts = @()
foreach ($scriptFile in $businessScriptFiles) {
    try {
        $businessSourceParts += Get-Content -LiteralPath $scriptFile.FullName -Raw -Encoding UTF8
    }
    catch {
        $businessSourceParts += Get-Content -LiteralPath $scriptFile.FullName -Raw
    }
}
$businessSource = $businessSourceParts -join "`n"

$businessRootCandidates = @()
if (Test-Path -LiteralPath $scriptsPath -PathType Container) {
    $businessRootCandidates = @(
        Get-ChildItem -LiteralPath $scriptsPath -Directory |
            Where-Object { $_.Name -notmatch '^(?i:BbxCommon|Plugins|ThirdParty|ExternalLibrary|Generated)$' } |
            ForEach-Object { Get-RelativePathText -Root $resolvedRoot -FullName $_.FullName }
    )
}

$asmdefFiles = @()
$sceneFiles = @()
if ($hasAssets) {
    $asmdefFiles = @(Get-ChildItem -LiteralPath $assetsPath -Recurse -File -Filter "*.asmdef")
    $sceneFiles = @(Get-ChildItem -LiteralPath $assetsPath -Recurse -File -Filter "*.unity")
}

$manifestText = ""
if ($hasManifest) {
    $manifestText = Get-Content -LiteralPath $packagesManifestPath -Raw -Encoding UTF8
}

$hasBbxCommon =
    (Test-Path -LiteralPath (Join-Path $scriptsPath "BbxCommon/BbxCommon.asmdef") -PathType Leaf) -and
    (Test-Path -LiteralPath (Join-Path $scriptsPath "BbxCommon/GameFramework/GameEngineBase.cs") -PathType Leaf)

$typeSignals = [ordered]@{
    GameEngineSubclass = Get-RegexCount -Text $businessSource -Pattern 'GameEngineBase\s*<'
    StageCreation = Get-RegexCount -Text $businessSource -Pattern '\bCreateStage\s*(?:<[^>]+>)?\s*\('
    StageActivation = Get-RegexCount -Text $businessSource -Pattern '\b(?:SetActiveGameStage|LoadStage)\s*\('
    RawComponent = Get-RegexCount -Text $businessSource -Pattern ':\s*EcsRawComponent\b'
    SingletonRawComponent = Get-RegexCount -Text $businessSource -Pattern ':\s*EcsSingletonRawComponent\b'
    RawAspect = Get-RegexCount -Text $businessSource -Pattern ':\s*EcsRawAspect\b'
    EcsSystem = Get-RegexCount -Text $businessSource -Pattern ':\s*Ecs(?:Mix)?SystemBase\b'
    DisableAutoCreation = Get-RegexCount -Text $businessSource -Pattern '\[\s*DisableAutoCreation'
    UiScene = Get-RegexCount -Text $businessSource -Pattern ':\s*UiSceneBase(?:\s*<[^>]+>)?\b'
    UiView = Get-RegexCount -Text $businessSource -Pattern ':\s*(?:UiViewBase|HudViewBase)\b'
    UiController = Get-RegexCount -Text $businessSource -Pattern ':\s*(?:UiControllerBase|HudControllerBase)\s*<'
    BbxScriptableObject = Get-RegexCount -Text $businessSource -Pattern ':\s*BbxScriptableObject\b'
    CsvData = Get-RegexCount -Text $businessSource -Pattern ':\s*CsvDataBase\s*<'
    StageLoadItem = Get-RegexCount -Text $businessSource -Pattern ':\s*IStageLoad\b'
}

$hasEcsSlice =
    (($typeSignals.RawComponent + $typeSignals.SingletonRawComponent) -gt 0) -and
    ($typeSignals.EcsSystem -gt 0) -and
    ($typeSignals.StageActivation -gt 0)
$hasUiSlice =
    ($typeSignals.UiScene -gt 0) -and
    ($typeSignals.UiView -gt 0) -and
    ($typeSignals.UiController -gt 0)
$hasBusinessVerticalSlice =
    ($typeSignals.GameEngineSubclass -gt 0) -and
    ($typeSignals.StageCreation -gt 0) -and
    ($hasEcsSlice -or $hasUiSlice)

if (-not $isUnityProject) {
    $suggestedState = "NonUnityOrWrongRoot"
}
elseif ($businessScriptFiles.Count -eq 0 -and -not $hasBbxCommon) {
    $suggestedState = "BlankUnityShell"
}
elseif ($businessScriptFiles.Count -eq 0 -and $hasBbxCommon) {
    $suggestedState = "FrameworkOnlyShell"
}
elseif (-not $hasBusinessVerticalSlice) {
    $suggestedState = "ScaffoldOrNearEmpty"
}
else {
    $suggestedState = "ReviewAsEstablished"
}

$result = [ordered]@{
    ProjectRoot = $resolvedRoot
    UnityMarkers = [ordered]@{
        Assets = $hasAssets
        PackagesManifest = $hasManifest
        ProjectVersion = $hasProjectVersion
        IsUnityProject = $isUnityProject
    }
    Dependencies = [ordered]@{
        HasBbxCommon = $hasBbxCommon
        HasUnityEntitiesPackage = $manifestText -match '"com\.unity\.entities"\s*:'
    }
    Counts = [ordered]@{
        AllScriptsUnderAssetsScripts = $allScriptFiles.Count
        BusinessScripts = $businessScriptFiles.Count
        AssemblyDefinitions = $asmdefFiles.Count
        Scenes = $sceneFiles.Count
    }
    BusinessRootCandidates = $businessRootCandidates
    TypeSignals = $typeSignals
    HasEcsSliceSignal = $hasEcsSlice
    HasUiSliceSignal = $hasUiSlice
    HasBusinessVerticalSliceSignal = $hasBusinessVerticalSlice
    SuggestedState = $suggestedState
    Warning = "SuggestedState is a heuristic. Read key files and apply references/project-state-classification.md before modifying the project."
}

$result | ConvertTo-Json -Depth 6

