using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;

namespace BbxEditor.Wpf.Views;

public sealed class TimelineOverviewControl : FrameworkElement
{
    private const double HeaderHeight = 30;
    private const double NodeColumnWidth = 240;
    private TimelineItem? _dragItem;
    private TimelineItem? _hoverItem;
    private TimelineDragMode _dragMode;
    private double _dragScaleMax = 1;
    private double _dragOffsetTime;

    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
        nameof(Document), typeof(TimelineDocument), typeof(TimelineOverviewControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnDocumentChanged));
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(TimelineItem), typeof(TimelineOverviewControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty CatalogProperty = DependencyProperty.Register(
        nameof(Catalog), typeof(TaskCatalog), typeof(TimelineOverviewControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public TimelineOverviewControl()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    public TimelineDocument? Document
    {
        get => (TimelineDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public TimelineItem? SelectedItem
    {
        get => (TimelineItem?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public TaskCatalog? Catalog
    {
        get => (TaskCatalog?)GetValue(CatalogProperty);
        set => SetValue(CatalogProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var background = new SolidColorBrush(Color.FromRgb(45, 48, 53));
        var header = new SolidColorBrush(Color.FromRgb(51, 55, 60));
        var alternatingRow = new SolidColorBrush(Color.FromRgb(48, 51, 56));
        var selectedNode = new SolidColorBrush(Color.FromRgb(62, 74, 85));
        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(72, 77, 84)), 1);
        var dividerPen = new Pen(new SolidColorBrush(Color.FromRgb(91, 97, 105)), 1);
        var primaryText = new SolidColorBrush(Color.FromRgb(237, 240, 243));
        var secondaryText = new SolidColorBrush(Color.FromRgb(182, 187, 194));

        drawingContext.DrawRectangle(background, null, new Rect(RenderSize));
        drawingContext.DrawRectangle(header, null,
            new Rect(0, 0, RenderSize.Width, Math.Min(HeaderHeight, RenderSize.Height)));
        drawingContext.DrawLine(dividerPen, new Point(NodeColumnWidth, 0), new Point(NodeColumnWidth, RenderSize.Height));
        drawingContext.DrawText(CreateText("NODE", 10, secondaryText), new Point(12, 8));

        if (Document is null || RenderSize.Width <= NodeColumnWidth + 1 || RenderSize.Height <= HeaderHeight) return;
        var max = CalculateMax(Document);
        var rowHeight = CalculateRowHeight(Document);
        var axisWidth = Math.Max(1, RenderSize.Width - NodeColumnWidth);

        for (var tick = 0; tick <= 5; tick++)
        {
            var x = NodeColumnWidth + tick * axisWidth / 5;
            drawingContext.DrawLine(gridPen, new Point(x, HeaderHeight), new Point(x, RenderSize.Height));
            var text = CreateText((max * tick / 5).ToString("0.##"), 10, secondaryText);
            drawingContext.DrawText(text, new Point(Math.Min(x + 5, RenderSize.Width - text.Width - 5), 8));
        }

        for (var index = 0; index < Document.Items.Count; index++)
        {
            var item = Document.Items[index];
            var rowTop = HeaderHeight + index * rowHeight;
            if (rowTop >= RenderSize.Height) break;
            if (index % 2 == 1)
            {
                drawingContext.DrawRectangle(alternatingRow, null,
                    new Rect(0, rowTop, RenderSize.Width, Math.Min(rowHeight, RenderSize.Height - rowTop)));
            }

            var isHovered = ReferenceEquals(item, _hoverItem);
            var isSelected = ReferenceEquals(item, SelectedItem);
            if (isSelected)
            {
                drawingContext.DrawRectangle(selectedNode, null,
                    new Rect(0, rowTop, NodeColumnWidth, Math.Min(rowHeight, RenderSize.Height - rowTop)));
            }
            else if (isHovered)
            {
                drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(56, 60, 66)), null,
                    new Rect(0, rowTop, NodeColumnWidth, Math.Min(rowHeight, RenderSize.Height - rowTop)));
            }

            drawingContext.DrawLine(gridPen, new Point(0, rowTop + rowHeight), new Point(RenderSize.Width, rowTop + rowHeight));
            DrawNodeLabel(drawingContext, item, rowTop, rowHeight, primaryText, secondaryText);

            var (start, end) = GetBarBounds(item, max);
            var color = item.Duration < 0
                ? (isHovered ? Color.FromRgb(185, 128, 132) : Color.FromRgb(166, 111, 114))
                : (isHovered ? Color.FromRgb(133, 154, 172) : Color.FromRgb(113, 135, 154));
            var rect = new Rect(start, rowTop + 7, Math.Max(5, end - start), Math.Max(8, rowHeight - 14));
            var borderBrush = new SolidColorBrush(isSelected ? Color.FromRgb(218, 224, 229) : Color.FromRgb(87, 96, 105));
            drawingContext.DrawRoundedRectangle(new SolidColorBrush(color), new Pen(borderBrush, isSelected ? 2 : 1), rect, 5, 5);
            var handleBrush = new SolidColorBrush(Color.FromArgb(isHovered || isSelected ? (byte)230 : (byte)155, 237, 240, 243));
            drawingContext.DrawRoundedRectangle(handleBrush, null,
                new Rect(Math.Max(start + 1, end - 7), rect.Top + 3, 3, Math.Max(3, rect.Height - 6)), 1.5, 1.5);
        }
    }

    private void DrawNodeLabel(DrawingContext context, TimelineItem item, double rowTop, double rowHeight, Brush primary, Brush secondary)
    {
        var definition = Catalog?.FindTask(item.Task.TaskType);
        var displayName = definition?.DisplayName ?? item.Task.TaskType;
        var title = CreateText(displayName, 15, primary, NodeColumnWidth - 24);
        var type = CreateText(item.Task.TaskType, 12, secondary, NodeColumnWidth - 24);
        var titleY = rowTop + Math.Max(5, (rowHeight - title.Height - type.Height - 2) / 2);
        context.DrawText(title, new Point(12, titleY));
        context.DrawText(type, new Point(12, titleY + title.Height + 2));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs args)
    {
        base.OnMouseLeftButtonDown(args);
        Focus();
        var position = args.GetPosition(this);
        var rowItem = HitTestRow(position);
        if (rowItem is null) return;

        SelectedItem = rowItem;
        if (position.X < NodeColumnWidth)
        {
            Cursor = Cursors.Hand;
            args.Handled = true;
            InvalidateVisual();
            return;
        }

        var hit = HitTestBar(position);
        if (hit is null)
        {
            args.Handled = true;
            return;
        }

        var (item, _, end) = hit.Value;
        _dragItem = item;
        _dragScaleMax = CalculateMax(Document!);
        _dragMode = Math.Abs(position.X - end) <= 9 ? TimelineDragMode.Resize : TimelineDragMode.Move;
        _dragOffsetTime = _dragMode == TimelineDragMode.Move ? ToTime(position.X, _dragScaleMax) - item.StartTime : 0;
        Cursor = _dragMode == TimelineDragMode.Resize ? Cursors.SizeWE : Cursors.SizeAll;
        CaptureMouse();
        args.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs args)
    {
        base.OnMouseMove(args);
        var position = args.GetPosition(this);
        if (_dragItem is not null && IsMouseCaptured)
        {
            var time = ToTime(position.X, _dragScaleMax);
            if (_dragMode == TimelineDragMode.Move)
            {
                _dragItem.StartTime = Math.Max(0, time - _dragOffsetTime);
            }
            else if (_dragMode == TimelineDragMode.Resize)
            {
                _dragItem.Duration = Math.Max(0, time - _dragItem.StartTime);
            }
            args.Handled = true;
            return;
        }

        var rowItem = HitTestRow(position);
        var hit = HitTestBar(position);
        var hoverItem = position.X < NodeColumnWidth ? rowItem : hit?.Item;
        if (!ReferenceEquals(_hoverItem, hoverItem))
        {
            _hoverItem = hoverItem;
            InvalidateVisual();
        }
        Cursor = hit is not null && Math.Abs(position.X - hit.Value.End) <= 9
            ? Cursors.SizeWE
            : hit is not null ? Cursors.SizeAll
            : position.X < NodeColumnWidth && rowItem is not null ? Cursors.Hand
            : Cursors.Arrow;
    }

    protected override void OnMouseLeave(MouseEventArgs args)
    {
        base.OnMouseLeave(args);
        if (_dragItem is not null) return;
        _hoverItem = null;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs args)
    {
        base.OnMouseLeftButtonUp(args);
        EndDrag();
    }

    protected override void OnLostMouseCapture(MouseEventArgs args)
    {
        base.OnLostMouseCapture(args);
        _dragItem = null;
        _dragMode = TimelineDragMode.None;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    private void EndDrag()
    {
        if (_dragItem is null) return;
        _dragItem = null;
        _dragMode = TimelineDragMode.None;
        if (IsMouseCaptured) ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    private TimelineItem? HitTestRow(Point position)
    {
        if (Document is null || Document.Items.Count == 0 || position.Y < HeaderHeight) return null;
        var rowHeight = CalculateRowHeight(Document);
        var index = (int)((position.Y - HeaderHeight) / rowHeight);
        return index >= 0 && index < Document.Items.Count ? Document.Items[index] : null;
    }

    private (TimelineItem Item, double Start, double End)? HitTestBar(Point position)
    {
        var item = HitTestRow(position);
        if (item is null || position.X < NodeColumnWidth) return null;
        var (start, end) = GetBarBounds(item, CalculateMax(Document!));
        return position.X >= start - 3 && position.X <= end + 3 ? (item, start, end) : null;
    }

    private (double Start, double End) GetBarBounds(TimelineItem item, double max)
    {
        var axisWidth = Math.Max(1, RenderSize.Width - NodeColumnWidth);
        var start = NodeColumnWidth + Math.Clamp(item.StartTime / max * axisWidth, 0, axisWidth);
        var minimumEnd = Math.Min(axisWidth, start - NodeColumnWidth + 4);
        var end = item.Duration < 0
            ? RenderSize.Width
            : NodeColumnWidth + Math.Clamp((item.StartTime + item.Duration) / max * axisWidth, minimumEnd, axisWidth);
        return (start, end);
    }

    private double ToTime(double x, double max)
    {
        var axisWidth = Math.Max(1, RenderSize.Width - NodeColumnWidth);
        return Math.Max(0, (x - NodeColumnWidth) / axisWidth * max);
    }

    private static double CalculateMax(TimelineDocument document)
    {
        var max = document.Items.Select(item => item.StartTime + (item.Duration < 0 ? 1 : item.Duration)).DefaultIfEmpty(1).Max();
        return max <= 0 ? 1 : max;
    }

    private double CalculateRowHeight(TimelineDocument document) =>
        Math.Clamp((RenderSize.Height - HeaderHeight) / Math.Max(1, document.Items.Count), 22, 54);

    private FormattedText CreateText(string text, double size, Brush brush, double? maxWidth = null)
    {
        var formatted = new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        if (maxWidth is > 0) formatted.MaxTextWidth = maxWidth.Value;
        formatted.Trimming = TextTrimming.CharacterEllipsis;
        return formatted;
    }

    private static void OnDocumentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var control = (TimelineOverviewControl)sender;
        if (args.OldValue is TimelineDocument oldDocument) control.Unsubscribe(oldDocument);
        if (args.NewValue is TimelineDocument newDocument) control.Subscribe(newDocument);
        control.InvalidateVisual();
    }

    private void Subscribe(TimelineDocument document)
    {
        document.Items.CollectionChanged += OnCollectionChanged;
        foreach (var item in document.Items) item.PropertyChanged += OnItemChanged;
    }

    private void Unsubscribe(TimelineDocument document)
    {
        document.Items.CollectionChanged -= OnCollectionChanged;
        foreach (var item in document.Items) item.PropertyChanged -= OnItemChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems is not null) foreach (TimelineItem item in args.OldItems) item.PropertyChanged -= OnItemChanged;
        if (args.NewItems is not null) foreach (TimelineItem item in args.NewItems) item.PropertyChanged += OnItemChanged;
        InvalidateVisual();
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs args) => InvalidateVisual();

    private enum TimelineDragMode { None, Move, Resize }
}
