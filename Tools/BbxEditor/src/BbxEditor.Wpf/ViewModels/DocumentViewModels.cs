using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using BbxEditor.Contracts;
using BbxEditor.Application;
using BbxEditor.Diagnostics;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;
using BbxEditor.Wpf.Presentation;
using BbxEditor.Wpf.Services;

namespace BbxEditor.Wpf.ViewModels;

public abstract class DocumentViewModel : ObservableObject
{
    private bool _isPreview;

    protected DocumentViewModel(EditorDocument document, MainViewModel owner)
    {
        Document = document;
        Owner = owner;
        CloseCommand = new RelayCommand(() => owner.CloseDocument(this));
        Document.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(EditorDocument.DisplayName) or nameof(EditorDocument.IsDirty))
            {
                RaisePropertyChanged(nameof(Header));
            }
        };
    }

    public EditorDocument Document { get; }
    public TaskDocument? TaskDocument => Document as TaskDocument;
    public CsvDocument? CsvDocument => Document as CsvDocument;
    public virtual CsvRow? SelectedCsvRow => null;
    public bool IsTask => Document is TaskDocument;
    public bool IsCsv => Document is CsvDocument;
    public bool IsScriptableObject => Document is ScriptableObjectDocument;
    public bool IsDesignPlan => Document is DesignPlanDocument;
    public virtual bool CanSave => true;
    public virtual bool TracksRecentFiles => true;
    public virtual string? Priority => null;
    public virtual string? PriorityColor => null;
    public virtual bool HasPriority => false;
    public virtual string? State => null;
    public virtual string? StateColor => null;
    public virtual bool HasState => false;
    public MainViewModel Owner { get; }
    public virtual string Header => Document.DisplayName;
    public bool IsPreview
    {
        get => _isPreview;
        set => SetProperty(ref _isPreview, value);
    }
    public RelayCommand CloseCommand { get; }

    public void SelectTask(TaskInstance? task) => Owner.SelectedTask = task;
    public void MarkDirty() => Document.IsDirty = true;
    public virtual void OnPinned() { }
    public virtual void Dispose() { }
}

public sealed class DesignPlanDocumentViewModel : DocumentViewModel
{
    public DesignPlanDocumentViewModel(DesignPlanDocument document, MainViewModel owner) : base(document, owner) { }

    public DesignPlanDocument DesignPlan => (DesignPlanDocument)Document;
    public override string Header => DesignPlan.Title;
    public override bool CanSave => false;
    public override bool TracksRecentFiles => false;
    public override string? Priority => DesignPlan.Priority;
    public override string? PriorityColor => DesignPlanMetadataPresentation.GetPriorityColor(Priority);
    public override bool HasPriority => PriorityColor is not null;
    public override string? State => DesignPlan.State;
    public override string? StateColor => DesignPlanMetadataPresentation.GetStateColor(State);
    public override bool HasState => StateColor is not null;
}

public sealed class CsvDocumentViewModel : DocumentViewModel
{
    private CsvRow? _selectedRow;

    public CsvDocumentViewModel(CsvDocument document, MainViewModel owner) : base(document, owner)
    {
        AddRowCommand = new RelayCommand(AddRow);
        DeleteRowCommand = new RelayCommand(DeleteRow, () => SelectedRow is not null);
        ValidateCommand = new RelayCommand(Validate);
    }

    public CsvDocument Csv => (CsvDocument)Document;
    public CsvRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (SetProperty(ref _selectedRow, value))
            {
                RaisePropertyChanged(nameof(SelectedCsvRow));
                DeleteRowCommand.RaiseCanExecuteChanged();
            }
        }
    }
    public override CsvRow? SelectedCsvRow => SelectedRow;
    public string MetadataText => Csv.Metadata is null ? "Untyped CSV" : Csv.Metadata.FullTypeName;
    public RelayCommand AddRowCommand { get; }
    public RelayCommand DeleteRowCommand { get; }
    public RelayCommand ValidateCommand { get; }

    private void AddRow()
    {
        var row = new CsvRow();
        foreach (var _ in Csv.Columns) row.Cells.Add(new CsvCell());
        Csv.AddRow(row);
        SelectedRow = row;
    }

    private void DeleteRow()
    {
        if (SelectedRow is null) return;
        Csv.RemoveRow(SelectedRow);
        SelectedRow = null;
    }

    private void Validate()
    {
        var diagnostics = CsvDocumentCodec.Validate(Csv);
        if (diagnostics.Count == 0) Owner.SetStatus("CSV validation passed.");
        else Owner.RecordDiagnostics(diagnostics);
    }
}

public sealed class ScriptableObjectDocumentViewModel : DocumentViewModel
{
    public ScriptableObjectDocumentViewModel(ScriptableObjectDocument document, MainViewModel owner) : base(document, owner)
    {
        ValidateCommand = new RelayCommand(Validate);
    }

    public ScriptableObjectDocument Asset => (ScriptableObjectDocument)Document;
    public string MetadataText => Asset.Metadata?.FullTypeName ?? "Unknown BbxScriptableObject";
    public RelayCommand ValidateCommand { get; }

    private void Validate()
    {
        var diagnostics = ScriptableObjectDocumentCodec.Validate(Asset);
        if (diagnostics.Count == 0) Owner.SetStatus("BbxScriptableObject validation passed.");
        else Owner.RecordDiagnostics(diagnostics);
    }
}

public sealed class TimelineDocumentViewModel : DocumentViewModel
{
    private TimelineItem? _selectedItem;

    public TimelineDocumentViewModel(TimelineDocument document, MainViewModel owner) : base(document, owner)
    {
        AddTaskCommand = new RelayCommand(AddSelectedTask);
        MoveUpCommand = new RelayCommand(item => Move(item as TimelineItem, -1));
        MoveDownCommand = new RelayCommand(item => Move(item as TimelineItem, 1));
        DeleteCommand = new RelayCommand(item => Delete(item as TimelineItem));
        AddEnterConditionCommand = new RelayCommand(item => AddCondition(item as TimelineItem, EConditionGroup.Enter));
        AddConditionCommand = new RelayCommand(item => AddCondition(item as TimelineItem, EConditionGroup.Normal));
        AddExitConditionCommand = new RelayCommand(item => AddCondition(item as TimelineItem, EConditionGroup.Exit));
        DeleteConditionCommand = new RelayCommand(DeleteCondition);
        SelectTaskCommand = new RelayCommand(SelectTimelineTask);
    }

    public TimelineDocument Timeline => (TimelineDocument)Document;
    public ObservableCollection<TimelineItem> Items => Timeline.Items;
    public RelayCommand AddTaskCommand { get; }
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand AddEnterConditionCommand { get; }
    public RelayCommand AddConditionCommand { get; }
    public RelayCommand AddExitConditionCommand { get; }
    public RelayCommand DeleteConditionCommand { get; }
    public RelayCommand SelectTaskCommand { get; }

    public TimelineItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                Owner.SelectedTimelineItem = value;
                SelectTask(value?.Task);
            }
        }
    }

    private void AddSelectedTask()
    {
        var definition = Owner.SelectTask(
            "Select Action Node",
            "Select an Action type to add to the Timeline.",
            task => task.HasTag(TaskContractConstants.TagAction));
        if (definition is null) return;
        if (!definition.HasTag(TaskContractConstants.TagAction))
        {
            Owner.SetStatus("The Timeline can only contain tasks tagged as Action.", true);
            return;
        }
        var item = new TimelineItem { Task = TaskInstance.FromDefinition(definition) };
        Items.Add(item);
        MarkDirty();
        SelectedItem = item;
    }

    private void Move(TimelineItem? item, int direction)
    {
        if (item is null) return;
        var oldIndex = Items.IndexOf(item);
        var newIndex = Math.Clamp(oldIndex + direction, 0, Items.Count - 1);
        if (oldIndex == newIndex) return;
        Items.Move(oldIndex, newIndex);
        MarkDirty();
    }

    private void Delete(TimelineItem? item)
    {
        if (item is null || !Items.Remove(item)) return;
        MarkDirty();
        if (ReferenceEquals(SelectedItem, item)) SelectedItem = null;
    }

    private void AddCondition(TimelineItem? item, EConditionGroup group)
    {
        if (item is null) return;
        var definition = Owner.SelectTask(
            "Select Condition Node",
            $"Select a Condition type to add to the {group} condition group.",
            task => task.HasTag(TaskContractConstants.TagCondition));
        if (definition is null) return;
        if (!definition.HasTag(TaskContractConstants.TagCondition))
        {
            Owner.SetStatus("Condition lists can only contain tasks tagged as Condition.", true);
            return;
        }
        var task = TaskInstance.FromDefinition(definition);
        GetConditionList(item, group).Add(task);
        item.ConditionsExpanded = true;
        MarkDirty();
        SelectTask(task);
    }

    private void DeleteCondition(object? parameter)
    {
        if (parameter is not TaskInstance task) return;
        foreach (var item in Items)
        {
            if (item.EnterConditions.Remove(task) || item.Conditions.Remove(task) || item.ExitConditions.Remove(task))
            {
                MarkDirty();
                SelectTask(null);
                return;
            }
        }
    }

    private static ICollection<TaskInstance> GetConditionList(TimelineItem item, EConditionGroup group) => group switch
    {
        EConditionGroup.Enter => item.EnterConditions,
        EConditionGroup.Normal => item.Conditions,
        EConditionGroup.Exit => item.ExitConditions,
        _ => throw new ArgumentOutOfRangeException(nameof(group)),
    };

    private void SelectTimelineTask(object? parameter)
    {
        var task = parameter as TaskInstance;
        Owner.SelectedTimelineItem = SelectedItem;
        SelectTask(task);
    }

    private enum EConditionGroup { Enter, Normal, Exit }
}

public sealed class BehaviorTreeDocumentViewModel : DocumentViewModel
{
    private BehaviorNode? _selectedNode;
    private BehaviorNode? _highlightedSearchNode;
    private BehaviorNode? _connectionSource;
    private string? _selectedPort;
    private CancellationTokenSource? _nodeSearchIndexCancellation;
    private TransientVectorIndex? _nodeSearchIndex;
    private string _nodeSearchIndexStatus = "Literal search only";
    private int _nodeSearchIndexVersion;
    private bool _nodeSearchPinned;
    private bool _disposed;

    public BehaviorTreeDocumentViewModel(BehaviorTreeDocument document, MainViewModel owner) : base(document, owner)
    {
        AddNodeCommand = new RelayCommand(AddSelectedTask);
        DeleteNodeCommand = new RelayCommand(DeleteSelectedNode);
        SetConnectionSourceCommand = new RelayCommand(SetConnectionSource);
        ConnectToSelectedCommand = new RelayCommand(ConnectToSelected);
        DisconnectIncomingCommand = new RelayCommand(DisconnectIncoming);
        MoveEdgeUpCommand = new RelayCommand(() => MoveIncoming(-1));
        MoveEdgeDownCommand = new RelayCommand(() => MoveIncoming(1));
        ValidateCommand = new RelayCommand(Validate);
        Nodes.CollectionChanged += OnSearchNodesChanged;
        foreach (var node in Nodes) node.PropertyChanged += OnSearchNodePropertyChanged;
    }

    public BehaviorTreeDocument Tree => (BehaviorTreeDocument)Document;
    public ObservableCollection<BehaviorNode> Nodes => Tree.Nodes;
    public ObservableCollection<BehaviorEdge> Edges => Tree.Edges;
    public RelayCommand AddNodeCommand { get; }
    public RelayCommand DeleteNodeCommand { get; }
    public RelayCommand SetConnectionSourceCommand { get; }
    public RelayCommand ConnectToSelectedCommand { get; }
    public RelayCommand DisconnectIncomingCommand { get; }
    public RelayCommand MoveEdgeUpCommand { get; }
    public RelayCommand MoveEdgeDownCommand { get; }
    public RelayCommand ValidateCommand { get; }
    public int NodeSearchIndexVersion => _nodeSearchIndexVersion;
    public string NodeSearchIndexStatus
    {
        get => _nodeSearchIndexStatus;
        private set => SetProperty(ref _nodeSearchIndexStatus, value);
    }

    public BehaviorNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                Owner.SelectedTimelineItem = null;
                SelectTask(value?.Task);
                RaisePropertyChanged(nameof(AvailablePorts));
                SelectedPort = AvailablePorts.FirstOrDefault();
            }
        }
    }

    public BehaviorNode? HighlightedSearchNode
    {
        get => _highlightedSearchNode;
        set => SetProperty(ref _highlightedSearchNode, value);
    }

    public override void OnPinned()
    {
        if (_disposed || _nodeSearchPinned) return;
        _nodeSearchPinned = true;
        RebuildNodeSearchIndex();
    }

    internal void RebuildNodeSearchIndex()
    {
        if (_disposed || !_nodeSearchPinned) return;
        var next = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _nodeSearchIndexCancellation, next);
        previous?.Cancel();
        previous?.Dispose();
        _nodeSearchIndex = null;
        NodeSearchIndexStatus = "Indexing semantic search…";
        RaiseSearchIndexVersion();
        _ = BuildNodeSearchIndexAsync(next.Token);
    }

    internal async Task<IReadOnlyList<BehaviorNode>> FindNodesAsync(string query, CancellationToken cancellationToken)
    {
        var nodes = Nodes.ToArray();
        var index = _nodeSearchIndex;
        IReadOnlyList<string> rankedTexts = [];
        if (index is not null)
        {
            try
            {
                rankedTexts = await Owner.RankTransientVectorIndexAsync(query, index, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Keep literal node search available if the transient vector query fails.
            }
        }
        return BehaviorTreeNodeSearch.Rank(nodes, query, rankedTexts).Select(result => result.Node).ToArray();
    }

    private async Task BuildNodeSearchIndexAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(80, cancellationToken);
            var nodes = Nodes.ToArray();
            var texts = nodes.SelectMany(node => new[]
                {
                    BehaviorTreeNodeSearch.GetTitleVectorText(node),
                    BehaviorTreeNodeSearch.GetTypeVectorText(node),
                })
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var index = await Owner.BuildTransientVectorIndexAsync(texts, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _nodeSearchIndex = index;
            NodeSearchIndexStatus = index is null ? "Literal search only" : "Semantic search ready";
            RaiseSearchIndexVersion();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            if (cancellationToken.IsCancellationRequested) return;
            _nodeSearchIndex = null;
            NodeSearchIndexStatus = "Literal search only";
            RaiseSearchIndexVersion();
        }
    }

    private void RaiseSearchIndexVersion()
    {
        _nodeSearchIndexVersion++;
        RaisePropertyChanged(nameof(NodeSearchIndexVersion));
    }

    private void OnSearchNodesChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems is not null)
            foreach (var node in args.OldItems.OfType<BehaviorNode>()) node.PropertyChanged -= OnSearchNodePropertyChanged;
        if (args.NewItems is not null)
            foreach (var node in args.NewItems.OfType<BehaviorNode>()) node.PropertyChanged += OnSearchNodePropertyChanged;
        RebuildNodeSearchIndex();
    }

    private void OnSearchNodePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(BehaviorNode.Name)) RebuildNodeSearchIndex();
    }

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Nodes.CollectionChanged -= OnSearchNodesChanged;
        foreach (var node in Nodes) node.PropertyChanged -= OnSearchNodePropertyChanged;
        var cancellation = Interlocked.Exchange(ref _nodeSearchIndexCancellation, null);
        cancellation?.Cancel();
        cancellation?.Dispose();
        _nodeSearchIndex = null;
    }

    public BehaviorNode? ConnectionSource
    {
        get => _connectionSource;
        private set
        {
            if (SetProperty(ref _connectionSource, value))
            {
                RaisePropertyChanged(nameof(ConnectionSourceText));
                RaisePropertyChanged(nameof(AvailablePorts));
                SelectedPort = AvailablePorts.FirstOrDefault();
            }
        }
    }

    public string ConnectionSourceText => ConnectionSource is null ? "No connection source" : $"Connection source: {ConnectionSource.Name}";

    public IReadOnlyList<string> AvailablePorts
    {
        get
        {
            var source = ConnectionSource ?? SelectedNode;
            if (source is null) return [];
            var definition = Owner.Catalog.FindTask(source.Task.TaskType);
            if (definition?.HasTag(TaskContractConstants.TagCondition) == true) return [];
            return new[] { "EnterCondition", "Condition", "ExitCondition" }
                .Concat(source.Task.Fields.Where(taskField => taskField.Type.IsConnectPoint).Select(taskField => taskField.FieldName))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }

    public string? SelectedPort
    {
        get => _selectedPort;
        set => SetProperty(ref _selectedPort, value);
    }

    public void NodeMoved(BehaviorNode node, EditorPoint point)
    {
        node.Position = point;
        MarkDirty();
    }

    private void AddSelectedTask()
    {
        var definition = Owner.SelectTask(
            "Select Behavior Tree Node",
            "Select a node type to add to the Behavior Tree canvas.",
            task => !task.HasTag(TaskContractConstants.TagTimeline) &&
                    (task.TypeName != "TaskBtRoot" || Nodes.All(node => node.Task.TaskType != "TaskBtRoot")));
        if (definition is null) return;
        _ = AddTask(definition);
    }

    public BehaviorNode AddTask(TaskDefinition definition, EditorPoint? position = null)
    {
        var prefix = definition.TypeName;
        var used = Nodes.Select(node => node.Name).ToHashSet(StringComparer.Ordinal);
        var index = 0;
        while (used.Contains($"{prefix}_{index}")) index++;
        var node = new BehaviorNode
        {
            Name = $"{prefix}_{index}",
            Task = TaskInstance.FromDefinition(definition),
            Position = position ?? new EditorPoint(80 + Nodes.Count % 4 * 240, 80 + Nodes.Count / 4 * 180),
        };
        Nodes.Add(node);
        SelectedNode = node;
        MarkDirty();
        return node;
    }

    private void DeleteSelectedNode()
    {
        if (SelectedNode is not null) RemoveNode(SelectedNode);
    }

    public void RemoveNode(BehaviorNode node)
    {
        var id = node.Id;
        foreach (var edge in Edges.Where(edge => edge.SourceNodeId == id || edge.TargetNodeId == id).ToArray()) Edges.Remove(edge);
        if (!Nodes.Remove(node)) return;
        if (ReferenceEquals(SelectedNode, node)) SelectedNode = null;
        if (ConnectionSource?.Id == id) ConnectionSource = null;
        ReindexAll();
        MarkDirty();
    }

    private void SetConnectionSource()
    {
        ConnectionSource = SelectedNode;
        Owner.SetStatus(ConnectionSourceText);
    }

    private void ConnectToSelected()
    {
        if (ConnectionSource is null || SelectedNode is null || string.IsNullOrWhiteSpace(SelectedPort)) return;
        _ = TryConnect(ConnectionSource, SelectedPort, SelectedNode);
    }

    public bool TryConnect(BehaviorNode source, string sourcePort, BehaviorNode target)
    {
        if (source.Id == target.Id)
        {
            Owner.SetStatus("A node cannot be connected to itself.", true);
            return false;
        }
        if (Edges.Any(edge => edge.TargetNodeId == target.Id))
        {
            Owner.SetStatus("The target node already has a parent connection.", true);
            return false;
        }
        if (target.Task.TaskType == "TaskBtRoot")
        {
            Owner.SetStatus("TaskBtRoot cannot have a parent connection.", true);
            return false;
        }
        var sourceDefinition = Owner.Catalog.FindTask(source.Task.TaskType);
        var targetDefinition = Owner.Catalog.FindTask(target.Task.TaskType);
        if (sourceDefinition?.HasTag(TaskContractConstants.TagCondition) == true)
        {
            Owner.SetStatus("Condition nodes do not have output ports.", true);
            return false;
        }
        var conditionPort = DocumentValidator.IsConditionPort(sourcePort);
        var targetIsCondition = targetDefinition?.HasTag(TaskContractConstants.TagCondition) == true;
        if (conditionPort != targetIsCondition)
        {
            Owner.SetStatus(conditionPort ? "Condition ports can only connect to Condition nodes." : "Task ports cannot connect to Condition nodes.", true);
            return false;
        }
        var taskField = source.Task.FindField(sourcePort);
        if (!conditionPort && taskField?.Type.IsConnectPoint != true)
        {
            Owner.SetStatus($"The connection source does not have port: {sourcePort}", true);
            return false;
        }
        if (taskField?.Type.IsSingleConnectPoint == true && Edges.Any(edge => edge.SourceNodeId == source.Id && edge.SourcePort == sourcePort))
        {
            Owner.SetStatus("The Single port already has a connection.", true);
            return false;
        }
        if (CanReach(target.Id, source.Id))
        {
            Owner.SetStatus("This connection would create a cycle in the Behavior Tree.", true);
            return false;
        }
        var order = Edges.Where(edge => edge.SourceNodeId == source.Id && edge.SourcePort == sourcePort)
            .Select(edge => edge.Order).DefaultIfEmpty(-1).Max() + 1;
        Edges.Add(new BehaviorEdge
        {
            SourceNodeId = source.Id,
            SourcePort = sourcePort,
            TargetNodeId = target.Id,
            Order = order,
        });
        MarkDirty();
        return true;
    }

    private bool CanReach(Guid start, Guid target)
    {
        var visited = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(start);
        while (pending.TryPop(out var current) && visited.Add(current))
        {
            if (current == target) return true;
            foreach (var edge in Edges.Where(edge => edge.SourceNodeId == current)) pending.Push(edge.TargetNodeId);
        }
        return false;
    }

    private void DisconnectIncoming()
    {
        if (SelectedNode is null) return;
        var edge = Edges.FirstOrDefault(item => item.TargetNodeId == SelectedNode.Id);
        if (edge is null) return;
        Edges.Remove(edge);
        Reindex(edge.SourceNodeId, edge.SourcePort);
        MarkDirty();
    }

    private void MoveIncoming(int direction)
    {
        if (SelectedNode is null) return;
        var incoming = Edges.FirstOrDefault(edge => edge.TargetNodeId == SelectedNode.Id);
        if (incoming is null) return;
        var siblings = Edges.Where(edge => edge.SourceNodeId == incoming.SourceNodeId && edge.SourcePort == incoming.SourcePort)
            .OrderBy(edge => edge.Order).ToList();
        var oldIndex = siblings.IndexOf(incoming);
        var newIndex = Math.Clamp(oldIndex + direction, 0, siblings.Count - 1);
        if (oldIndex == newIndex) return;
        (siblings[oldIndex], siblings[newIndex]) = (siblings[newIndex], siblings[oldIndex]);
        for (var i = 0; i < siblings.Count; i++) siblings[i].Order = i;
        MarkDirty();
        RaisePropertyChanged(nameof(Edges));
    }

    private void ReindexAll()
    {
        foreach (var group in Edges.GroupBy(edge => (edge.SourceNodeId, edge.SourcePort))) Reindex(group.Key.SourceNodeId, group.Key.SourcePort);
    }

    private void Reindex(Guid sourceNodeId, string sourcePort)
    {
        var index = 0;
        foreach (var edge in Edges.Where(edge => edge.SourceNodeId == sourceNodeId && edge.SourcePort == sourcePort).OrderBy(edge => edge.Order)) edge.Order = index++;
    }

    private void Validate()
    {
        var diagnostics = DocumentValidator.Validate(Tree, Owner.Catalog);
        if (diagnostics.Count == 0)
        {
            Owner.SetStatus("Behavior Tree validation passed.", false);
            return;
        }
        Owner.RecordDiagnostics(diagnostics);
    }
}
