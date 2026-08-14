using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BbxEditor.Contracts;
using BbxEditor.Domain;
using BbxEditor.Wpf.ViewModels;

namespace BbxEditor.Wpf.Views;

public sealed class BehaviorTreeCanvas : FrameworkElement
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel), typeof(BehaviorTreeDocumentViewModel), typeof(BehaviorTreeCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnViewModelChanged));

    private const double NodeWidth = 210;
    private const double BaseNodeHeight = 78;
    private BehaviorNode? _dragNode;
    private BehaviorNode? _hoverNode;
    private BehaviorNode? _connectionDragNode;
    private string? _connectionDragPort;
    private Point _connectionMouse;
    private Point _lastMouse;
    private bool _panning;
    private Vector _pan = new(20, 20);
    private double _zoom = 1;

    public BehaviorTreeCanvas()
    {
        Focusable = true;
        ClipToBounds = true;
        MouseLeftButtonDown += OnLeftDown;
        MouseLeftButtonUp += OnLeftUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
        KeyDown += OnKeyDown;
        LostMouseCapture += OnLostMouseCapture;
        MouseLeave += OnMouseLeave;
    }

    public BehaviorTreeDocumentViewModel? ViewModel
    {
        get => (BehaviorTreeDocumentViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(39, 41, 45)), null, new Rect(RenderSize));
        DrawGrid(drawingContext);
        if (ViewModel is null) return;
        foreach (var edge in ViewModel.Edges.OrderBy(edge => edge.Order)) DrawEdge(drawingContext, edge);
        DrawConnectionPreview(drawingContext);
        foreach (var node in ViewModel.Nodes) DrawNode(drawingContext, node);
    }

    private void DrawGrid(DrawingContext context)
    {
        var minorSpacing = 20 * _zoom;
        if (minorSpacing >= 8)
        {
            var minorPen = new Pen(new SolidColorBrush(Color.FromRgb(48, 51, 56)), 1);
            var startX = _pan.X % minorSpacing;
            var startY = _pan.Y % minorSpacing;
            for (var x = startX; x < RenderSize.Width; x += minorSpacing) context.DrawLine(minorPen, new Point(x, 0), new Point(x, RenderSize.Height));
            for (var y = startY; y < RenderSize.Height; y += minorSpacing) context.DrawLine(minorPen, new Point(0, y), new Point(RenderSize.Width, y));
        }

        var majorSpacing = 100 * _zoom;
        if (majorSpacing < 12) return;
        var majorPen = new Pen(new SolidColorBrush(Color.FromRgb(57, 60, 66)), 1);
        var majorStartX = _pan.X % majorSpacing;
        var majorStartY = _pan.Y % majorSpacing;
        for (var x = majorStartX; x < RenderSize.Width; x += majorSpacing) context.DrawLine(majorPen, new Point(x, 0), new Point(x, RenderSize.Height));
        for (var y = majorStartY; y < RenderSize.Height; y += majorSpacing) context.DrawLine(majorPen, new Point(0, y), new Point(RenderSize.Width, y));
    }

    private void DrawEdge(DrawingContext context, BehaviorEdge edge)
    {
        if (ViewModel is null) return;
        var source = ViewModel.Nodes.FirstOrDefault(node => node.Id == edge.SourceNodeId);
        var target = ViewModel.Nodes.FirstOrDefault(node => node.Id == edge.TargetNodeId);
        if (source is null || target is null) return;
        var start = WorldToScreen(GetOutputPortPoint(source, edge.SourcePort));
        var end = WorldToScreen(GetInputPortPoint(target));
        DrawBezier(context, start, end, edge.SourcePort, $"{edge.SourcePort} [{edge.Order}]");
    }

    private void DrawConnectionPreview(DrawingContext context)
    {
        if (_connectionDragNode is null || _connectionDragPort is null) return;
        var start = WorldToScreen(GetOutputPortPoint(_connectionDragNode, _connectionDragPort));
        DrawBezier(context, start, _connectionMouse, _connectionDragPort, null);
    }

    private void DrawBezier(DrawingContext context, Point start, Point end, string port, string? labelText)
    {
        var (firstControl, secondControl) = CalculateBezierControls(start, end);
        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(start, false, false);
            stream.BezierTo(firstControl, secondControl, end, true, false);
        }
        geometry.Freeze();
        var condition = port is "EnterCondition" or "Condition" or "ExitCondition";
        var brush = new SolidColorBrush(condition ? Color.FromRgb(146, 180, 154) : Color.FromRgb(174, 183, 192));
        context.DrawGeometry(null, new Pen(brush, Math.Max(1.2, 2 * _zoom)), geometry);
        if (labelText is not null)
        {
            var label = CreateText(labelText, 10, brush);
            context.DrawText(label, new Point((start.X + end.X) / 2 - label.Width / 2, (start.Y + end.Y) / 2 - label.Height - 2));
        }
    }

    internal static (Point First, Point Second) CalculateBezierControls(Point start, Point end)
    {
        var horizontalGap = end.X - start.X;
        if (horizontalGap >= 0)
        {
            // Keep forward curves monotonic. A fixed minimum handle makes close ports overshoot and curl back.
            var handle = Math.Min(160, horizontalGap * .45);
            return (new Point(start.X + handle, start.Y), new Point(end.X - handle, end.Y));
        }

        // A backward connection needs some clearance outside both nodes, but it should not grow without bound.
        var separation = Math.Sqrt(horizontalGap * horizontalGap + Math.Pow(end.Y - start.Y, 2));
        var returnHandle = Math.Clamp(24 + separation * .15, 28, 90);
        return (new Point(start.X + returnHandle, start.Y), new Point(end.X - returnHandle, end.Y));
    }

    private void DrawNode(DrawingContext context, BehaviorNode node)
    {
        if (ViewModel is null) return;
        var topLeft = WorldToScreen(new Point(node.Position.X, node.Position.Y));
        var rect = new Rect(topLeft, new Size(NodeWidth * _zoom, NodeHeight(node) * _zoom));
        var definition = ViewModel.Owner.Catalog.FindTask(node.Task.TaskType);
        var isCondition = definition?.HasTag(TaskContractConstants.TagCondition) == true;
        var isHovered = ReferenceEquals(node, _hoverNode);
        var isSelected = ReferenceEquals(node, ViewModel.SelectedNode);
        var isSearchHighlighted = ReferenceEquals(node, ViewModel.HighlightedSearchNode);
        var isDragging = ReferenceEquals(node, _dragNode);
        var fillColor = isCondition
            ? (isHovered ? Color.FromRgb(66, 82, 71) : Color.FromRgb(57, 72, 62))
            : (isHovered ? Color.FromRgb(66, 70, 77) : Color.FromRgb(55, 59, 65));
        var borderColor = isSearchHighlighted ? Color.FromRgb(102, 201, 232)
            : isDragging ? Color.FromRgb(207, 218, 226)
            : isSelected ? Color.FromRgb(164, 184, 201)
            : node == ViewModel.ConnectionSource ? Color.FromRgb(139, 180, 184)
            : Color.FromRgb(95, 102, 111);
        var radius = 8 * _zoom;
        var shadowRect = new Rect(rect.X + 3 * _zoom, rect.Y + 4 * _zoom, rect.Width, rect.Height);
        context.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(100, 8, 9, 10)), null, shadowRect, radius, radius);
        context.DrawRoundedRectangle(new SolidColorBrush(fillColor),
            new Pen(new SolidColorBrush(borderColor), isSearchHighlighted ? 4 : isSelected || isDragging ? 2.5 : 1.3), rect, radius, radius);
        var dividerY = topLeft.Y + 52 * _zoom;
        context.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(82, 88, 96)), 1),
            new Point(rect.Left + 1, dividerY), new Point(rect.Right - 1, dividerY));
        context.DrawText(CreateText(node.Name, 15 * _zoom, new SolidColorBrush(Color.FromRgb(240, 241, 243))), topLeft + new Vector(10 * _zoom, 6 * _zoom));
        context.DrawText(CreateText(node.Task.TaskType, 12 * _zoom, new SolidColorBrush(Color.FromRgb(176, 182, 190))), topLeft + new Vector(10 * _zoom, 30 * _zoom));
        var input = WorldToScreen(new Point(node.Position.X, node.Position.Y + NodeHeight(node) / 2));
        var conditionBrush = new SolidColorBrush(Color.FromRgb(153, 184, 160));
        var taskBrush = new SolidColorBrush(Color.FromRgb(197, 204, 211));
        context.DrawEllipse(isCondition ? conditionBrush : taskBrush, new Pen(new SolidColorBrush(Color.FromRgb(45, 48, 53)), 1), input, 5 * _zoom, 5 * _zoom);
        var ports = GetPorts(node).ToArray();
        for (var index = 0; index < ports.Length; index++)
        {
            var output = WorldToScreen(GetOutputPortPoint(node, ports[index]));
            var y = output.Y - 5 * _zoom;
            var portBrush = ports[index] is "EnterCondition" or "Condition" or "ExitCondition" ? conditionBrush : taskBrush;
            context.DrawEllipse(portBrush, new Pen(new SolidColorBrush(Color.FromRgb(45, 48, 53)), 1), output, 5 * _zoom, 5 * _zoom);
            context.DrawText(CreateText(ports[index], 11 * _zoom, portBrush), new Point(topLeft.X + 9 * _zoom, y));
        }
    }

    private IEnumerable<string> GetPorts(BehaviorNode node)
    {
        if (ViewModel?.Owner.Catalog.FindTask(node.Task.TaskType)?.HasTag(TaskContractConstants.TagCondition) == true) yield break;
        yield return "EnterCondition";
        yield return "Condition";
        yield return "ExitCondition";
        foreach (var field in node.Task.Fields.Where(field => field.Type.IsConnectPoint)) yield return field.FieldName;
    }

    private double NodeHeight(BehaviorNode node) => Math.Max(BaseNodeHeight, 64 + GetPorts(node).Count() * 20);

    public void CenterOnNode(BehaviorNode node)
    {
        var nodeCenter = new Point(node.Position.X + NodeWidth / 2, node.Position.Y + NodeHeight(node) / 2);
        _pan = CalculateCenteredPan(nodeCenter, RenderSize, _zoom);
        InvalidateVisual();
    }

    internal static Vector CalculateCenteredPan(Point worldCenter, Size viewport, double zoom) =>
        new(viewport.Width / 2 - worldCenter.X * zoom, viewport.Height / 2 - worldCenter.Y * zoom);

    private void OnLeftDown(object sender, MouseButtonEventArgs args)
    {
        Focus();
        var point = args.GetPosition(this);
        var output = HitOutputPort(point);
        if (output is not null && ViewModel is not null)
        {
            _connectionDragNode = output.Value.Node;
            _connectionDragPort = output.Value.Port;
            _connectionMouse = point;
            ViewModel.SelectedNode = output.Value.Node;
            Cursor = Cursors.Cross;
            CaptureMouse();
            InvalidateVisual();
            args.Handled = true;
            return;
        }
        var world = ScreenToWorld(point);
        var node = HitNode(world);
        if (ViewModel is not null) ViewModel.SelectedNode = node;
        if (node is not null)
        {
            _dragNode = node;
            _lastMouse = point;
            Cursor = Cursors.SizeAll;
            CaptureMouse();
        }
        InvalidateVisual();
        args.Handled = true;
    }

    private void OnLeftUp(object sender, MouseButtonEventArgs args)
    {
        if (_connectionDragNode is not null && _connectionDragPort is not null)
        {
            CompleteConnection(args.GetPosition(this));
            ClearConnectionDrag();
            args.Handled = true;
            return;
        }
        _dragNode = null;
        if (!_panning) ReleaseMouseCapture();
        Cursor = HitNode(ScreenToWorld(args.GetPosition(this))) is null ? Cursors.Arrow : Cursors.SizeAll;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs args)
    {
        if (args.ChangedButton is not MouseButton.Middle and not MouseButton.Right) return;
        _panning = true;
        _lastMouse = args.GetPosition(this);
        Cursor = Cursors.ScrollAll;
        CaptureMouse();
        args.Handled = true;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs args)
    {
        if (args.ChangedButton is not MouseButton.Middle and not MouseButton.Right) return;
        _panning = false;
        if (_dragNode is null) ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
        args.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs args)
    {
        var point = args.GetPosition(this);
        var delta = point - _lastMouse;
        if (_connectionDragNode is not null && args.LeftButton == MouseButtonState.Pressed)
        {
            _connectionMouse = point;
            InvalidateVisual();
        }
        else if (_dragNode is not null && args.LeftButton == MouseButtonState.Pressed && ViewModel is not null)
        {
            ViewModel.NodeMoved(_dragNode, new EditorPoint(_dragNode.Position.X + delta.X / _zoom, _dragNode.Position.Y + delta.Y / _zoom));
            _lastMouse = point;
            InvalidateVisual();
        }
        else if (_panning)
        {
            _pan += delta;
            _lastMouse = point;
            InvalidateVisual();
        }
        else
        {
            var hoverNode = HitNode(ScreenToWorld(point));
            if (!ReferenceEquals(_hoverNode, hoverNode))
            {
                _hoverNode = hoverNode;
                InvalidateVisual();
            }
            Cursor = HitOutputPort(point) is not null ? Cursors.Hand : hoverNode is not null ? Cursors.SizeAll : Cursors.Arrow;
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs args)
    {
        var mouse = args.GetPosition(this);
        var worldBefore = ScreenToWorld(mouse);
        _zoom = Math.Clamp(_zoom * (args.Delta > 0 ? 1.12 : 1 / 1.12), .35, 2.5);
        _pan = (Vector)mouse - new Vector(worldBefore.X * _zoom, worldBefore.Y * _zoom);
        InvalidateVisual();
        args.Handled = true;
    }

    private BehaviorNode? HitNode(Point world) => ViewModel?.Nodes.Reverse().FirstOrDefault(node =>
        new Rect(node.Position.X, node.Position.Y, NodeWidth, NodeHeight(node)).Contains(world));

    private (BehaviorNode Node, string Port)? HitOutputPort(Point screen)
    {
        if (ViewModel is null) return null;
        var radius = Math.Max(8, 9 * _zoom);
        foreach (var node in ViewModel.Nodes.Reverse())
        {
            foreach (var port in GetPorts(node))
            {
                var point = WorldToScreen(GetOutputPortPoint(node, port));
                if ((screen - point).Length <= radius) return (node, port);
            }
        }
        return null;
    }

    private Point GetOutputPortPoint(BehaviorNode node, string port)
    {
        var index = Array.IndexOf(GetPorts(node).ToArray(), port);
        return index < 0
            ? new Point(node.Position.X + NodeWidth, node.Position.Y + NodeHeight(node) / 2)
            : new Point(node.Position.X + NodeWidth, node.Position.Y + 63 + index * 20);
    }

    private Point GetInputPortPoint(BehaviorNode node) =>
        new(node.Position.X, node.Position.Y + NodeHeight(node) / 2);

    private void CompleteConnection(Point screen)
    {
        if (ViewModel is null || _connectionDragNode is null || _connectionDragPort is null) return;
        var source = _connectionDragNode;
        var sourcePort = _connectionDragPort;
        var target = HitNode(ScreenToWorld(screen));
        if (target is not null)
        {
            _ = ViewModel.TryConnect(source, sourcePort, target);
            return;
        }

        var conditionPort = sourcePort is "EnterCondition" or "Condition" or "ExitCondition";
        var world = ScreenToWorld(screen);
        if (IsMouseCaptured) ReleaseMouseCapture();
        var definition = ViewModel.Owner.SelectTask(
            conditionPort ? "Select Condition Node" : "Select Behavior Tree Child Node",
            conditionPort ? "Select a Condition type to connect to the current condition port." : "Select a node type to create and connect to the current task port.",
            task => conditionPort
                ? task.HasTag(TaskContractConstants.TagCondition)
                : !task.HasTag(TaskContractConstants.TagCondition) &&
                  !task.HasTag(TaskContractConstants.TagTimeline) &&
                  task.TypeName != "TaskBtRoot");
        if (definition is null) return;

        var targetIsCondition = definition.HasTag(TaskContractConstants.TagCondition);
        if (conditionPort != targetIsCondition || definition.TypeName == "TaskBtRoot")
        {
            ViewModel.Owner.SetStatus(conditionPort ? "A Condition task is required for a condition port." : "A non-Condition, non-Root task is required for a task port.", true);
            return;
        }

        var created = ViewModel.AddTask(definition, new EditorPoint(world.X, world.Y));
        if (!ViewModel.TryConnect(source, sourcePort, created)) ViewModel.RemoveNode(created);
    }

    private void ClearConnectionDrag()
    {
        _connectionDragNode = null;
        _connectionDragPort = null;
        Cursor = Cursors.Arrow;
        if (!_panning && IsMouseCaptured) ReleaseMouseCapture();
        InvalidateVisual();
    }

    private void OnKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Escape || _connectionDragNode is null) return;
        ClearConnectionDrag();
        args.Handled = true;
    }

    private void OnLostMouseCapture(object sender, MouseEventArgs args)
    {
        if (Mouse.LeftButton == MouseButtonState.Pressed) return;
        _dragNode = null;
        _panning = false;
        _connectionDragNode = null;
        _connectionDragPort = null;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    private void OnMouseLeave(object sender, MouseEventArgs args)
    {
        if (_dragNode is not null || _connectionDragNode is not null || _panning) return;
        _hoverNode = null;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    private Point WorldToScreen(Point point) => new(point.X * _zoom + _pan.X, point.Y * _zoom + _pan.Y);
    private Point ScreenToWorld(Point point) => new((point.X - _pan.X) / _zoom, (point.Y - _pan.Y) / _zoom);

    private FormattedText CreateText(string text, double size, Brush brush) => new(
        text, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
        new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private static void OnViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var canvas = (BehaviorTreeCanvas)sender;
        if (args.OldValue is BehaviorTreeDocumentViewModel oldViewModel) canvas.Unsubscribe(oldViewModel);
        if (args.NewValue is BehaviorTreeDocumentViewModel newViewModel) canvas.Subscribe(newViewModel);
        canvas.InvalidateVisual();
    }

    private void Subscribe(BehaviorTreeDocumentViewModel viewModel)
    {
        viewModel.Nodes.CollectionChanged += OnCollectionChanged;
        viewModel.Edges.CollectionChanged += OnCollectionChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        foreach (var node in viewModel.Nodes) node.PropertyChanged += OnNodePropertyChanged;
    }

    private void Unsubscribe(BehaviorTreeDocumentViewModel viewModel)
    {
        viewModel.Nodes.CollectionChanged -= OnCollectionChanged;
        viewModel.Edges.CollectionChanged -= OnCollectionChanged;
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        foreach (var node in viewModel.Nodes) node.PropertyChanged -= OnNodePropertyChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems is not null) foreach (var item in args.OldItems.OfType<BehaviorNode>()) item.PropertyChanged -= OnNodePropertyChanged;
        if (args.NewItems is not null) foreach (var item in args.NewItems.OfType<BehaviorNode>()) item.PropertyChanged += OnNodePropertyChanged;
        InvalidateVisual();
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs args) => InvalidateVisual();
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args) => InvalidateVisual();
}
