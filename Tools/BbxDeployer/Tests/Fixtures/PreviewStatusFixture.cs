using BbxDeployer.Core;

namespace BbxDeployer.Tests;

internal sealed class PreviewStatusFixture
{
    private static readonly DateTime SourceTimestampUtc =
        new(2025, 1, 15, 8, 30, 0, DateTimeKind.Utc);

    public PreviewStatusFixture(TestWorkspace workspace)
    {
        Source = workspace.CreateRepository("PreviewSource", "MainGame");
        NewProject = workspace.CreateRepository("NewDestination", "BrandNewGame");
        WaitForSync = workspace.CreateRepository("WaitingDestination", "WaitingGame");
        Synchronized = workspace.CreateRepository("SynchronizedDestination", "CurrentGame");
        Warning = workspace.CreateRepository("WarningDestination", "NewerGame");
        Items = CreateItems();

        CreateSourceData();
        CreateNewProjectData();
        CreateWaitForSyncData();
        CreateSynchronizedData();
        CreateWarningData();
    }

    public ProjectContext Source { get; }

    public ProjectContext NewProject { get; }

    public ProjectContext WaitForSync { get; }

    public ProjectContext Synchronized { get; }

    public ProjectContext Warning { get; }

    public IReadOnlyList<SyncItem> Items { get; }

    private static IReadOnlyList<SyncItem> CreateItems()
    {
        return
        [
            CreateItem("shared-a", "Shared A", "SharedA"),
            CreateItem("shared-b", "Shared B", "SharedB"),
            CreateItem("shared-c", "Shared C", "SharedC")
        ];
    }

    private static SyncItem CreateItem(string id, string displayName, string path)
    {
        return new SyncItem
        {
            Id = id,
            DisplayName = displayName,
            SourceBase = PathBaseKind.RepositoryRoot,
            SourceRelativePath = path,
            TargetBase = PathBaseKind.RepositoryRoot,
            TargetRelativePath = path
        };
    }

    private void CreateSourceData()
    {
        WriteFile(Source, "SharedA/a.txt", "alpha", SourceTimestampUtc);
        WriteFile(Source, "SharedB/b.txt", "bravo", SourceTimestampUtc);
        WriteFile(Source, "SharedC/c.txt", "charlie", SourceTimestampUtc);
    }

    private void CreateNewProjectData()
    {
        WriteFile(NewProject, "SharedA/a.txt", "old", SourceTimestampUtc.AddDays(-1));
    }

    private void CreateWaitForSyncData()
    {
        WriteFile(WaitForSync, "SharedA/a.txt", "old", SourceTimestampUtc.AddDays(-1));
        WriteFile(WaitForSync, "SharedB/b.txt", "bravo", SourceTimestampUtc);
    }

    private void CreateSynchronizedData()
    {
        WriteFile(Synchronized, "SharedA/a.txt", "alpha", SourceTimestampUtc);
        WriteFile(Synchronized, "SharedB/b.txt", "bravo", SourceTimestampUtc);
        WriteFile(Synchronized, "SharedC/c.txt", "charlie", SourceTimestampUtc);
    }

    private void CreateWarningData()
    {
        WriteFile(Warning, "SharedA/a.txt", "alpha", SourceTimestampUtc);
        WriteFile(Warning, "SharedB/b.txt", "target edit", SourceTimestampUtc.AddMinutes(10));
        WriteFile(Warning, "SharedC/c.txt", "charlie", SourceTimestampUtc);
    }

    private void WriteFile(
        ProjectContext project,
        string relativePath,
        string contents,
        DateTime timestampUtc)
    {
        var path = Path.Combine(
            project.RepositoryRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        File.SetLastWriteTimeUtc(path, timestampUtc);
    }
}
