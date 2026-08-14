using System.Globalization;
using BbxEditor.Contracts;
using BbxEditor.Diagnostics;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;

namespace BbxEditor.Application;

public static class RuntimeExporter
{
    public static OperationResult<RuntimeTaskGroup> Export(TaskDocument document, TaskCatalog catalog)
    {
        var result = new OperationResult<RuntimeTaskGroup>();
        result.Diagnostics.AddRange(DocumentValidator.Validate(document, catalog));
        if (result.Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error))
        {
            return result;
        }

        result.Value = document switch
        {
            TimelineDocument timeline => ExportTimeline(timeline, catalog),
            BehaviorTreeDocument tree => ExportTree(tree, catalog),
            _ => throw new NotSupportedException(document.GetType().FullName),
        };
        return result;
    }

    private static RuntimeTaskGroup ExportTimeline(TimelineDocument document, TaskCatalog catalog)
    {
        var timelineDefinition = catalog.FindTask("TaskTimeline")
                                 ?? throw new InvalidDataException("TaskTimeline is missing from the metadata.");
        var group = new RuntimeTaskGroup
        {
            BindingContextFullType = document.BindingContextType,
            RootTaskId = 0,
        };
        var root = new RuntimeTaskValue { FullTypeName = timelineDefinition.FullTypeName };
        var hasEndless = document.Items.Any(item => item.Duration < 0);
        var maxTime = document.Items.Where(item => item.Duration >= 0)
            .Select(item => item.StartTime + item.Duration)
            .DefaultIfEmpty(0)
            .Max();
        root.Fields.Add(new RuntimeTaskField("Duration", FieldValueSource.Value, hasEndless ? "-1" : Format(maxTime)));
        group.Tasks.Add(0, root);

        var nextId = 1;
        foreach (var item in document.Items)
        {
            var itemId = nextId++;
            var itemValue = ConvertTask(item.Task, catalog);
            group.Tasks.Add(itemId, itemValue);
            root.TimelineItems.Add(new RuntimeTimelineItem(item.StartTime, item.Duration, itemId));
            AddConditions(item.EnterConditions, itemValue.EnterConditionReferences);
            AddConditions(item.Conditions, itemValue.ConditionReferences);
            AddConditions(item.ExitConditions, itemValue.ExitConditionReferences);

            void AddConditions(IEnumerable<TaskInstance> conditions, ICollection<int> references)
            {
                foreach (var condition in conditions)
                {
                    var conditionId = nextId++;
                    group.Tasks.Add(conditionId, ConvertTask(condition, catalog));
                    references.Add(conditionId);
                }
            }
        }
        return group;
    }

    private static RuntimeTaskGroup ExportTree(BehaviorTreeDocument document, TaskCatalog catalog)
    {
        var group = new RuntimeTaskGroup { BindingContextFullType = document.BindingContextType };
        var idByNode = new Dictionary<Guid, int>();
        for (var index = 0; index < document.Nodes.Count; index++)
        {
            var node = document.Nodes[index];
            idByNode[node.Id] = index;
            group.Tasks.Add(index, ConvertTask(node.Task, catalog));
            if (node.Task.TaskType == "TaskBtRoot")
            {
                group.RootTaskId = index;
            }
        }

        foreach (var edgeGroup in document.Edges
                     .OrderBy(edge => edge.Order)
                     .GroupBy(edge => (edge.SourceNodeId, edge.SourcePort)))
        {
            var sourceTask = group.Tasks[idByNode[edgeGroup.Key.SourceNodeId]];
            var targetIds = edgeGroup.Select(edge => idByNode[edge.TargetNodeId]).ToArray();
            switch (edgeGroup.Key.SourcePort)
            {
                case "EnterCondition": AddIds(sourceTask.EnterConditionReferences, targetIds); break;
                case "Condition": AddIds(sourceTask.ConditionReferences, targetIds); break;
                case "ExitCondition": AddIds(sourceTask.ExitConditionReferences, targetIds); break;
                default: AppendConnectPoint(sourceTask, edgeGroup.Key.SourcePort, targetIds); break;
            }
        }
        return group;
    }

    private static RuntimeTaskValue ConvertTask(TaskInstance task, TaskCatalog catalog)
    {
        var definition = catalog.FindTask(task.TaskType)
                         ?? throw new InvalidDataException($"Task type not found: {task.TaskType}");
        var result = new RuntimeTaskValue { FullTypeName = definition.FullTypeName };
        foreach (var field in task.Fields)
        {
            result.Fields.Add(new RuntimeTaskField(field.FieldName, field.Source, field.Value));
        }
        return result;
    }

    private static void AppendConnectPoint(RuntimeTaskValue task, string fieldName, IEnumerable<int> targetIds)
    {
        var index = task.Fields.Select((field, position) => (field, position))
            .FirstOrDefault(item => item.field.FieldName == fieldName).position;
        var existing = task.Fields.Count > 0 && index < task.Fields.Count && task.Fields[index].FieldName == fieldName
            ? task.Fields[index]
            : new RuntimeTaskField(fieldName, FieldValueSource.Value, string.Empty);
        var value = existing.Value + string.Concat(targetIds.Select(id => id.ToString(CultureInfo.InvariantCulture) + TaskContractConstants.ListElementSeparator));
        var updated = existing with { Value = value };
        if (task.Fields.Count > 0 && index < task.Fields.Count && task.Fields[index].FieldName == fieldName)
        {
            task.Fields[index] = updated;
        }
        else
        {
            task.Fields.Add(updated);
        }
    }

    private static string Format(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static void AddIds(ICollection<int> destination, IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            destination.Add(value);
        }
    }
}
