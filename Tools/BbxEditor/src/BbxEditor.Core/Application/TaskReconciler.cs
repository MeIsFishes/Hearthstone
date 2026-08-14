using BbxEditor.Contracts;
using BbxEditor.Diagnostics;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;

namespace BbxEditor.Application;

public static class TaskReconciler
{
    public static IReadOnlyList<Diagnostic> Reconcile(TaskDocument document, TaskCatalog catalog)
    {
        var diagnostics = new List<Diagnostic>();
        var context = catalog.FindContext(document.BindingContextType);
        if (context is not null && context.TypeName.Contains('.') && !document.BindingContextType.Contains('.'))
        {
            document.BindingContextType = context.TypeName;
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Info, "CONTEXT_UPGRADED_TO_FULL_NAME", $"The context was upgraded to its fully qualified name: {context.TypeName}"));
        }
        foreach (var task in EnumerateTasks(document))
        {
            var definition = catalog.FindTask(task.TaskType);
            if (definition is null)
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "TASK_TYPE_MISSING", $"Task type not found: {task.TaskType}"));
                continue;
            }

            var currentFields = task.Fields.ToDictionary(item => item.FieldName, StringComparer.Ordinal);
            task.Fields.Clear();
            foreach (var definitionField in definition.Fields)
            {
                if (currentFields.TryGetValue(definitionField.Name, out var current))
                {
                    current.Type = definitionField.Type;
                    current.Comment = definitionField.Comment;
                    if (current.Source == FieldValueSource.Value && current.Type.IsDictionary &&
                        current.Type.GenericType1 is not null && current.Type.GenericType2 is not null &&
                        LegacyCollectionValueCodec.TryDecodeAlternatingDictionary(current.Value, out var legacyValues))
                    {
                        current.Value = LegacyCollectionValueCodec.EncodeDictionary(
                            legacyValues,
                            current.Type.GenericType1,
                            current.Type.GenericType2);
                        diagnostics.Add(new Diagnostic(DiagnosticSeverity.Info, "DICTIONARY_VALUE_UPGRADED",
                            $"{task.TaskType}.{current.FieldName} was upgraded to the CrossLibrary JsonApi dictionary format."));
                    }
                    task.Fields.Add(current);
                }
                else
                {
                    task.Fields.Add(new TaskFieldValue
                    {
                        FieldName = definitionField.Name,
                        Type = definitionField.Type,
                        Comment = definitionField.Comment,
                        Source = FieldValueSource.Value,
                        Value = string.Empty,
                    });
                    diagnostics.Add(new Diagnostic(DiagnosticSeverity.Info, "FIELD_ADDED", $"A field was added to {task.TaskType}: {definitionField.Name}"));
                }
            }

            foreach (var removed in currentFields.Keys.Except(definition.Fields.Select(item => item.Name), StringComparer.Ordinal))
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, "FIELD_REMOVED", $"An obsolete field was removed from {task.TaskType}: {removed}"));
            }
        }
        document.IsDirty = false;
        return diagnostics;
    }

    public static IEnumerable<TaskInstance> EnumerateTasks(TaskDocument document)
    {
        if (document is TimelineDocument timeline)
        {
            foreach (var item in timeline.Items)
            {
                yield return item.Task;
                foreach (var condition in item.EnterConditions.Concat(item.Conditions).Concat(item.ExitConditions))
                {
                    yield return condition;
                }
            }
        }
        else if (document is BehaviorTreeDocument tree)
        {
            foreach (var node in tree.Nodes)
            {
                yield return node.Task;
            }
        }
    }
}
