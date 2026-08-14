using BbxEditor.Contracts;
using BbxEditor.Diagnostics;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;

namespace BbxEditor.Application;

public static class DocumentValidator
{
    public static IReadOnlyList<Diagnostic> Validate(TaskDocument document, TaskCatalog catalog)
    {
        var diagnostics = new List<Diagnostic>();
        if (string.IsNullOrWhiteSpace(document.BindingContextType))
        {
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CONTEXT_REQUIRED", "A TaskContext must be selected for the document."));
        }
        else if (catalog.FindContext(document.BindingContextType) is null)
        {
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CONTEXT_UNKNOWN", $"TaskContext not found: {document.BindingContextType}"));
        }

        foreach (var task in TaskReconciler.EnumerateTasks(document))
        {
            if (catalog.FindTask(task.TaskType) is null)
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TASK_UNKNOWN", $"Task type not found: {task.TaskType}"));
            }
            ValidateFields(task, catalog, diagnostics);
        }

        if (document is BehaviorTreeDocument tree)
        {
            ValidateTree(tree, catalog, diagnostics);
        }
        return diagnostics;
    }

    private static void ValidateFields(TaskInstance task, TaskCatalog catalog, ICollection<Diagnostic> diagnostics)
    {
        foreach (var field in task.Fields.Where(field => field.Source == FieldValueSource.Value && !field.Type.IsConnectPoint))
        {
            if (!TaskValueTypeSupport.IsSupportedConstant(field.Type, catalog) && !string.IsNullOrEmpty(field.Value))
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "FIELD_COMPLEX_VALUE_UNSUPPORTED",
                    $"{task.TaskType}.{field.FieldName} does not support class objects or complex collection constants: {field.Type}."));
            }
            if ((field.Type.IsList || field.Type.IsDictionary) && !TaskValueTypeSupport.IsSupportedConstant(field.Type, catalog))
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "FIELD_COLLECTION_TYPE_UNSUPPORTED",
                    $"{task.TaskType}.{field.FieldName} only supports scalar or enum values; nested collections and class objects are not supported: {field.Type}."));
                continue;
            }
            if (!field.Type.IsList && !field.Type.IsDictionary) continue;

            if (field.Type.IsList)
            {
                foreach (var element in LegacyCollectionValueCodec.DecodeList(field.Value))
                {
                    ValidateScalar(task, field, element, field.Type.GenericType1!, catalog, diagnostics);
                }
            }
            if (field.Type.IsDictionary)
            {
                if (string.IsNullOrWhiteSpace(field.Value)) continue;
                if (!LegacyCollectionValueCodec.TryDecodeCrossLibraryDictionary(
                        field.Value,
                        field.Type.GenericType1!,
                        field.Type.GenericType2!,
                        out var pairs,
                        out var dictionaryError))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "FIELD_DICTIONARY_JSON_INVALID",
                        $"{task.TaskType}.{field.FieldName} is not a valid CrossLibrary JsonApi dictionary: {dictionaryError}"));
                    continue;
                }
                var normalizedKeys = pairs
                    .Select(pair => new
                    {
                        Pair = pair,
                        Valid = TaskValueTypeSupport.TryNormalizeDictionaryKey(
                            pair.Key, field.Type.GenericType1!, catalog, out var normalized),
                        Normalized = normalized,
                    })
                    .Where(item => item.Valid)
                    .ToArray();
                var duplicate = normalizedKeys
                    .GroupBy(item => item.Normalized, StringComparer.Ordinal)
                    .FirstOrDefault(group => group.Count() > 1);
                if (duplicate is not null)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "FIELD_DICTIONARY_DUPLICATE_KEY",
                        $"{task.TaskType}.{field.FieldName} contains a duplicate key after conversion to the declared type: {duplicate.First().Pair.Key}."));
                }
                foreach (var pair in pairs)
                {
                    ValidateDictionaryScalar(task, field, pair.Key, field.Type.GenericType1!, catalog, diagnostics, "key");
                    ValidateDictionaryScalar(task, field, pair.Value, field.Type.GenericType2!, catalog, diagnostics, "value");
                }
            }
        }
    }

    private static void ValidateDictionaryScalar(
        TaskInstance task,
        TaskFieldValue field,
        string value,
        TaskTypeReference type,
        TaskCatalog catalog,
        ICollection<Diagnostic> diagnostics,
        string role)
    {
        if (TaskValueTypeSupport.IsRepresentableDictionaryScalar(value, type, catalog)) return;
        var reason = type.TypeName == "string" && value == "null"
            ? "the legacy JsonApi interprets the literal string \"null\" as a null reference"
            : $"it is not a valid {type}";
        diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "FIELD_COLLECTION_VALUE_INVALID",
            $"The {role} \"{value}\" in {task.TaskType}.{field.FieldName} is invalid because {reason}."));
    }

    private static void ValidateScalar(
        TaskInstance task,
        TaskFieldValue field,
        string value,
        TaskTypeReference type,
        TaskCatalog catalog,
        ICollection<Diagnostic> diagnostics,
        string? role = null)
    {
        if (TaskValueTypeSupport.IsValidScalar(value, type, catalog)) return;
        diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "FIELD_COLLECTION_VALUE_INVALID",
            $"The {role ?? "element"} \"{value}\" in {task.TaskType}.{field.FieldName} is not a valid {type}."));
    }

    private static void ValidateTree(BehaviorTreeDocument tree, TaskCatalog catalog, ICollection<Diagnostic> diagnostics)
    {
        var nodeIds = tree.Nodes.Select(node => node.Id).ToHashSet();
        var roots = tree.Nodes.Where(node => node.Task.TaskType == "TaskBtRoot").ToArray();
        if (roots.Length != 1)
        {
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_ROOT_COUNT", $"A Behavior Tree must contain exactly one TaskBtRoot; current count: {roots.Length}."));
        }
        else if (tree.Edges.Any(edge => edge.TargetNodeId == roots[0].Id))
        {
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_ROOT_HAS_PARENT", "TaskBtRoot cannot have a parent connection."));
        }

        foreach (var edge in tree.Edges)
        {
            if (!nodeIds.Contains(edge.SourceNodeId) || !nodeIds.Contains(edge.TargetNodeId))
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_DANGLING_EDGE", "A connection references a deleted node."));
            }
            if (edge.SourceNodeId == edge.TargetNodeId)
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_SELF_EDGE", "A node cannot be connected to itself."));
            }
            var sourceNode = tree.Nodes.FirstOrDefault(node => node.Id == edge.SourceNodeId);
            var targetNode = tree.Nodes.FirstOrDefault(node => node.Id == edge.TargetNodeId);
            if (sourceNode is not null && targetNode is not null)
            {
                var sourceIsCondition = catalog.FindTask(sourceNode.Task.TaskType)?.HasTag(TaskContractConstants.TagCondition) == true;
                var targetIsCondition = catalog.FindTask(targetNode.Task.TaskType)?.HasTag(TaskContractConstants.TagCondition) == true;
                if (sourceIsCondition)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_CONDITION_HAS_OUTPUT", $"A Condition node cannot be used as a connection source: {sourceNode.Name}"));
                }
                var conditionPort = IsConditionPort(edge.SourcePort);
                if (conditionPort != targetIsCondition)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_PORT_TYPE_MISMATCH", $"Port {sourceNode.Name}.{edge.SourcePort} is incompatible with target {targetNode.Name}."));
                }
            }
        }

        foreach (var group in tree.Edges.GroupBy(edge => edge.TargetNodeId).Where(group => group.Count() > 1))
        {
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_MULTIPLE_PARENTS", $"Node {group.Key} has multiple parent connections."));
        }

        foreach (var group in tree.Edges.GroupBy(edge => (edge.SourceNodeId, edge.SourcePort)))
        {
            var source = tree.Nodes.FirstOrDefault(node => node.Id == group.Key.SourceNodeId);
            var field = source?.Task.Fields.FirstOrDefault(item => item.FieldName == group.Key.SourcePort);
            if (field?.Type.IsSingleConnectPoint == true && group.Count() > 1)
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_SINGLE_PORT_LIMIT", $"{source!.Name}.{group.Key.SourcePort} allows only one connection."));
            }

            if (!IsConditionPort(group.Key.SourcePort) && field?.Type.IsConnectPoint != true)
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_UNKNOWN_PORT", $"Node {source?.Name} does not have connection port: {group.Key.SourcePort}"));
            }
            if (group.GroupBy(edge => edge.Order).Any(orderGroup => orderGroup.Count() > 1))
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_DUPLICATE_ORDER", $"{source?.Name}.{group.Key.SourcePort} contains duplicate child ordering values."));
            }
        }

        if (HasCycle(tree))
        {
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TREE_CYCLE", "The Behavior Tree contains a cycle."));
        }

        if (roots.Length == 1)
        {
            var reachable = Traverse(tree, roots[0].Id);
            foreach (var node in tree.Nodes.Where(node => !reachable.Contains(node.Id)))
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, "TREE_UNREACHABLE", $"Node is unreachable from the root: {node.Name}"));
            }
        }
    }

    private static bool HasCycle(BehaviorTreeDocument tree)
    {
        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();
        bool Visit(Guid node)
        {
            if (visiting.Contains(node)) return true;
            if (!visited.Add(node)) return false;
            visiting.Add(node);
            foreach (var edge in tree.Edges.Where(edge => edge.SourceNodeId == node))
            {
                if (Visit(edge.TargetNodeId)) return true;
            }
            visiting.Remove(node);
            return false;
        }
        return tree.Nodes.Any(node => Visit(node.Id));
    }

    private static HashSet<Guid> Traverse(BehaviorTreeDocument tree, Guid root)
    {
        var result = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(root);
        while (pending.TryPop(out var current) && result.Add(current))
        {
            foreach (var edge in tree.Edges.Where(edge => edge.SourceNodeId == current))
            {
                pending.Push(edge.TargetNodeId);
            }
        }
        return result;
    }

    public static bool IsConditionPort(string port) => port is "EnterCondition" or "Condition" or "ExitCondition";
}
