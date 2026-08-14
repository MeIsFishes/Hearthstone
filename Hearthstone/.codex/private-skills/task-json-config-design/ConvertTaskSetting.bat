@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -Command "$scriptArgs=$args; $text=[System.IO.File]::ReadAllText('%~f0'); $parts=[regex]::Split($text, '(?m)^# POWERSHELL\r?$'); if ($parts.Count -lt 2) { Write-Error 'PowerShell payload not found.'; exit 1 }; & ([scriptblock]::Create($parts[1])) @scriptArgs" -- %*
exit /b %ERRORLEVEL%

# POWERSHELL
param(
    [string]$Direction,
    [string]$Folder,
    [string]$TaskKey
)

$ErrorActionPreference = 'Stop'

function Show-Usage {
    Write-Host 'Usage: ConvertTaskSetting.bat <direction> <folder> <taskKey>'
    Write-Host '  direction 0: .task.setting -> .json/.editor.json'
    Write-Host '  direction 1: .json -> .task.setting'
    Write-Host 'Example: ConvertTaskSetting.bat 1 "Mods/Native/Task/" "ActiveFireball"'
}

function Resolve-TaskFolder([string]$PathText) {
    if ([string]::IsNullOrWhiteSpace($PathText)) {
        throw 'Folder argument is empty.'
    }
    $resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PathText)
    return $resolved
}

function Get-JsonApiListValues($JsonList) {
    $values = New-Object System.Collections.Generic.List[object]
    if ($null -eq $JsonList) {
        return $values
    }

    $index = 0
    while ($true) {
        $prop = $JsonList.PSObject.Properties["$index"]
        if ($null -eq $prop) {
            break
        }
        $values.Add($prop.Value)
        $index++
    }
    return $values
}

function Get-JsonApiDictionaryEntries($JsonDictionary) {
    $entries = New-Object System.Collections.Generic.List[object]
    if ($null -eq $JsonDictionary) {
        return $entries
    }

    $index = 0
    while ($true) {
        $keyProp = $JsonDictionary.PSObject.Properties["$index, Key"]
        $valueProp = $JsonDictionary.PSObject.Properties["$index, Value"]
        if ($null -eq $keyProp) {
            break
        }
        if ($null -eq $valueProp) {
            throw "JsonApi dictionary entry $index is missing Value."
        }
        $entries.Add([pscustomobject]@{
            Key = $keyProp.Value
            Value = $valueProp.Value
        })
        $index++
    }
    return $entries
}

function Get-ValueSourceName($ValueSource) {
    if ($null -eq $ValueSource) {
        return 'Value'
    }
    $valueProp = $ValueSource.PSObject.Properties['Value']
    if ($null -ne $valueProp) {
        return [string]$valueProp.Value
    }
    return [string]$ValueSource
}

function Get-ShortTypeName([string]$FullTypeName) {
    if ([string]::IsNullOrWhiteSpace($FullTypeName)) {
        return 'TaskNode'
    }
    $plusIndex = $FullTypeName.LastIndexOf('+')
    $dotIndex = $FullTypeName.LastIndexOf('.')
    $index = [Math]::Max($plusIndex, $dotIndex)
    if ($index -ge 0 -and $index + 1 -lt $FullTypeName.Length) {
        return $FullTypeName.Substring($index + 1)
    }
    return $FullTypeName
}

function Get-ContextConfigName([string]$ContextTypeName) {
    if ([string]::IsNullOrWhiteSpace($ContextTypeName)) {
        return $ContextTypeName
    }
    return Get-ShortTypeName $ContextTypeName
}

function Assert-ValueSourceName([string]$Source, [string]$Owner) {
    if (@('Value', 'Context', 'Blackboard') -notcontains $Source) {
        throw "$Owner Source must be one of Value, Context, Blackboard, but got '$Source'. Use enum member names, not enum type names."
    }
}

function Convert-ScalarValue([string]$Text) {
    if ($null -eq $Text) {
        return $null
    }
    if ($Text -eq '') {
        return ''
    }
    if ($Text -ieq 'true') {
        return $true
    }
    if ($Text -ieq 'false') {
        return $false
    }

    $intValue = 0
    if ([int]::TryParse($Text, [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$intValue)) {
        return $intValue
    }

    $doubleValue = 0.0
    if ([double]::TryParse($Text, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$doubleValue)) {
        return $doubleValue
    }

    return $Text
}

function Convert-FieldValue([string]$Source, [string]$RawValue) {
    if ($Source -ne 'Value') {
        return $RawValue
    }
    if ($null -eq $RawValue) {
        return $null
    }
    if ($RawValue.Contains('%||%')) {
        $items = @()
        foreach ($item in ($RawValue -split [regex]::Escape('%||%'))) {
            if ($item -eq '') {
                continue
            }
            $items += (Convert-ScalarValue $item)
        }
        return ,$items
    }
    return Convert-ScalarValue $RawValue
}

function Convert-NameToSafeIdentifier([string]$Name) {
    $safe = $Name -replace '[^A-Za-z0-9_]', '_'
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return 'TaskNode'
    }
    return $safe
}

function New-FullTypeInfo([string]$FullType) {
    return [ordered]@{
        FullType = $FullType
    }
}

function New-SpecialTypeInfo([string]$SpecialType) {
    return [ordered]@{
        SpecialType = $SpecialType
    }
}

function New-ListTypeInfo($GenericType1) {
    return [ordered]@{
        SpecialType = 'List'
        GenericType1 = $GenericType1
    }
}

function New-DictionaryTypeInfo($GenericType1, $GenericType2) {
    return [ordered]@{
        SpecialType = 'Dictionary'
        GenericType1 = $GenericType1
        GenericType2 = $GenericType2
    }
}

function New-TypedObject([string]$FullType) {
    return [ordered]@{
        'Default.TypeInfo' = New-FullTypeInfo $FullType
    }
}

function New-EnumValueSource([string]$Source) {
    return [ordered]@{
        'Default.TypeInfo' = New-FullTypeInfo 'BbxCommon.ETaskFieldValueSource'
        Value = $Source
    }
}

function New-JsonApiList($Items, $GenericTypeInfo) {
    $list = [ordered]@{
        'Default.TypeInfo' = New-ListTypeInfo $GenericTypeInfo
    }
    $index = 0
    foreach ($item in @($Items)) {
        $list["$index"] = $item
        $index++
    }
    return $list
}

function New-JsonApiDictionary($Entries, $GenericType1, $GenericType2) {
    $dictionary = [ordered]@{
        'Default.TypeInfo' = New-DictionaryTypeInfo $GenericType1 $GenericType2
    }
    $index = 0
    foreach ($entry in @($Entries)) {
        $dictionary["$index, Key"] = $entry.Key
        $dictionary["$index, Value"] = $entry.Value
        $index++
    }
    return $dictionary
}

function Convert-SettingValueToTaskString($Value) {
    if ($null -eq $Value) {
        return ''
    }
    if (($Value -is [System.Array]) -or ($Value -is [System.Collections.IList] -and -not ($Value -is [string]))) {
        $parts = @()
        foreach ($item in $Value) {
            if ($null -eq $item) {
                $parts += ''
            }
            else {
                $parts += [string]$item
            }
        }
        if ($parts.Count -eq 0) {
            return ''
        }
        return (($parts | ForEach-Object { $_ + '%||%' }) -join '')
    }
    if ($Value -is [System.Collections.IDictionary] -or $Value -is [pscustomobject]) {
        return ($Value | ConvertTo-Json -Depth 100 -Compress)
    }
    return [string]$Value
}

function New-TaskFieldInfo([string]$FieldName, [string]$Source, [string]$Value) {
    Assert-ValueSourceName $Source "Field $FieldName"
    $fieldInfo = New-TypedObject 'BbxCommon.TaskFieldInfo'
    $fieldInfo.FieldName = $FieldName
    $fieldInfo.ValueSource = New-EnumValueSource $Source
    $fieldInfo.Value = $Value
    return $fieldInfo
}

function Get-ObjectPropertyValue($Object, [string]$Name, $DefaultValue) {
    if ($null -eq $Object) {
        return $DefaultValue
    }
    if ($Object -is [System.Collections.IDictionary]) {
        if ($Object.ContainsKey($Name)) {
            return ,$Object[$Name]
        }
        return $DefaultValue
    }
    $prop = $Object.PSObject.Properties[$Name]
    if ($null -eq $prop) {
        return $DefaultValue
    }
    return ,$prop.Value
}

function Get-RequiredObjectPropertyValue($Object, [string]$Name, [string]$Owner) {
    if ($Object -is [System.Collections.IDictionary]) {
        if (-not $Object.ContainsKey($Name)) {
            throw "$Owner is missing required property '$Name'."
        }
        return ,$Object[$Name]
    }
    $prop = $Object.PSObject.Properties[$Name]
    if ($null -eq $prop) {
        throw "$Owner is missing required property '$Name'."
    }
    return ,$prop.Value
}

function Get-ObjectPropertyNames($Object) {
    if ($null -eq $Object) {
        return @()
    }
    if ($Object -is [System.Collections.IDictionary]) {
        return @($Object.Keys | ForEach-Object { [string]$_ })
    }
    return @($Object.PSObject.Properties.Name)
}

function Read-JsonFilePreserveArrays([string]$Path) {
    Add-Type -AssemblyName System.Web.Extensions
    $serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
    $serializer.MaxJsonLength = [int]::MaxValue
    $raw = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    return $serializer.DeserializeObject($raw)
}

function Get-NextNonWhitespaceCharIndex([string]$Text, [int]$StartIndex) {
    for ($i = $StartIndex; $i -lt $Text.Length; $i++) {
        if (-not [char]::IsWhiteSpace($Text[$i])) {
            return $i
        }
    }
    return -1
}

function Append-JsonIndent([System.Text.StringBuilder]$Builder, [int]$Indent) {
    $null = $Builder.Append((' ' * ($Indent * 4)))
}

function Format-JsonString([string]$Json) {
    $builder = New-Object System.Text.StringBuilder
    $indent = 0
    $inString = $false
    $escaped = $false

    for ($i = 0; $i -lt $Json.Length; $i++) {
        $char = $Json[$i]

        if ($inString) {
            $null = $builder.Append($char)
            if ($escaped) {
                $escaped = $false
            }
            elseif ($char -eq '\') {
                $escaped = $true
            }
            elseif ($char -eq '"') {
                $inString = $false
            }
            continue
        }

        if ([char]::IsWhiteSpace($char)) {
            continue
        }

        switch ($char) {
            '"' {
                $inString = $true
                $null = $builder.Append($char)
                break
            }
            { $_ -eq '{' -or $_ -eq '[' } {
                $closeChar = if ($char -eq '{') { '}' } else { ']' }
                $nextIndex = Get-NextNonWhitespaceCharIndex $Json ($i + 1)
                if ($nextIndex -ge 0 -and $Json[$nextIndex] -eq $closeChar) {
                    $null = $builder.Append($char)
                    $null = $builder.Append($closeChar)
                    $i = $nextIndex
                }
                else {
                    $null = $builder.Append($char)
                    $null = $builder.Append([Environment]::NewLine)
                    $indent++
                    Append-JsonIndent $builder $indent
                }
                break
            }
            { $_ -eq '}' -or $_ -eq ']' } {
                $null = $builder.Append([Environment]::NewLine)
                $indent--
                Append-JsonIndent $builder $indent
                $null = $builder.Append($char)
                break
            }
            ',' {
                $null = $builder.Append($char)
                $null = $builder.Append([Environment]::NewLine)
                Append-JsonIndent $builder $indent
                break
            }
            ':' {
                $null = $builder.Append(': ')
                break
            }
            default {
                $null = $builder.Append($char)
                break
            }
        }
    }

    return $builder.ToString()
}

function ConvertTo-TaskPrettyJson($Value) {
    $compactJson = $Value | ConvertTo-Json -Depth 100 -Compress
    return Format-JsonString $compactJson
}

function Write-JsonFileNoBom([string]$Path, $Value) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $json = ConvertTo-TaskPrettyJson $Value
    [System.IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, $utf8NoBom)
}

function Convert-ToArrayForIteration($Value) {
    if ($null -eq $Value) {
        return @()
    }
    if (($Value -is [System.Array]) -or ($Value -is [System.Collections.IList] -and -not ($Value -is [string]))) {
        $items = @()
        foreach ($item in $Value) {
            $items += ,$item
        }
        return $items
    }
    return @($Value)
}

function Get-TaskTypeName([string]$FullTypeName) {
    return Get-ShortTypeName $FullTypeName
}

function Test-IsFullTypeName([string]$TypeName) {
    return (-not [string]::IsNullOrWhiteSpace($TypeName)) -and $TypeName.Contains('.')
}

function New-TaskExportTypeInfo([string]$TypeName, $GenericType1 = 'null', $GenericType2 = 'null') {
    return [ordered]@{
        'Default.TypeInfo' = New-FullTypeInfo 'BbxCommon.Internal.TaskExportTypeInfo'
        TypeName = $TypeName
        GenericType1 = $GenericType1
        GenericType2 = $GenericType2
    }
}

function Infer-EditorTypeInfo($Value, [string]$Source) {
    if ($Source -ne 'Value') {
        return New-TaskExportTypeInfo 'string'
    }
    if ($Value -is [bool]) {
        return New-TaskExportTypeInfo 'bool'
    }
    if ($Value -is [int] -or $Value -is [long]) {
        return New-TaskExportTypeInfo 'int'
    }
    if ($Value -is [float] -or $Value -is [double] -or $Value -is [decimal]) {
        return New-TaskExportTypeInfo 'float'
    }
    if (($Value -is [System.Array]) -or ($Value -is [System.Collections.IList] -and -not ($Value -is [string]))) {
        return New-TaskExportTypeInfo 'List' (New-TaskExportTypeInfo 'string')
    }
    return New-TaskExportTypeInfo 'string'
}

function New-TaskEditField([string]$FieldName, $TypeInfo, [string]$Source, [string]$Value) {
    $field = New-TypedObject 'BbxCommon.TaskEditField'
    $field.FieldName = $FieldName
    $field.TypeInfo = $TypeInfo
    $field.ValueSource = New-EnumValueSource $Source
    $field.Value = $Value
    return $field
}

function New-TaskTimelineItemInfo([double]$StartTime, [double]$Duration, [int]$Id) {
    $item = New-TypedObject 'BbxCommon.TaskTimelineItemInfo'
    $item.StartTime = $StartTime
    $item.Duration = $Duration
    $item.Id = $Id
    return $item
}

function New-Vector2([double]$X, [double]$Y) {
    return [ordered]@{
        'Default.TypeInfo' = New-FullTypeInfo 'Godot.Vector2'
        X = $X
        Y = $Y
    }
}

function Get-LayoutConditions($Node) {
    $children = @()
    $conditions = Get-RequiredObjectPropertyValue $Node 'Conditions' ("Node " + [string](Get-RequiredObjectPropertyValue $Node 'Name' 'Node'))
    foreach ($conditionKey in @('Enter', 'During', 'Exit')) {
        foreach ($conditionName in (Convert-ToArrayForIteration (Get-RequiredObjectPropertyValue $conditions $conditionKey 'Conditions'))) {
            $children += [pscustomobject]@{
                Name = [string]$conditionName
                ColumnOffset = 1.0
            }
        }
    }

    return $children
}

function Get-LayoutTaskChildren($Node) {
    $children = @()
    $columnOffset = 1.5
    if (@(Get-LayoutConditions $Node).Count -gt 0) {
        $columnOffset = 2.0
    }
    $connectPoints = Get-RequiredObjectPropertyValue $Node 'ConnectPoints' ("Node " + [string](Get-RequiredObjectPropertyValue $Node 'Name' 'Node'))
    foreach ($connectName in (Get-ObjectPropertyNames $connectPoints)) {
        foreach ($childName in (Convert-ToArrayForIteration (Get-RequiredObjectPropertyValue $connectPoints $connectName 'ConnectPoints'))) {
            $children += [pscustomobject]@{
                Name = [string]$childName
                ColumnOffset = $columnOffset
            }
        }
    }
    return $children
}

function Get-LayoutChildren($Node) {
    $children = @()
    $children += @(Get-LayoutConditions $Node)
    $children += @(Get-LayoutTaskChildren $Node)
    return $children
}

function Get-LeafSlotCount([string]$NodeName, $nodeByName, $leafCache, $activeStack) {
    if ($leafCache.ContainsKey($NodeName)) {
        return [int]$leafCache[$NodeName]
    }
    if ($activeStack.ContainsKey($NodeName)) {
        return 1
    }
    if (-not $nodeByName.ContainsKey($NodeName)) {
        return 1
    }

    $activeStack[$NodeName] = $true
    $children = Get-LayoutTaskChildren $nodeByName[$NodeName]
    $sum = 0
    foreach ($child in $children) {
        if ($nodeByName.ContainsKey($child.Name)) {
            $sum += Get-LeafSlotCount $child.Name $nodeByName $leafCache $activeStack
        }
    }
    $activeStack.Remove($NodeName)

    if ($sum -le 0) {
        $sum = 1
    }
    $leafCache[$NodeName] = $sum
    return $sum
}

function Set-LayoutSubtreePositions([string]$NodeName, [int]$StartSlot, $nodeByName, $leafCache, $columnByName, $positionSlots, $placed) {
    if (-not $nodeByName.ContainsKey($NodeName)) {
        return
    }
    $slotCount = Get-LeafSlotCount $NodeName $nodeByName $leafCache @{}
    $centerSlot = $StartSlot + (($slotCount - 1) / 2.0)
    $positionSlots[$NodeName] = $centerSlot

    if ($placed.ContainsKey($NodeName)) {
        return
    }
    $placed[$NodeName] = $true

    $childStart = $StartSlot
    foreach ($child in (Get-LayoutTaskChildren $nodeByName[$NodeName])) {
        if (-not $nodeByName.ContainsKey($child.Name)) {
            continue
        }
        $childSlots = Get-LeafSlotCount $child.Name $nodeByName $leafCache @{}
        Set-LayoutSubtreePositions $child.Name $childStart $nodeByName $leafCache $columnByName $positionSlots $placed
        $childStart += $childSlots
    }
}

function Test-LayoutSlotAvailable([double]$Column, [double]$Slot, $columnByName, $positionSlots, [double]$minimumSlotDistance) {
    foreach ($placedName in $positionSlots.Keys) {
        if (-not $columnByName.ContainsKey($placedName) -or [double]$columnByName[$placedName] -ne $Column) {
            continue
        }
        if ([Math]::Abs([double]$positionSlots[$placedName] - $Slot) -lt $minimumSlotDistance) {
            return $false
        }
    }
    return $true
}

function Find-NearestLayoutSlot([double]$Column, [double]$DesiredSlot, $columnByName, $positionSlots, [double]$minimumSlotDistance) {
    if (Test-LayoutSlotAvailable $Column $DesiredSlot $columnByName $positionSlots $minimumSlotDistance) {
        return $DesiredSlot
    }
    for ($step = 1; $step -le 1000; $step++) {
        $offset = $step * $minimumSlotDistance
        $above = $DesiredSlot - $offset
        if (Test-LayoutSlotAvailable $Column $above $columnByName $positionSlots $minimumSlotDistance) {
            return $above
        }
        $below = $DesiredSlot + $offset
        if (Test-LayoutSlotAvailable $Column $below $columnByName $positionSlots $minimumSlotDistance) {
            return $below
        }
    }
    throw "Could not find an available layout slot in column $Column."
}

function Set-LayoutConditionPositions($orderedNames, $nodeByName, $columnByName, $positionSlots, [double]$rowSpacing) {
    $minimumSlotDistance = 130.0 / $rowSpacing
    $madeProgress = $true
    while ($madeProgress) {
        $madeProgress = $false
        foreach ($parentName in $orderedNames) {
            if (-not $positionSlots.ContainsKey($parentName) -or -not $nodeByName.ContainsKey($parentName)) {
                continue
            }
            $conditions = @((Get-LayoutConditions $nodeByName[$parentName]) | Where-Object { $nodeByName.ContainsKey($_.Name) })
            for ($index = 0; $index -lt $conditions.Count; $index++) {
                $conditionName = [string]$conditions[$index].Name
                if ($positionSlots.ContainsKey($conditionName) -or -not $columnByName.ContainsKey($conditionName)) {
                    continue
                }
                $centeredOffset = ($index - (($conditions.Count - 1) / 2.0)) * $minimumSlotDistance
                $desiredSlot = [double]$positionSlots[$parentName] + $centeredOffset
                $conditionColumn = [double]$columnByName[$conditionName]
                $positionSlots[$conditionName] = Find-NearestLayoutSlot $conditionColumn $desiredSlot $columnByName $positionSlots $minimumSlotDistance
                $madeProgress = $true
            }
        }
    }
}

function New-BehaviorTreeLayoutPositions($Nodes, [string]$RootName) {
    $left = 120.0
    $top = 160.0
    $columnSpacing = 250.0
    $rowSpacing = 150.0

    $nodeByName = @{}
    $orderedNames = @()
    foreach ($node in $Nodes) {
        $name = [string](Get-RequiredObjectPropertyValue $node 'Name' 'Node')
        $nodeByName[$name] = $node
        $orderedNames += $name
    }

    $columnByName = @{}
    $queue = New-Object System.Collections.Queue
    $columnByName[$RootName] = 0
    $queue.Enqueue($RootName)
    while ($queue.Count -gt 0) {
        $current = [string]$queue.Dequeue()
        if (-not $nodeByName.ContainsKey($current)) {
            continue
        }
        $currentColumn = [double]$columnByName[$current]
        foreach ($child in (Get-LayoutChildren $nodeByName[$current])) {
            if (-not $nodeByName.ContainsKey($child.Name)) {
                continue
            }
            $candidateColumn = $currentColumn + [double]$child.ColumnOffset
            if (-not $columnByName.ContainsKey($child.Name) -or $candidateColumn -lt [double]$columnByName[$child.Name]) {
                $columnByName[$child.Name] = $candidateColumn
                $queue.Enqueue($child.Name)
            }
        }
    }

    $maxColumn = 0.0
    foreach ($name in $columnByName.Keys) {
        $maxColumn = [Math]::Max($maxColumn, [double]$columnByName[$name])
    }
    foreach ($name in $orderedNames) {
        if (-not $columnByName.ContainsKey($name)) {
            $maxColumn += 1.0
            $columnByName[$name] = $maxColumn
        }
    }

    $leafCache = @{}
    $slotByName = @{}
    Set-LayoutSubtreePositions $RootName 0 $nodeByName $leafCache $columnByName $slotByName @{}
    Set-LayoutConditionPositions $orderedNames $nodeByName $columnByName $slotByName $rowSpacing

    $nextLooseSlot = 0
    foreach ($slot in $slotByName.Values) {
        $nextLooseSlot = [Math]::Max($nextLooseSlot, [int][Math]::Ceiling([double]$slot + 1.0))
    }
    foreach ($name in $orderedNames) {
        if (-not $slotByName.ContainsKey($name)) {
            $slotByName[$name] = $nextLooseSlot
            $nextLooseSlot += 1
        }
    }

    $positions = @{}
    foreach ($name in $orderedNames) {
        $positions[$name] = [pscustomobject]@{
            X = $left + ([double]$columnByName[$name] * $columnSpacing)
            Y = $top + ([double]$slotByName[$name] * $rowSpacing)
        }
    }
    return $positions
}

function New-GraphNodeLineEditData([string]$FromTask, [int]$FromPort, [string]$ToTask, [string]$FieldName, [int]$Index) {
    $line = New-TypedObject 'BbxCommon.GraphNodeLineEditData'
    $line.FromTask = $FromTask
    $line.FromPort = $FromPort
    $line.ToTask = $ToTask
    $line.FieldName = $FieldName
    $line.Index = $Index
    $line.TaskType = 'null'
    $line.Fields = New-JsonApiList @() (New-FullTypeInfo 'BbxCommon.TaskEditField')
    return $line
}

function New-GraphNodeEditData([string]$Name, [string]$FullTypeName, [int]$Index, $Fields, $ConnectPoints, $LayoutPositions) {
    $node = New-TypedObject 'BbxCommon.GraphNodeEditData'
    $node.Name = $Name
    if ($null -ne $LayoutPositions -and $LayoutPositions.ContainsKey($Name)) {
        $pos = $LayoutPositions[$Name]
        $node.Pos = New-Vector2 ([double]$pos.X) ([double]$pos.Y)
    }
    else {
        $node.Pos = New-Vector2 (120.0 + (($Index % 4) * 320.0)) (160.0 + ([Math]::Floor($Index / 4) * 240.0))
    }

    $portEntries = @(
        [pscustomobject]@{ Key = 0; Value = 'EnterCondition' },
        [pscustomobject]@{ Key = 1; Value = 'Condition' },
        [pscustomobject]@{ Key = 2; Value = 'ExitCondition' }
    )
    $portIndex = 3
    foreach ($connectName in (Get-ObjectPropertyNames $ConnectPoints)) {
        $portEntries += [pscustomobject]@{ Key = $portIndex; Value = $connectName }
        $portIndex++
    }
    $node.m_PortIndexToFieldName = New-JsonApiDictionary $portEntries (New-SpecialTypeInfo 'int') (New-SpecialTypeInfo 'string')
    $node.TaskType = Get-TaskTypeName $FullTypeName

    $editFields = @()
    foreach ($fieldName in (Get-ObjectPropertyNames $Fields)) {
        $field = Get-RequiredObjectPropertyValue $Fields $fieldName 'Fields'
        $source = [string](Get-RequiredObjectPropertyValue $field 'Source' "Field $fieldName")
        Assert-ValueSourceName $source "Field $fieldName"
        $value = Get-RequiredObjectPropertyValue $field 'Value' "Field $fieldName"
        $editFields += New-TaskEditField $fieldName (Infer-EditorTypeInfo $value $source) $source (Convert-SettingValueToTaskString -Value $value)
    }
    foreach ($connectName in (Get-ObjectPropertyNames $ConnectPoints)) {
        $editFields += New-TaskEditField $connectName (New-TaskExportTypeInfo 'TaskConnectPoint.Multiple') 'Value' ''
    }
    $node.Fields = New-JsonApiList $editFields (New-FullTypeInfo 'BbxCommon.TaskEditField')
    return $node
}

function Test-IsTimelineTaskType([string]$FullTypeName) {
    return [string]$FullTypeName -eq 'BbxCommon.TaskTimeline'
}

function New-TimelineItemEditData([double]$StartTime, [double]$Duration, [string]$FullTypeName, $Fields) {
    $item = New-TypedObject 'BbxCommon.TimelineItemEditData'
    $item.OnStartTimeChanged = 'null'
    $item.OnDurationChanged = 'null'
    $item.m_StartTime = $StartTime
    $item.m_Duration = $Duration
    $item.ExpandCondition = $false
    $item.EnterConditions = New-JsonApiList @() (New-FullTypeInfo 'BbxCommon.TaskEditData')
    $item.Conditions = New-JsonApiList @() (New-FullTypeInfo 'BbxCommon.TaskEditData')
    $item.ExitConditions = New-JsonApiList @() (New-FullTypeInfo 'BbxCommon.TaskEditData')
    $item.TaskType = Get-TaskTypeName $FullTypeName

    $editFields = @()
    foreach ($fieldName in (Get-ObjectPropertyNames $Fields)) {
        $field = Get-RequiredObjectPropertyValue $Fields $fieldName 'Fields'
        $source = [string](Get-RequiredObjectPropertyValue $field 'Source' "Field $fieldName")
        Assert-ValueSourceName $source "Field $fieldName"
        $value = Get-RequiredObjectPropertyValue $field 'Value' "Field $fieldName"
        $editFields += New-TaskEditField $fieldName (Infer-EditorTypeInfo $value $source) $source (Convert-SettingValueToTaskString -Value $value)
    }
    $item.Fields = New-JsonApiList $editFields (New-FullTypeInfo 'BbxCommon.TaskEditField')
    return $item
}

function New-TimelineEditorData([string]$FolderPath, [string]$Key, $RootNode, $nodeByName) {
    $timelineItems = Convert-ToArrayForIteration (Get-RequiredObjectPropertyValue $RootNode 'TimelineItems' 'Root TimelineItems')
    $taskDatas = @()
    $maxTime = 0.0
    $hasNegativeDuration = $false

    foreach ($timelineItem in $timelineItems) {
        $startTime = [double](Get-RequiredObjectPropertyValue $timelineItem 'StartTime' 'TimelineItem')
        $duration = [double](Get-RequiredObjectPropertyValue $timelineItem 'Duration' 'TimelineItem')
        $nodeName = [string](Get-RequiredObjectPropertyValue $timelineItem 'Node' 'TimelineItem')
        if (-not $nodeByName.ContainsKey($nodeName)) {
            throw "TimelineItem references missing node '$nodeName'."
        }
        $node = $nodeByName[$nodeName]
        $nodeType = [string](Get-RequiredObjectPropertyValue $node 'Type' "Node $nodeName")
        $fields = Get-RequiredObjectPropertyValue $node 'Fields' "Node $nodeName"
        $taskDatas += New-TimelineItemEditData $startTime $duration $nodeType $fields

        if ($duration -lt 0) {
            $hasNegativeDuration = $true
            $maxTime = [Math]::Max($maxTime, $startTime)
        }
        else {
            $maxTime = [Math]::Max($maxTime, $startTime + $duration)
        }
    }

    $editorData = New-TypedObject 'BbxCommon.EditorModel+TimelineSaveTargetData'
    $editorData.TaskDatas = New-JsonApiList $taskDatas (New-FullTypeInfo 'BbxCommon.TimelineItemEditData')
    $editorData.m_MaxTime = $maxTime
    $editorData.m_HasNagetiveDuration = $hasNegativeDuration
    $editorData.m_FilePath = Join-Path $FolderPath $Key
    return $editorData
}

function Add-LineEntry([System.Collections.Generic.List[object]]$Entries, [string]$Key, $Line) {
    for ($i = 0; $i -lt $Entries.Count; $i++) {
        if ($Entries[$i].Key -eq $Key) {
            $current = @($Entries[$i].Value)
            $current += $Line
            $Entries[$i] = [pscustomobject]@{ Key = $Key; Value = $current }
            return
        }
    }
    $Entries.Add([pscustomobject]@{ Key = $Key; Value = @($Line) })
}

function Convert-TaskSettingToJson([string]$FolderPath, [string]$Key) {
    $inputPath = Join-Path $FolderPath ($Key + '.task.setting')
    $jsonOutputPath = Join-Path $FolderPath ($Key + '.json')
    $editorOutputPath = Join-Path $FolderPath ($Key + '.editor.json')

    if (-not [System.IO.File]::Exists($inputPath)) {
        throw "Input setting not found: $inputPath"
    }

    $setting = Read-JsonFilePreserveArrays $inputPath
    $taskType = [string](Get-RequiredObjectPropertyValue $setting 'TaskType' 'Setting')
    if (@('BehaviorTree', 'Timeline') -notcontains $taskType) {
        throw "Direction 0 supports only BehaviorTree or Timeline .task.setting files, but got '$taskType'."
    }

    $nodes = Convert-ToArrayForIteration (Get-RequiredObjectPropertyValue $setting 'Nodes' 'Setting')
    if ($nodes.Count -eq 0) {
        throw 'Nodes is empty.'
    }

    $nameToId = @{}
    $nodeByName = @{}
    for ($i = 0; $i -lt $nodes.Count; $i++) {
        $node = $nodes[$i]
        $name = [string](Get-RequiredObjectPropertyValue $node 'Name' "Nodes[$i]")
        if ($nameToId.ContainsKey($name)) {
            throw "Duplicate node name: $name"
        }
        $nameToId[$name] = $i
        $nodeByName[$name] = $node
    }

    $rootName = [string](Get-RequiredObjectPropertyValue $setting 'Root' 'Setting')
    if (-not $nameToId.ContainsKey($rootName)) {
        throw "Root node '$rootName' does not exist in Nodes."
    }

    $rootNode = $nodeByName[$rootName]
    $rootType = [string](Get-RequiredObjectPropertyValue $rootNode 'Type' "Node $rootName")
    if ($taskType -eq 'Timeline' -and -not (Test-IsTimelineTaskType $rootType)) {
        throw "Timeline Root '$rootName' must be BbxCommon.TaskTimeline, but got '$rootType'."
    }

    $layoutPositions = $null
    if ($taskType -eq 'BehaviorTree') {
        $layoutPositions = New-BehaviorTreeLayoutPositions $nodes $rootName
    }

    $taskInfoEntries = @()
    $nodeEditEntries = @()
    $lineEntries = New-Object System.Collections.Generic.List[object]

    for ($i = 0; $i -lt $nodes.Count; $i++) {
        $node = $nodes[$i]
        $nodeName = [string](Get-RequiredObjectPropertyValue $node 'Name' "Nodes[$i]")
        $nodeType = [string](Get-RequiredObjectPropertyValue $node 'Type' "Node $nodeName")
        if (-not (Test-IsFullTypeName $nodeType)) {
            throw "Node $nodeName Type must be a full type name, but got '$nodeType'."
        }
        $fields = Get-RequiredObjectPropertyValue $node 'Fields' "Node $nodeName"
        if ($taskType -eq 'BehaviorTree') {
            $connectPoints = Get-RequiredObjectPropertyValue $node 'ConnectPoints' "Node $nodeName"
        }
        else {
            $connectPoints = Get-ObjectPropertyValue $node 'ConnectPoints' ([ordered]@{})
            if ((Get-ObjectPropertyNames $connectPoints).Count -gt 0) {
                throw "Timeline node $nodeName must not contain ConnectPoints."
            }
        }
        $conditions = Get-RequiredObjectPropertyValue $node 'Conditions' "Node $nodeName"
        $isTimelineNode = Test-IsTimelineTaskType $nodeType

        foreach ($conditionKey in @('Enter', 'During', 'Exit')) {
            $null = Get-RequiredObjectPropertyValue $conditions $conditionKey "Node $nodeName Conditions"
        }

        $timelineInfos = @()
        if ($isTimelineNode) {
            $timelineItems = Convert-ToArrayForIteration (Get-RequiredObjectPropertyValue $node 'TimelineItems' "Node $nodeName")
            foreach ($timelineItem in $timelineItems) {
                $startTime = [double](Get-RequiredObjectPropertyValue $timelineItem 'StartTime' "Node $nodeName TimelineItems")
                $duration = [double](Get-RequiredObjectPropertyValue $timelineItem 'Duration' "Node $nodeName TimelineItems")
                $targetName = [string](Get-RequiredObjectPropertyValue $timelineItem 'Node' "Node $nodeName TimelineItems")
                if (-not $nameToId.ContainsKey($targetName)) {
                    throw "TimelineItem on $nodeName references missing node '$targetName'."
                }
                $timelineInfos += New-TaskTimelineItemInfo $startTime $duration $nameToId[$targetName]
            }
        }
        else {
            $unexpectedTimelineItems = Get-ObjectPropertyValue $node 'TimelineItems' $null
            if ($null -ne $unexpectedTimelineItems) {
                throw "Node $nodeName is not BbxCommon.TaskTimeline, so it must not contain TimelineItems."
            }
        }

        $taskFields = @()
        foreach ($fieldName in (Get-ObjectPropertyNames $fields)) {
            $field = Get-RequiredObjectPropertyValue $fields $fieldName "Node $nodeName Fields"
            $source = [string](Get-RequiredObjectPropertyValue $field 'Source' "Field $nodeName.$fieldName")
            $value = Get-RequiredObjectPropertyValue $field 'Value' "Field $nodeName.$fieldName"
            $taskFields += New-TaskFieldInfo $fieldName $source (Convert-SettingValueToTaskString -Value $value)
        }

        foreach ($connectName in (Get-ObjectPropertyNames $connectPoints)) {
            $targetIds = @()
            $targets = Convert-ToArrayForIteration (Get-RequiredObjectPropertyValue $connectPoints $connectName "Node $nodeName ConnectPoints")
            $index = 0
            foreach ($targetName in $targets) {
                $targetNameText = [string]$targetName
                if (-not $nameToId.ContainsKey($targetNameText)) {
                    throw "ConnectPoint $nodeName.$connectName references missing node '$targetNameText'."
                }
                $targetIds += $nameToId[$targetNameText]
                Add-LineEntry $lineEntries "$nodeName.$connectName" (New-GraphNodeLineEditData $nodeName 3 $targetNameText $connectName $index)
                $index++
            }
            $connectValue = (($targetIds | ForEach-Object { [string]$_ + '%||%' }) -join '')
            $taskFields += New-TaskFieldInfo $connectName 'Value' $connectValue
        }

        $enterRefs = @()
        foreach ($conditionName in (Convert-ToArrayForIteration (Get-RequiredObjectPropertyValue $conditions 'Enter' "Node $nodeName Conditions"))) {
            $conditionNameText = [string]$conditionName
            if (-not $nameToId.ContainsKey($conditionNameText)) {
                throw "Enter condition on $nodeName references missing node '$conditionNameText'."
            }
            $enterRefs += $nameToId[$conditionNameText]
            Add-LineEntry $lineEntries "$nodeName.EnterCondition" (New-GraphNodeLineEditData $nodeName 0 $conditionNameText 'EnterCondition' ($enterRefs.Count - 1))
        }

        $duringRefs = @()
        foreach ($conditionName in (Convert-ToArrayForIteration (Get-RequiredObjectPropertyValue $conditions 'During' "Node $nodeName Conditions"))) {
            $conditionNameText = [string]$conditionName
            if (-not $nameToId.ContainsKey($conditionNameText)) {
                throw "During condition on $nodeName references missing node '$conditionNameText'."
            }
            $duringRefs += $nameToId[$conditionNameText]
            Add-LineEntry $lineEntries "$nodeName.Condition" (New-GraphNodeLineEditData $nodeName 1 $conditionNameText 'Condition' ($duringRefs.Count - 1))
        }

        $exitRefs = @()
        foreach ($conditionName in (Convert-ToArrayForIteration (Get-RequiredObjectPropertyValue $conditions 'Exit' "Node $nodeName Conditions"))) {
            $conditionNameText = [string]$conditionName
            if (-not $nameToId.ContainsKey($conditionNameText)) {
                throw "Exit condition on $nodeName references missing node '$conditionNameText'."
            }
            $exitRefs += $nameToId[$conditionNameText]
            Add-LineEntry $lineEntries "$nodeName.ExitCondition" (New-GraphNodeLineEditData $nodeName 2 $conditionNameText 'ExitCondition' ($exitRefs.Count - 1))
        }

        $taskValueInfo = New-TypedObject 'BbxCommon.TaskValueInfo'
        $taskValueInfo.FullTypeName = $nodeType
        $taskValueInfo.FieldInfos = New-JsonApiList $taskFields (New-FullTypeInfo 'BbxCommon.TaskFieldInfo')
        $taskValueInfo.EnterConditionReferences = New-JsonApiList $enterRefs (New-SpecialTypeInfo 'int')
        $taskValueInfo.ConditionReferences = New-JsonApiList $duringRefs (New-SpecialTypeInfo 'int')
        $taskValueInfo.ExitConditionReferences = New-JsonApiList $exitRefs (New-SpecialTypeInfo 'int')
        $taskValueInfo.TimelineItemInfos = New-JsonApiList $timelineInfos (New-FullTypeInfo 'BbxCommon.TaskTimelineItemInfo')

        $taskInfoEntries += [pscustomobject]@{ Key = $i; Value = $taskValueInfo }
        if ($taskType -eq 'BehaviorTree') {
            $nodeEditEntries += [pscustomobject]@{ Key = $nodeName; Value = New-GraphNodeEditData $nodeName $nodeType $i $fields $connectPoints $layoutPositions }
        }
    }

    $taskGroupInfo = New-TypedObject 'BbxCommon.TaskGroupInfo'
    $taskGroupInfo.RootTaskId = $nameToId[$rootName]
    $bindingContext = Get-ContextConfigName ([string](Get-RequiredObjectPropertyValue $setting 'BindingContext' 'Setting'))
    $taskGroupInfo.BindingContextFullType = $bindingContext
    $taskGroupInfo.TaskInfos = New-JsonApiDictionary $taskInfoEntries (New-SpecialTypeInfo 'int') (New-FullTypeInfo 'BbxCommon.TaskValueInfo')

    if ($taskType -eq 'BehaviorTree') {
        $editorLineEntries = @()
        foreach ($entry in $lineEntries) {
            $editorLineEntries += [pscustomobject]@{
                Key = $entry.Key
                Value = New-JsonApiList $entry.Value (New-FullTypeInfo 'BbxCommon.GraphNodeLineEditData')
            }
        }

        $editorData = New-TypedObject 'BbxCommon.EditorModel+NodeGraphSaveTargetData'
        $editorData.NodeLineEditDataList = New-JsonApiDictionary $editorLineEntries (New-SpecialTypeInfo 'string') (New-ListTypeInfo (New-FullTypeInfo 'BbxCommon.GraphNodeLineEditData'))
        $editorData.NodeEditDataDictionary = New-JsonApiDictionary $nodeEditEntries (New-SpecialTypeInfo 'string') (New-FullTypeInfo 'BbxCommon.GraphNodeEditData')
        $editorData.m_FilePath = Join-Path $FolderPath $Key
        $editorData.m_BindingContextType = $bindingContext
    }
    else {
        $editorData = New-TimelineEditorData $FolderPath $Key $rootNode $nodeByName
        $editorData.m_BindingContextType = $bindingContext
    }

    Write-JsonFileNoBom $jsonOutputPath $taskGroupInfo
    Write-JsonFileNoBom $editorOutputPath $editorData
    Write-Host "Generated $jsonOutputPath"
    Write-Host "Generated $editorOutputPath"
}

function Convert-TaskJsonToSetting([string]$FolderPath, [string]$Key) {
    $inputPath = Join-Path $FolderPath ($Key + '.json')
    $outputPath = Join-Path $FolderPath ($Key + '.task.setting')

    if (-not [System.IO.File]::Exists($inputPath)) {
        throw "Input json not found: $inputPath"
    }

    $config = Get-Content -LiteralPath $inputPath -Encoding UTF8 -Raw | ConvertFrom-Json
    if ($null -eq $config.TaskInfos) {
        throw 'Input json does not contain TaskInfos.'
    }

    $taskEntries = Get-JsonApiDictionaryEntries $config.TaskInfos
    if ($taskEntries.Count -eq 0) {
        throw 'TaskInfos is empty.'
    }

    $idToTaskInfo = @{}
    $idToName = @{}
    $usedNames = @{}
    $hasTimeline = $false

    foreach ($entry in $taskEntries) {
        $id = [int]$entry.Key
        $taskInfo = $entry.Value
        $idToTaskInfo[$id] = $taskInfo

        $shortName = Convert-NameToSafeIdentifier (Get-ShortTypeName ([string]$taskInfo.FullTypeName))
        $baseName = "${shortName}_${id}"
        $name = $baseName
        $suffix = 1
        while ($usedNames.ContainsKey($name)) {
            $name = "${baseName}_${suffix}"
            $suffix++
        }
        $usedNames[$name] = $true
        $idToName[$id] = $name

        if ([string]$taskInfo.FullTypeName -eq 'BbxCommon.TaskTimeline') {
            $hasTimeline = $true
        }
        if ((Get-JsonApiListValues $taskInfo.TimelineItemInfos).Count -gt 0) {
            $hasTimeline = $true
        }
    }

    $taskType = if ($hasTimeline) { 'Timeline' } else { 'BehaviorTree' }

    $nodes = @()
    $knownIds = @{}
    foreach ($id in $idToTaskInfo.Keys) {
        $knownIds[[int]$id] = $true
    }

    foreach ($entry in ($taskEntries | Sort-Object { [int]$_.Key })) {
        $id = [int]$entry.Key
        $taskInfo = $entry.Value

        $fields = [ordered]@{}
        $connectPoints = [ordered]@{}

        foreach ($fieldInfo in (Get-JsonApiListValues $taskInfo.FieldInfos)) {
            $fieldName = [string]$fieldInfo.FieldName
            $source = Get-ValueSourceName $fieldInfo.ValueSource
            $rawValue = [string]$fieldInfo.Value

            $isConnectPoint = $false
            if ($source -eq 'Value' -and $fieldName -eq 'Tasks' -and $rawValue.Contains('%||%')) {
                $targetNames = @()
                $allTargetsExist = $true
                foreach ($part in ($rawValue -split [regex]::Escape('%||%'))) {
                    if ($part -eq '') {
                        continue
                    }
                    $targetId = 0
                    if (-not [int]::TryParse($part, [ref]$targetId) -or -not $knownIds.ContainsKey($targetId)) {
                        $allTargetsExist = $false
                        break
                    }
                    $targetNames += $idToName[$targetId]
                }
                if ($allTargetsExist) {
                    $connectPoints[$fieldName] = $targetNames
                    $isConnectPoint = $true
                }
            }

            if (-not $isConnectPoint) {
                $fields[$fieldName] = [ordered]@{
                    Source = $source
                    Value = Convert-FieldValue $source $rawValue
                }
            }
        }

        $conditions = [ordered]@{
            Enter = @((Get-JsonApiListValues $taskInfo.EnterConditionReferences) | ForEach-Object { $idToName[[int]$_] })
            During = @((Get-JsonApiListValues $taskInfo.ConditionReferences) | ForEach-Object { $idToName[[int]$_] })
            Exit = @((Get-JsonApiListValues $taskInfo.ExitConditionReferences) | ForEach-Object { $idToName[[int]$_] })
        }

        $node = [ordered]@{
            Name = $idToName[$id]
            Type = [string]$taskInfo.FullTypeName
            Fields = $fields
        }

        if ([string]$taskInfo.FullTypeName -eq 'BbxCommon.TaskTimeline') {
            $timelineItems = @()
            foreach ($timelineInfo in (Get-JsonApiListValues $taskInfo.TimelineItemInfos)) {
                $timelineTargetId = [int]$timelineInfo.Id
                if (-not $idToName.ContainsKey($timelineTargetId)) {
                    throw "TimelineItem on task $id references missing task id $timelineTargetId."
                }
                $timelineItems += [ordered]@{
                    StartTime = [double]$timelineInfo.StartTime
                    Duration = [double]$timelineInfo.Duration
                    Node = $idToName[$timelineTargetId]
                }
            }
            $node.TimelineItems = $timelineItems
        }

        if ($taskType -eq 'BehaviorTree') {
            $node.ConnectPoints = $connectPoints
        }
        $node.Conditions = $conditions
        $nodes += $node
    }

    $rootId = [int]$config.RootTaskId
    if (-not $idToName.ContainsKey($rootId)) {
        throw "RootTaskId $rootId does not exist in TaskInfos."
    }

    $setting = [ordered]@{
        TaskType = $taskType
        BindingContext = Get-ContextConfigName ([string]$config.BindingContextFullType)
        Root = $idToName[$rootId]
        Nodes = $nodes
    }

    Write-JsonFileNoBom $outputPath $setting
    Write-Host "Generated $outputPath"
}

try {
    if ($PSBoundParameters.Count -lt 3 -or [string]::IsNullOrWhiteSpace($Direction) -or [string]::IsNullOrWhiteSpace($Folder) -or [string]::IsNullOrWhiteSpace($TaskKey)) {
        Show-Usage
        exit 1
    }

    $folderPath = Resolve-TaskFolder $Folder
    if (-not [System.IO.Directory]::Exists($folderPath)) {
        throw "Folder not found: $folderPath"
    }

    switch ($Direction) {
        '0' {
            Convert-TaskSettingToJson $folderPath $TaskKey
            exit 0
        }
        '1' {
            Convert-TaskJsonToSetting $folderPath $TaskKey
            exit 0
        }
        default {
            Show-Usage
            throw "Unknown direction: $Direction"
        }
    }
}
catch {
    [Console]::Error.WriteLine('Error: ' + $_.Exception.Message)
    exit 1
}
