namespace BbxEditor.Wpf.Presentation;

public static class DesignPlanMetadataPresentation
{
    public static string? GetPriorityColor(string? priority) => priority switch
    {
        "P0" => "#D9534F",
        "P1" => "#F39C4A",
        "P2" => "#E3C34A",
        _ => null,
    };

    public static string? GetStateColor(string? state) => state switch
    {
        "In Design" => "#858B94",
        "Todo" => "#4F8FD7",
        "In Progress" => "#D0B27C",
        "Warning" => "#E3C34A",
        "Completed" => "#5FA66F",
        _ => null,
    };
}
