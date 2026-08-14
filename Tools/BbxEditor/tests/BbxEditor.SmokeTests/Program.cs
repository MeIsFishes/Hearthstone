using System.Text;
using System.Text.Json;
using BbxEditor.Application;
using BbxEditor.Contracts;
using BbxEditor.Diagnostics;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;
using BbxEditor.Wpf.Services;
using BbxEditor.Wpf.Presentation;
using BbxEditor.Wpf.ViewModels;
using BbxEditor.Wpf.Views;

var tempRoot = Path.Combine(Path.GetTempPath(), "BbxEditor.SmokeTests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempRoot);
try
{
    TestTaskDocuments(tempRoot);
    TestTaskMetadataDiscovery(tempRoot);
    TestApplicationLog();
    TestCsvDocuments(tempRoot);
    TestCsvAssociationTargets();
    TestMetadataCatalog(tempRoot);
    TestScriptableObjectDocuments(tempRoot);
    TestSettings(tempRoot);
    TestDesignPlanIndex(tempRoot);
    TestDesignPlanSearchAndPresentation();
    TestMarkdownRendering(tempRoot);
    TestVectorSearchInfrastructure(tempRoot);
    TestTaskSelectionVectorOrdering();
    TestCsvColumnSearchOrdering();
    TestInspectorStrategySelection();
    TestExplorerCurrentDocumentSelection();
    TestMainWindowShortcuts();
    TestWorkspaceChromeTheme();
    TestBehaviorTreeConnectionRouting();
    TestBehaviorTreeNodeSearch();
    await TestOpenDocumentFileWatchAsync(tempRoot);
    await TestDesignPlanDirectoryWatchAsync(tempRoot);
    await TestProjectFileIndexAsync(tempRoot);
    await TestProductionVectorSearchAsync(tempRoot);
    if (args.Length >= 2) TestExistingCsvEditorDocument(args[0], args[1]);
    if (args.Length >= 3) await TestExistingProjectIndexAsync(args[0], args[2]);
    Console.WriteLine("ALL SMOKE TESTS PASSED");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine("SMOKE TEST FAILED");
    Console.Error.WriteLine(exception);
    return 1;
}
finally
{
    if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
}

static void TestDesignPlanIndex(string root)
{
    var projectRoot = Path.Combine(root, "DesignPlanProject");
    var newestDate = Path.Combine(projectRoot, "AutoDoc", "DesignPlan", "2026.08.10");
    var olderDate = Path.Combine(projectRoot, "AutoDoc", "DesignPlan", "2026.08.09");
    var legacyMonth = Path.Combine(projectRoot, "AutoDoc", "DesignPlan", "2026.08");
    var invalidDate = Path.Combine(projectRoot, "AutoDoc", "DesignPlan", "2026.02.30");
    Directory.CreateDirectory(newestDate);
    Directory.CreateDirectory(olderDate);
    Directory.CreateDirectory(legacyMonth);
    Directory.CreateDirectory(invalidDate);
    Directory.CreateDirectory(Path.Combine(newestDate, "nested"));
    var planDirectory = Path.Combine(projectRoot, "AutoDoc", "DesignPlan", "Plan");
    var reviewDirectory = Path.Combine(projectRoot, "AutoDoc", "DesignPlan", "Review");
    Directory.CreateDirectory(planDirectory);
    Directory.CreateDirectory(reviewDirectory);
    var associatedPlan = Path.Combine(planDirectory, "design-p0-plan.md");
    var associatedReview = Path.Combine(reviewDirectory, "design-p0-review.md");
    File.WriteAllText(associatedPlan, "# Plan");
    File.WriteAllText(associatedReview, "# Review");

    WriteDesignPlan("todo-p1.md", "Zulu Todo P1", "Todo", "P1");
    WriteDesignPlan("todo-p0.md", "Zulu Todo P0", "Todo", "P0", "## Design");
    WriteDesignPlan("in-progress-p1.md", "Beta In Progress P1", "In Progress", "P1");
    WriteDesignPlan("design-p2.md", "Alpha In Design P2", "In Design", "P2");
    WriteDesignPlan("design-p0.md", "Alpha In Design P0", "In Design", "P0", "## Linked design", true);
    WriteDesignPlan("warning-p2.md", "Beta Warning P2", "Warning", "P2");
    WriteDesignPlan("completed-p2.md", "Alpha Completed P2", "Completed", "P2");
    WriteDesignPlan("completed-p0.md", "Alpha Completed P0", "Completed", "P0");
    File.WriteAllText(Path.Combine(newestDate, "fallback-title.md"), "# Missing strict header");
    File.WriteAllText(Path.Combine(newestDate, "ignored.txt"), "not markdown");
    File.WriteAllText(Path.Combine(newestDate, "nested", "ignored.md"), "title: Nested");
    File.WriteAllText(Path.Combine(olderDate, "old-plan.md"),
        "title: Older Plan\nstate: Completed\npriority: P2");
    File.WriteAllText(Path.Combine(legacyMonth, "ignored.md"), "title: Legacy month");
    File.WriteAllText(Path.Combine(invalidDate, "ignored.md"), "title: Invalid date");

    var dates = DesignPlanIndexService.Scan(projectRoot);
    Assert(dates.Count == 2 && dates[0].Date == "2026.08.10" && dates[1].Date == "2026.08.09",
        "Design plans were not grouped into valid YYYY.MM.DD folders in newest-first order.");
    Assert(dates[0].Documents.Count == 9 && dates[0].Documents.All(document => document.RelativePath.StartsWith("2026.08.10/", StringComparison.Ordinal)),
        "Design plan scanning did not stay within direct Markdown children of the date folder.");
    Assert(dates[0].Documents.Select(document => document.FileName).SequenceEqual([
            "todo-p0.md", "todo-p1.md", "in-progress-p1.md", "design-p0.md", "design-p2.md",
            "warning-p2.md", "completed-p0.md", "completed-p2.md", "fallback-title.md"
        ]),
        "Same-date design plans were not ordered by Todo, In Progress, In Design, Warning, Completed and then P0, P1, P2.");
    var indexed = dates[0].Documents.Single(document => document.FileName == "todo-p0.md");
    Assert(indexed.Title == "Zulu Todo P0" && indexed.State == "Todo" && indexed.Priority == "P0",
        "Design plan strict header metadata was not indexed.");
    Assert(dates[0].Documents.Single(document => document.FileName == "fallback-title.md").Title == "fallback-title",
        "A malformed design plan header did not fall back to the file name.");
    var content = DesignPlanIndexService.LoadContent(indexed.FullPath);
    Assert(content.Title == "Zulu Todo P0" && content.State == "Todo" && content.Priority == "P0" &&
           content.PlanPath is null && content.ReviewPath is null &&
           content.MarkdownBody == "## Design" && !content.MarkdownBody.Contains("title:", StringComparison.Ordinal),
        "The strict design plan header was not removed from the rendered Markdown body.");
    var associatedContent = DesignPlanIndexService.LoadContent(Path.Combine(newestDate, "design-p0.md"));
    Assert(associatedContent.PlanPath == associatedPlan && associatedContent.ReviewPath == associatedReview &&
           associatedContent.MarkdownBody == "## Linked design" &&
           !associatedContent.MarkdownBody.Contains("plan:", StringComparison.Ordinal) &&
           !associatedContent.MarkdownBody.Contains("review:", StringComparison.Ordinal),
        "Plan and Review associations were not resolved from the game project root or removed from the rendered body.");
    Assert(DesignPlanIndexService.ResolveAssociatedDocumentPath(
               Path.Combine(newestDate, "design-p0.md"), "https://example.com/plan.md") is null &&
           DesignPlanIndexService.ResolveAssociatedDocumentPath(
               Path.Combine(newestDate, "design-p0.md"), "invalid\0path.md") is null,
        "Unsupported or invalid associated document paths should not be exposed as local document buttons.");
    var linkedDocument = dates[0].Documents.Single(document => document.FileName == "design-p0.md");
    var linkedUri = new UriBuilder(new Uri(linkedDocument.FullPath)) { Fragment = "details" }.Uri;
    Assert(DesignPlanIndexService.FindLinkedDocument(dates.SelectMany(date => date.Documents), linkedUri) == linkedDocument,
        "A local Markdown link did not resolve to the corresponding indexed design plan.");
    Assert(DesignPlanIndexService.FindLinkedDocument(dates.SelectMany(date => date.Documents), new Uri("https://example.com/plan.md")) is null &&
           DesignPlanIndexService.FindLinkedDocument(dates.SelectMany(date => date.Documents), new Uri(Path.Combine(newestDate, "missing.md"))) is null,
        "External or unindexed document links should not resolve as indexed design plans.");

    void WriteDesignPlan(string fileName, string title, string state, string priority, string body = "", bool withAssociations = false) =>
        File.WriteAllText(Path.Combine(newestDate, fileName),
            $"title: {title}\nstate: {state}\npriority: {priority}\n" +
            (withAssociations
                ? "plan: AutoDoc/DesignPlan/Plan/design-p0-plan.md\nreview: AutoDoc/DesignPlan/Review/design-p0-review.md\n"
                : string.Empty) +
            $"\n{body}");
}

static void TestDesignPlanSearchAndPresentation()
{
    var titleMatch = new IndexedDesignPlanDocument(
        @"C:\Game\AutoDoc\DesignPlan\2026.08.10\unrelated-name.md", "2026.08.10/unrelated-name.md", "2026.08.10",
        "unrelated-name.md", "Target Alpha", "In Design", "P0");
    var fileMatch = new IndexedDesignPlanDocument(
        @"C:\Game\AutoDoc\DesignPlan\2026.08.10\target-alpha.md", "2026.08.10/target-alpha.md", "2026.08.10",
        "target-alpha.md", "Unrelated Title", "Todo", "P1");
    var unrelated = new IndexedDesignPlanDocument(
        @"C:\Game\AutoDoc\DesignPlan\2026.08.10\other.md", "2026.08.10/other.md", "2026.08.10",
        "other.md", "Other Plan", "Completed", "P2");
    var matches = DesignPlanSearchService.FindLiteralMatches([fileMatch, unrelated, titleMatch], "target alpha");
    Assert(matches.SequenceEqual([titleMatch, fileMatch]),
        "Design plan literal search did not prioritize title matches before file-name matches.");
    var semanticCandidates = Enumerable.Range(0, DesignPlanSearchService.DefaultMaxSemanticMatches + 2)
        .Select(index => new IndexedDesignPlanDocument(
            $@"C:\Game\AutoDoc\DesignPlan\2026.08.10\semantic-{index}.md",
            $"2026.08.10/semantic-{index}.md",
            "2026.08.10",
            $"semantic-{index}.md",
            $"Semantic {index}",
            "In Design",
            "P1"))
        .ToArray();
    var mergedMatches = DesignPlanSearchService.MergeVectorMatches(
        semanticCandidates,
        [],
        semanticCandidates.Reverse().Select(DesignPlanSearchService.GetVectorName).ToArray());
    Assert(mergedMatches.Count == DesignPlanSearchService.DefaultMaxSemanticMatches &&
           mergedMatches.SequenceEqual(semanticCandidates.Reverse().Take(DesignPlanSearchService.DefaultMaxSemanticMatches)),
        "Design plan semantic search ignored search rank or restored the entire unfiltered tree.");
    var explorer = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BbxEditor.Wpf", "Views", "ExplorerControl.xaml"));
    Assert(explorer.Contains("ItemsSource=\"{Binding DesignPlanSearchResults}\"", StringComparison.Ordinal) &&
           explorer.Contains("Binding=\"{Binding IsDesignPlanSearchActive}\"", StringComparison.Ordinal),
        "Active design plan search does not switch from the date tree to a flat search-ranked result list.");
    var viewer = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BbxEditor.Wpf", "Views", "DesignPlanViewerControl.xaml"));
    Assert(viewer.Contains("x:Name=\"AssociatedDocumentsBar\"", StringComparison.Ordinal) &&
           viewer.Contains("x:Key=\"AssociatedDocumentButtonStyle\"", StringComparison.Ordinal) &&
           viewer.Contains("Background\" Value=\"#355A78\"", StringComparison.Ordinal) &&
           viewer.Contains("Content=\"Open Plan\"", StringComparison.Ordinal) &&
           viewer.Contains("Content=\"Open Review\"", StringComparison.Ordinal) &&
           viewer.Split("Style=\"{StaticResource AssociatedDocumentButtonStyle}\"", StringSplitOptions.None).Length == 3 &&
           viewer.IndexOf("AssociatedDocumentsBar", StringComparison.Ordinal) < viewer.IndexOf("x:Name=\"Browser\"", StringComparison.Ordinal),
        "The design plan viewer does not place both dark-blue Plan and Review buttons above the Markdown browser.");
    var viewerCode = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BbxEditor.Wpf", "Views", "DesignPlanViewerControl.xaml.cs"));
    var mainViewModel = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "BbxEditor.Wpf", "ViewModels", "MainViewModel.cs"));
    Assert(viewerCode.Contains("$\"{documentType}: {viewModel.DesignPlan.Title}\"", StringComparison.Ordinal) &&
           mainViewModel.Contains("internal void OpenAssociatedDesignPlan(string path, string tabTitle)", StringComparison.Ordinal) &&
           mainViewModel.Contains("OpenDesignPlan(path, false, false, tabTitle);", StringComparison.Ordinal) &&
           mainViewModel.Contains("TabTitleOverride = tabTitleOverride", StringComparison.Ordinal),
        "Associated Plan and Review buttons do not route unindexed Markdown files into named fixed editor tabs.");
    Assert(DesignPlanSearchService.GetVectorName(titleMatch).Equals("Target Alpha", StringComparison.OrdinalIgnoreCase),
        "Design plan vector indexing did not use the title as its semantic name.");

    var projectFile = new IndexedProjectFile(@"C:\Game\Data\Ship.csv", "Data/Ship.csv", ProjectFileKind.Csv, "Native");
    var withDesignPlans = DesignPlanSearchService.BuildVectorCorpus([projectFile], ["Game.TaskAttack"], [titleMatch]);
    var withoutDesignPlans = DesignPlanSearchService.BuildVectorCorpus([projectFile], ["Game.TaskAttack"], []);
    Assert(withDesignPlans.Contains("Target Alpha", StringComparer.OrdinalIgnoreCase) &&
           !withoutDesignPlans.Contains("Target Alpha", StringComparer.OrdinalIgnoreCase),
        "Design plan vector names were not added to and removed from the shared Explorer corpus.");

    Assert(DesignPlanMetadataPresentation.GetPriorityColor("P0") == "#D9534F" &&
           DesignPlanMetadataPresentation.GetPriorityColor("P1") == "#F39C4A" &&
           DesignPlanMetadataPresentation.GetPriorityColor("P2") == "#E3C34A" &&
           DesignPlanMetadataPresentation.GetStateColor("In Design") == "#858B94" &&
           DesignPlanMetadataPresentation.GetStateColor("Todo") == "#4F8FD7" &&
           DesignPlanMetadataPresentation.GetStateColor("In Progress") == "#D0B27C" &&
           DesignPlanMetadataPresentation.GetStateColor("Warning") == "#E3C34A" &&
           DesignPlanMetadataPresentation.GetStateColor("Completed") == "#5FA66F",
        "Design plan priority/state badge colors do not match the required mapping.");
}

static void TestMarkdownRendering(string root)
{
    var documentDirectory = Path.Combine(root, "MarkdownProject", "AutoDoc", "DesignPlan", "2026.08.10");
    var imagePath = Path.Combine(root, "MarkdownProject", "AutoDoc", "media", "design-plan", "prototype image.png");
    Directory.CreateDirectory(documentDirectory);
    Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);
    File.WriteAllBytes(imagePath, [0x89, 0x50, 0x4E, 0x47]);
    var documentPath = Path.Combine(documentDirectory, "viewer-test.md");
    var relativeImage = "../../media/design-plan/prototype%20image.png";
    var markdown = $"""
        # Viewer Test

        | Feature | Result |
        | --- | --- |
        | Table | **Rendered** |

        - [x] Task item
        - ~~Removed item~~

        [Related plan](related-plan.md#details)

        ![Prototype]({relativeImage})
        """;

    var resolvedImage = MarkdownRenderService.ResolveImageUrl(documentPath, relativeImage);
    Assert(resolvedImage == new Uri(imagePath).AbsoluteUri,
        "Relative Markdown image paths were not resolved against the document directory.");
    Assert(MarkdownRenderService.ResolveImageUrl(documentPath, "https://example.com/image.png") == "https://example.com/image.png",
        "Remote Markdown image URLs should remain unchanged.");
    Assert(MarkdownRenderService.ResolveImageUrl(documentPath, imagePath) == new Uri(imagePath).AbsoluteUri,
        "Absolute local Markdown image paths were not converted to file URIs.");
    var internalLink = MarkdownRenderService.ResolveDocumentLinkUrl(documentPath, "related-plan.md#details");
    var expectedDocumentUri = new UriBuilder(new Uri(Path.Combine(documentDirectory, "related-plan.md")))
        { Fragment = "details" }.Uri;
    Uri? decodedUri = null;
    var decodedSuccess = Uri.TryCreate(internalLink, UriKind.Absolute, out var internalUri) &&
                         MarkdownRenderService.TryDecodeDesignPlanLink(internalUri, out decodedUri);
    Assert(decodedSuccess && decodedUri == expectedDocumentUri,
        "A relative Markdown document link was not encoded and decoded through the internal navigation URI.");
    Assert(MarkdownRenderService.ResolveDocumentLinkUrl(documentPath, "https://example.com/plan.md") == "https://example.com/plan.md",
        "Remote Markdown document URLs should remain unchanged.");

    var html = MarkdownRenderService.RenderDocument(markdown, documentPath);
    Assert(html.Contains("<table", StringComparison.Ordinal) &&
           html.Contains("type=\"checkbox\"", StringComparison.Ordinal) &&
           html.Contains("<del>", StringComparison.Ordinal) &&
           html.Contains("<img", StringComparison.Ordinal) &&
           html.Contains("https://bbx-design-plan.local/open?target=", StringComparison.Ordinal) &&
           html.Contains(new Uri(imagePath).AbsoluteUri, StringComparison.Ordinal) &&
           html.Contains("color-scheme: dark", StringComparison.Ordinal) &&
           html.Contains("background: #292c30", StringComparison.Ordinal),
        "The Markdown viewer did not render advanced Markdown, the linked local image, and the dark document theme into HTML.");
}

static void TestTaskMetadataDiscovery(string root)
{
    var metadataRoot = Path.Combine(root, "ExportedBbxEditorInfo");
    var documentDirectory = Path.Combine(metadataRoot, "Task");
    var taskMetadataDirectory = Path.Combine(root, "ExportedTaskInfo");
    Directory.CreateDirectory(documentDirectory);
    Directory.CreateDirectory(taskMetadataDirectory);
    File.WriteAllText(Path.Combine(documentDirectory, "Example.editor.json"), "{\"NodeEditDataDictionary\":{}}");
    File.WriteAllText(Path.Combine(documentDirectory, "Example.json"), "{\"TaskInfos\":{}}");
    File.WriteAllText(Path.Combine(taskMetadataDirectory, "TaskExample.json"), """
        {
          "Default.TypeInfo": { "FullType": "BbxCommon.Internal.TaskExportInfo" },
          "TaskTypeName": "TaskExample",
          "TaskFullTypeName": "Game.TaskExample",
          "Tags": {},
          "FieldInfos": {}
        }
        """);

    var resolved = TaskMetadataDirectoryResolver.Resolve(metadataRoot);
    Assert(Path.GetFullPath(resolved).Equals(Path.GetFullPath(taskMetadataDirectory), StringComparison.OrdinalIgnoreCase),
        "Task metadata discovery did not select the sibling ExportedTaskInfo directory.");
    var documentScan = TaskCatalog.LoadFromDirectory(documentDirectory);
    Assert(documentScan.Success && documentScan.Diagnostics.Count == 0,
        "Task editor/runtime documents produced metadata warnings.");
    var metadataScan = TaskCatalog.LoadFromDirectory(resolved);
    Assert(metadataScan.Success && metadataScan.Value!.Tasks.Count == 1,
        "Resolved Task metadata could not be loaded.");
    Console.WriteLine("PASS Task metadata directory discovery and task-document filtering");
}

static void TestApplicationLog()
{
    var log = new ApplicationLog();
    log.Add("Indexed project files.");
    log.Add(new Diagnostic(DiagnosticSeverity.Warning, "TEST_WARNING", "Warning message.", "warning.json"));
    log.Add(new Diagnostic(DiagnosticSeverity.Error, "TEST_ERROR", "Error message.", "error.json"));
    Assert(log.LogCount == 1 && log.WarningCount == 1 && log.ErrorCount == 1 && log.HasErrors &&
           log.SummaryText == "1 logs, 1 warnings, 1 errors" && log.Entries.Count == 3,
        "Application log severity counts or summary text were incorrect.");
    Console.WriteLine("PASS one-line application log severity summary");
}

static void TestTaskDocuments(string root)
{
    var context = new TaskContextDefinition("Game.TaskContextSmoke", []);
    var definition = new TaskDefinition("TaskSmoke", "Game.TaskSmoke", null, [TaskContractConstants.TagAction],
        [new TaskFieldDefinition("Enabled", new TaskTypeReference("bool"), null)]);
    var timelineDefinition = new TaskDefinition("TaskTimeline", "Game.TaskTimeline", null, [TaskContractConstants.TagTimeline],
        [new TaskFieldDefinition("Duration", new TaskTypeReference("float"), null)]);
    var catalog = new TaskCatalog([definition, timelineDefinition], [context], []);
    var document = new TimelineDocument { BindingContextType = context.TypeName };
    var task = TaskInstance.FromDefinition(definition);
    task.FindField("Enabled")!.Value = "true";
    document.Items.Add(new TimelineItem { Task = task, Duration = 1 });
    var service = new DocumentFileService();
    var path = Path.Combine(root, "Smoke.editor.json");
    var save = service.Save(document, catalog, path);
    Assert(save.Success, Join(save.Diagnostics.Select(item => item.Message)));
    Assert(File.Exists(path) && File.Exists(Path.Combine(root, "Smoke.json")), "Task editor/runtime pair was not written.");
    var reopened = service.Open(path, catalog);
    Assert(reopened.Value is TimelineDocument timeline && timeline.Items.Count == 1, "Task document dispatch or roundtrip failed.");
    Console.WriteLine("PASS Task document roundtrip");
}

static void TestCsvDocuments(string root)
{
    var metadata = new CsvTypeMetadata
    {
        TypeName = "ShipCsvData",
        FullTypeName = "Game.ShipCsvData",
        TableNames = ["ShipCsvData"],
        Columns =
        [
            new EditorFieldMetadata { Name = "Id", Required = true, Unique = true, Type = new EditorTypeMetadata { Kind = EditorValueKind.Int32 } },
            new EditorFieldMetadata { Name = "Name", Required = true, Type = new EditorTypeMetadata { Kind = EditorValueKind.String } },
            new EditorFieldMetadata { Name = "Enabled", Type = new EditorTypeMetadata { Kind = EditorValueKind.Boolean } },
            new EditorFieldMetadata { Name = "Tint", Type = new EditorTypeMetadata { Kind = EditorValueKind.Color } },
            new EditorFieldMetadata { Name = "Offset", Type = new EditorTypeMetadata { Kind = EditorValueKind.Vector2 } },
            new EditorFieldMetadata { Name = "Position", Type = new EditorTypeMetadata { Kind = EditorValueKind.Vector3 } },
            new EditorFieldMetadata { Name = "Weights", Type = new EditorTypeMetadata { Kind = EditorValueKind.Vector4 } },
            new EditorFieldMetadata { Name = "Blackboard", Type = new EditorTypeMetadata { Kind = EditorValueKind.TaskBlackboardInjection } },
        ],
    };
    var path = Path.Combine(root, "ShipCsvData.csv");
    File.WriteAllText(path,
        "Id,Name,Enabled,Tint,Offset,Position,Weights,Blackboard,Unknown\r\n" +
        "// Unique identifier,Display name,Enabled flag,Tint color,2D offset,3D position,4D weights,Task Blackboard injection,Unmapped value\r\n" +
        "// Associated: None\r\n" +
        "1,\"A, B\",true,#11223344,1.5;-2,1;2;3,1;2;3;4,\"Duration,int,8;Speed,double,1.2;Name,string,Missle\",Keep\r\n",
        new UTF8Encoding(true));
    var opened = CsvDocumentCodec.Open(path, metadata);
    var document = opened.Value ?? throw new InvalidOperationException(Join(opened.Diagnostics.Select(item => item.Message)));
    Assert(document.HasUtf8Bom && document.NewLine == "\r\n", "CSV encoding or newline style was not detected.");
    Assert(document.HeaderComments.Count == 2 && document.HeaderComments[1] == "// Associated: None",
        "CSV header contract comments were not parsed.");
    var descriptions = CsvDocumentCodec.GetFieldDescriptions(document);
    Assert(descriptions.Count == document.Columns.Count && descriptions[0] == "Unique identifier" &&
           descriptions[1] == "Display name" && descriptions[^1] == "Unmapped value",
        "The first CSV comment was not mapped to per-column field descriptions.");
    Assert(document.Rows[0].Cells[1].Value == "A, B", "Quoted CSV cell was not parsed.");
    document.Rows[0].Cells[1].Value = "Edited";
    var save = CsvDocumentCodec.Save(document, path);
    Assert(save.Success, Join(save.Diagnostics.Select(item => item.Message)));
    var reopened = CsvDocumentCodec.Open(path, metadata).Value!;
    Assert(reopened.Rows[0].Cells[1].Value == "Edited" && reopened.Rows[0].Cells[8].Value == "Keep" &&
           reopened.HeaderComments.SequenceEqual(document.HeaderComments),
        "CSV save lost edited values, unknown values, or header comments.");
    Assert(TaskBlackboardInjectionCodec.TryParse("Label,string,A\\,B\\;C;Count,int,3", out var blackboard, out var blackboardError) &&
           blackboard.Count == 2 && blackboard[0].Value == "A,B;C" &&
           TaskBlackboardInjectionCodec.Serialize(blackboard) == "Label,string,A\\,B\\;C;Count,int,3",
        "Task blackboard injection parse/serialize roundtrip failed: " + blackboardError);
    reopened.AddRow(CreateRow("1", "Duplicate", "false", "#GG2233", "1", "1;2;3", "1;2;3;4", "Duration,int,broken", "Still here"));
    Assert(CsvDocumentCodec.Validate(reopened).Any(item => item.Code == "CSV_DUPLICATE_VALUE"), "Typed CSV uniqueness validation did not run.");
    Assert(CsvDocumentCodec.Validate(reopened).Count(item => item.Code == "CSV_VALUE_INVALID") >= 3,
        "Color, Vector, or Task blackboard single-cell validation did not reject malformed values.");
    var mismatchedComments = new CsvDocument();
    mismatchedComments.Columns.Add("Id");
    mismatchedComments.Columns.Add("Name");
    mismatchedComments.HeaderComments.Add("// Identifier only");
    mismatchedComments.HeaderComments.Add("// Associated: None");
    Assert(CsvDocumentCodec.GetFieldDescriptions(mismatchedComments).Count == 0 &&
           CsvDocumentCodec.Validate(mismatchedComments).Any(item => item.Code == "CSV_FIELD_COMMENTS_INVALID"),
        "A first CSV comment with a mismatched comma-separated column count was applied.");
    Console.WriteLine("PASS typed CSV parse, per-column comments, save, blackboard codec, special-value preservation, and validation");
}

static void TestCsvAssociationTargets()
{
    Assert(CsvAssociationContract.TryParse("// Associated: SharedTable, SoloTable", out var names, out var error) &&
           names.SequenceEqual(new[] { "SharedTable", "SoloTable" }),
        "The Associated CSV contract did not parse a valid sorted list: " + error);
    Assert(CsvAssociationContract.TryParse("// Associated: None", out names, out error) && names.Count == 0,
        "The Associated CSV contract did not parse None: " + error);
    Assert(!CsvAssociationContract.TryParse("// Associated: SoloTable, SharedTable", out _, out _),
        "The Associated CSV contract accepted a non-sorted list.");

    var shared = new CsvTypeMetadata
    {
        TypeName = "SharedCsvMetadata",
        FullTypeName = "Game.SharedCsvMetadata",
        DataLoadType = "Addition",
        TableNames = ["SharedTable", "SharedAlias"],
    };
    var solo = new CsvTypeMetadata
    {
        TypeName = "SoloTable",
        FullTypeName = "Game.SoloTable",
        DataLoadType = "Override",
        TableNames = ["SoloTable"],
    };
    var catalog = new BbxMetadataCatalog([shared, solo], [], []);
    Assert(ReferenceEquals(catalog.FindCsvByTableName("SharedAlias"), shared) &&
           ReferenceEquals(catalog.FindCsvForPath(@"C:\Game\SharedTable.csv"), shared),
        "CSV metadata lookup did not resolve table names and file paths consistently.");

    var document = new CsvDocument { FilePath = @"C:\Game\Assets\Resources\Source.csv" };
    document.HeaderComments.Add("// Source identifier");
    document.HeaderComments.Add("// Associated: SharedTable, SoloTable");
    var files = new[]
    {
        new IndexedProjectFile(@"C:\Game\Assets\Resources\SharedTable.csv", "Assets/Resources/SharedTable.csv", ProjectFileKind.Csv, "Native"),
        new IndexedProjectFile(@"C:\Game\Assets\Resources\SharedAlias.csv", "Assets/Resources/SharedAlias.csv", ProjectFileKind.Csv, "Native"),
        new IndexedProjectFile(@"C:\Game\Mods\Example\SharedTable.csv", "Mods/Example/SharedTable.csv", ProjectFileKind.Csv, "Example"),
        new IndexedProjectFile(@"C:\Game\Mods\Other\SoloTable.csv", "Mods/Other/SoloTable.csv", ProjectFileKind.Csv, "Other"),
        new IndexedProjectFile(@"C:\Game\Mods\Example\SharedTable.asset", "Mods/Example/SharedTable.asset", ProjectFileKind.ScriptableObject, "Example"),
    };
    var targets = CsvAssociationTargetResolver.Resolve(document, catalog, files, true);
    Assert(targets.Count == 4 && targets.All(target => target.CanOpen) &&
           targets.Select(target => target.TableName).SequenceEqual(new[] { "SharedAlias", "SharedTable", "SharedTable", "SoloTable" }) &&
           targets.Select(target => target.File!.ModName).SequenceEqual(new[] { "Native", "Native", "Example", "Other" }),
        "Associated CSV targets did not expand metadata table names or sort Native and Mod files deterministically.");
    Assert(CsvEditorControl.FormatAssociatedTargetLabel(targets[0]) == "SharedAlias · SharedCsvMetadata · Native" &&
           CsvEditorControl.FormatAssociatedTargetLabel(targets[1]) == "SharedTable · SharedCsvMetadata · Native" &&
           CsvEditorControl.FormatAssociatedTargetLabel(targets[3]) == "SoloTable · Other" &&
           CsvEditorControl.BuildAssociatedTargetToolTip(targets[0]).Contains("Type: Game.SharedCsvMetadata", StringComparison.Ordinal) &&
           CsvEditorControl.BuildAssociatedTargetToolTip(targets[0]).Contains("Path: Assets/Resources/SharedAlias.csv", StringComparison.Ordinal),
        "Associated CSV menu labels or tooltips did not apply the table/type/Mod display rules.");

    document.HeaderComments[1] = "// Associated: MissingTable";
    targets = CsvAssociationTargetResolver.Resolve(document, catalog, files, true);
    Assert(targets.Count == 1 && !targets[0].CanOpen && targets[0].UnavailableReason!.Contains("metadata", StringComparison.OrdinalIgnoreCase),
        "An Associated CSV without metadata did not remain visible as an unavailable target.");
    document.HeaderComments[1] = "// Associated: SoloTable";
    targets = CsvAssociationTargetResolver.Resolve(document, catalog, files, false);
    Assert(targets.Count == 1 && !targets[0].CanOpen && targets[0].UnavailableReason == "The project file index is not ready.",
        "Associated CSV resolution did not expose the unavailable project index state.");
    Console.WriteLine("PASS Associated CSV contract, metadata expansion, Mod targets, sorting, and missing states");
}

static void TestMetadataCatalog(string root)
{
    var metadataRoot = Path.Combine(root, "Metadata");
    Directory.CreateDirectory(Path.Combine(metadataRoot, "Csv"));
    Directory.CreateDirectory(Path.Combine(metadataRoot, "ScriptableObject"));
    var jsonOptions = new JsonSerializerOptions { WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    var csv = new CsvTypeMetadata { TypeName = "ShipCsvData", FullTypeName = "Game.ShipCsvData", TableNames = ["ShipCsvData"] };
    File.WriteAllText(Path.Combine(metadataRoot, "Csv", "ShipCsvData.json"), JsonSerializer.Serialize(csv, jsonOptions));
    var so = CreateScriptableMetadata();
    File.WriteAllText(Path.Combine(metadataRoot, "ScriptableObject", "Settings.json"), JsonSerializer.Serialize(so, jsonOptions));
    var loaded = BbxMetadataCatalog.LoadFromDirectory(metadataRoot);
    Assert(loaded.Success, Join(loaded.Diagnostics.Select(item => item.Message)));
    Assert(loaded.Value!.FindCsvForPath("ShipCsvData.csv")?.FullTypeName == "Game.ShipCsvData", "CSV metadata lookup failed.");
    Assert(loaded.Value.FindScriptableObjectByGuid(so.ScriptGuid)?.FullTypeName == so.FullTypeName, "ScriptableObject GUID lookup failed.");
    Console.WriteLine("PASS Unity-export metadata catalog");
}

static void TestScriptableObjectDocuments(string root)
{
    var metadata = CreateScriptableMetadata();
    var path = Path.Combine(root, "Settings.asset");
    const string yaml = """
        %YAML 1.1
        %TAG !u! tag:unity3d.com,2011:
        --- !u!114 &11400000
        MonoBehaviour:
          m_ObjectHideFlags: 0
          m_Script: {fileID: 11500000, guid: 0123456789abcdef0123456789abcdef, type: 3}
          m_Name: Settings
          Speed: 1.5
          Enabled: 1
          Mode: 5
          Row:
            Width: 1
          Names:
          - One
          - Two
        """;
    File.WriteAllText(path, yaml, new UTF8Encoding(false));
    var opened = ScriptableObjectDocumentCodec.Open(path, metadata);
    var document = opened.Value ?? throw new InvalidOperationException(Join(opened.Diagnostics.Select(item => item.Message)));
    Assert(document.Properties.Any(item => item.Path == "Row.Width"), "Nested ScriptableObject property was not discovered.");
    Assert(document.Properties.Single(item => item.Path == "Enabled").Value == "true", "Unity boolean was not converted for editing.");
    Assert(document.Properties.Single(item => item.Path == "Mode").Value == "Active", "Unity enum number was not converted for editing.");
    document.Properties.Single(item => item.Path == "Speed").Value = "2.5";
    document.Properties.Single(item => item.Path == "Enabled").Value = "false";
    document.Properties.Single(item => item.Path == "Mode").Value = "Idle";
    document.Properties.Single(item => item.Path == "Names").Value = "One\nTwo\nThree";
    var save = ScriptableObjectDocumentCodec.Save(document, path);
    Assert(save.Success, Join(save.Diagnostics.Select(item => item.Message)));
    var saved = File.ReadAllText(path);
    Assert(saved.Contains("m_Script: {fileID: 11500000, guid: 0123456789abcdef0123456789abcdef, type: 3}", StringComparison.Ordinal), "Unity script reference was changed.");
    Assert(saved.Contains("Speed: 2.5", StringComparison.Ordinal) && saved.Contains("Enabled: 0", StringComparison.Ordinal) && saved.Contains("Mode: 0", StringComparison.Ordinal) && saved.Contains("- Three", StringComparison.Ordinal), "ScriptableObject edits were not written.");
    document.Properties.Single(item => item.Path == "Speed").Value = "3.5";
    Assert(ScriptableObjectDocumentCodec.Save(document, path).Success && File.ReadAllText(path).Contains("Speed: 3.5", StringComparison.Ordinal), "Repeated ScriptableObject save failed.");
    Console.WriteLine("PASS metadata-limited BbxScriptableObject YAML editing");
}

static void TestSettings(string root)
{
    var service = new SettingsService(Path.Combine(root, "settings.json"));
    var settings = new AppSettings { GameProjectPath = "Game", MetadataPath = "Metadata", VectorSearchEnabled = true };
    settings.RecordRecentDocument(Path.Combine(root, "ShipCsvData.csv"));
    service.Save(settings);
    var loaded = service.Load();
    Assert(loaded.GameProjectPath == "Game" && loaded.MetadataPath == "Metadata" && loaded.RecentDocumentPaths.Count == 1 && loaded.VectorSearchEnabled,
        "BbxEditor settings did not roundtrip.");
    Assert(loaded.ExplorerDirectories.SequenceEqual(AppSettings.DefaultExplorerDirectories), "Default Explorer directories did not roundtrip.");
    var portableTarget = Path.Combine(root, "GameProject");
    var portableValue = PortablePath.MakeRelative(root, portableTarget);
    Assert(PortablePath.Resolve(root, portableValue) == Path.GetFullPath(portableTarget), "Relative project paths were not resolved from the settings directory.");
    Console.WriteLine("PASS BbxEditor settings");
}

static void TestVectorSearchInfrastructure(string root)
{
    Assert(VectorSearchNameNormalizer.NormalizeFileName("TaskEnemyCsvData.editor.json") == "Enemy",
        "Task prefix, CsvData suffix, or editor extension was not removed.");
    Assert(VectorSearchNameNormalizer.NormalizeFileName("ShipCsvData.csv") == "Ship",
        "CSV extension or CsvData suffix was not removed.");
    Assert(VectorSearchNameNormalizer.NormalizeFileName("PlayerWeaponData.asset") == "Player Weapon",
        "Data suffix or PascalCase boundary was not normalized.");
    Assert(VectorSearchNameNormalizer.NormalizeTaskName("Game.Tasks.TaskNodeEnemySpawnTask") == "Enemy Spawn",
        "Task namespace, TaskNode prefix, or Task suffix was not removed.");
    Assert(VectorSearchNameNormalizer.NormalizeTaskName("TaskOnceResolveProjectileHit") == "Resolve Projectile Hit",
        "The TaskOnce prefix was not removed.");
    Assert(VectorSearchNameNormalizer.NormalizeTaskName("TaskDurationChargeWeapon") == "Charge Weapon",
        "The TaskDuration prefix was not removed.");
    Assert(VectorSearchNameNormalizer.NormalizeTaskName("TaskConditionEnemyAlive") == "Enemy Alive",
        "The TaskCondition prefix was not removed.");
    Assert(VectorSearchNameNormalizer.NormalizeTaskName("TaskBtRoot") == "Root",
        "The TaskBt framework prefix was not removed.");
    Assert(VectorSearchNameNormalizer.NormalizeTaskName("TaskTimeline") == "Timeline",
        "The standalone TaskTimeline type should retain its meaningful Timeline name.");
    Assert(VectorSearchNameNormalizer.NormalizeQuery("enemy data") == "enemy data",
        "Search queries must not use the file-name affix removal rules.");

    var commonFile = Path.Combine(root, "BbxCommon", "settings.json");
    var commonService = new BbxCommonSettingsService(commonFile);
    var common = commonService.Load();
    Assert(File.Exists(commonFile) && common.ModelDirectory.Length == 0, "BbxCommon settings were not created on first load.");
    common.ModelDirectory = Path.Combine(root, "Models");
    commonService.Save(common);
    Assert(commonService.Load().ModelDirectory == common.ModelDirectory, "The shared model directory did not roundtrip.");

    var cacheFile = Path.Combine(root, "vector-index.json");
    var cacheStore = new VectorIndexCacheStore(cacheFile);
    var cache = new VectorIndexCache
    {
        ModelFingerprint = "test-model",
        Dimension = 3,
        Vectors = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Enemy"] = [1, 0, 0],
        },
    };
    cacheStore.Save(cache);
    var loaded = cacheStore.Load();
    Assert(loaded.ModelFingerprint == "test-model" && loaded.Dimension == 3 && loaded.Vectors["enemy"].SequenceEqual([1f, 0f, 0f]),
        "The local key/value vector cache did not roundtrip.");
    Console.WriteLine("PASS shared model settings, file-name normalization, and vector cache");
}

static void TestTaskSelectionVectorOrdering()
{
    var tasks = new[]
    {
        new TaskDefinition("TaskEnemyDeathEffectTask", "Game.TaskEnemyDeathEffectTask", null, [], []),
        new TaskDefinition("TaskShipRepairTask", "Game.TaskShipRepairTask", null, [], []),
        new TaskDefinition("TaskPlayerMoveTask", "Game.TaskPlayerMoveTask", null, [], []),
    };
    var literal = BbxEditor.Wpf.Views.TaskSelectionWindow.FindLiteralMatches(tasks, "repair");
    Assert(literal.Count == 1 && literal[0].TypeName == "TaskShipRepairTask", "Task node literal search did not find the expected type.");
    var merged = BbxEditor.Wpf.Views.TaskSelectionWindow.MergeVectorResults(tasks, literal, ["Enemy Death Effect", "Ship Repair"]);
    Assert(merged.Count == 2 && merged[0].TypeName == "TaskShipRepairTask" && merged[1].TypeName == "TaskEnemyDeathEffectTask",
        "Task node search did not keep literal matches before deduplicated vector matches.");
    Console.WriteLine("PASS Task node literal-first vector result ordering");
}

static void TestBehaviorTreeConnectionRouting()
{
    var (shortFirst, shortSecond) = BbxEditor.Wpf.Views.BehaviorTreeCanvas.CalculateBezierControls(
        new System.Windows.Point(100, 100), new System.Windows.Point(110, 125));
    Assert(shortFirst.X <= shortSecond.X && shortFirst.X >= 100 && shortSecond.X <= 110,
        "A short forward behavior-tree connection still overshoots its endpoints.");

    var (backwardFirst, backwardSecond) = BbxEditor.Wpf.Views.BehaviorTreeCanvas.CalculateBezierControls(
        new System.Windows.Point(200, 100), new System.Windows.Point(150, 120));
    Assert(backwardFirst.X > 200 && backwardSecond.X < 150 && backwardFirst.X - 200 <= 90 && 150 - backwardSecond.X <= 90,
        "A backward behavior-tree connection did not receive bounded return clearance.");
    Console.WriteLine("PASS adaptive behavior-tree connection routing");
}

static void TestBehaviorTreeNodeSearch()
{
    static BehaviorNode Node(string title, string typeName) => new()
    {
        Name = title,
        Task = TaskInstance.FromDefinition(new TaskDefinition(typeName, "Game." + typeName, null, [], [])),
    };

    var literalTitle = Node("Enemy Attack", "TaskUnrelated");
    var literalType = Node("Condition Node", "TaskEnemyCondition");
    var vectorTitleBest = Node("Shared Title", "TaskSharedType");
    var vectorTitleLater = Node("Boss Title", "TaskOtherBoss");
    var vectorType = Node("Support Node", "TaskSupportTarget");
    var nodes = new[] { vectorType, vectorTitleLater, literalType, vectorTitleBest, literalTitle };
    var rankedVectors = new[] { "Support Target", "Shared Title", "Shared Type", "Boss Title", "Other Boss" };

    var ranked = BehaviorTreeNodeSearch.Rank(nodes, "enemy", rankedVectors);
    Assert(ranked.Select(result => result.Node).SequenceEqual(new[]
           {
               literalTitle, literalType, vectorTitleBest, vectorTitleLater, vectorType,
           }) &&
           ranked.Select(result => result.Tier).SequenceEqual(new[]
           {
               BehaviorTreeNodeSearchTier.LiteralTitle,
               BehaviorTreeNodeSearchTier.LiteralTypeName,
               BehaviorTreeNodeSearchTier.VectorTitle,
               BehaviorTreeNodeSearchTier.VectorTitle,
               BehaviorTreeNodeSearchTier.VectorTypeName,
           }) &&
           ranked.Select(result => result.Node.Id).Distinct().Count() == ranked.Count,
        "Behavior Tree node search did not apply four-tier ordering or per-node result merging.");

    var literalOnly = BehaviorTreeNodeSearch.Rank(nodes, "enemy", []);
    Assert(literalOnly.Select(result => result.Node).SequenceEqual(new[] { literalTitle, literalType }),
        "Behavior Tree node search did not fall back to literal-only results without a vector index.");

    var pan = BehaviorTreeCanvas.CalculateCenteredPan(new System.Windows.Point(300, 200), new System.Windows.Size(1000, 600), 1.5);
    Assert(Math.Abs(pan.X - 50) < .001 && Math.Abs(pan.Y) < .001,
        "Behavior Tree search centering did not place the node at the viewport center.");
    Console.WriteLine("PASS Behavior Tree Ctrl+F ordering, node merge, literal fallback, and viewport centering");
}

static async Task TestOpenDocumentFileWatchAsync(string root)
{
    var directory = Path.Combine(root, "OpenDocumentWatch");
    Directory.CreateDirectory(directory);
    var path = Path.Combine(directory, "Watched.md");
    await File.WriteAllTextAsync(path, "title: Before\n");
    var initialFingerprint = OpenDocumentFileWatch.ReadFingerprint(path);
    Assert(initialFingerprint is not null and not "<missing>", "The initial open-document fingerprint was not created.");

    var changed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    using (var watch = new OpenDocumentFileWatch(path, changedPath => changed.TrySetResult(changedPath)))
    {
        await File.WriteAllTextAsync(path, "title: After\n");
        var completed = await Task.WhenAny(changed.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert(ReferenceEquals(completed, changed.Task), "The open-document watcher did not report an external write.");
        Assert(Path.GetFullPath(await changed.Task) == Path.GetFullPath(path), "The open-document watcher reported the wrong path.");
    }

    var changedFingerprint = OpenDocumentFileWatch.ReadFingerprint(path);
    Assert(changedFingerprint is not null && changedFingerprint != initialFingerprint,
        "The open-document fingerprint did not change after an external write.");
    File.Delete(path);
    Assert(OpenDocumentFileWatch.ReadFingerprint(path) == "<missing>", "A deleted open document was not identified as missing.");
    Console.WriteLine("PASS open-document external change watch and fingerprints");
}

static async Task TestDesignPlanDirectoryWatchAsync(string root)
{
    var projectRoot = Path.Combine(root, "DesignPlanWatchProject");
    var dateDirectory = Path.Combine(projectRoot, "AutoDoc", "DesignPlan", "2026.08.10");
    Directory.CreateDirectory(dateDirectory);
    var path = Path.Combine(dateDirectory, "watched-plan.md");
    await File.WriteAllTextAsync(path, "title: Before\nstate: Todo\npriority: P1\n");

    var changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    using (var watch = new DesignPlanDirectoryWatch(projectRoot, () => changed.TrySetResult(true)))
    {
        await File.WriteAllTextAsync(path, "title: After\nstate: Completed\npriority: P2\n");
        await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
    Assert(DesignPlanIndexService.Scan(projectRoot).Single().Documents.Single().Title == "After",
        "The design plan directory watcher notification did not correspond to an updated index snapshot.");
    Console.WriteLine("PASS design-plan directory watcher refresh");
}

static async Task TestProjectFileIndexAsync(string root)
{
    var projectRoot = Path.Combine(root, "IndexProject");
    var resources = Path.Combine(projectRoot, "Assets", "Resources", "Task");
    var nativeMod = Path.Combine(projectRoot, "Mods", "Native", "Data");
    var customMod = Path.Combine(projectRoot, "Mods", "ExampleMod", "Data");
    Directory.CreateDirectory(resources);
    Directory.CreateDirectory(nativeMod);
    Directory.CreateDirectory(customMod);
    File.WriteAllText(Path.Combine(resources, "Native.editor.json"), "{}");
    File.WriteAllText(Path.Combine(nativeMod, "Native.csv"), "Id\n1\n");
    File.WriteAllText(Path.Combine(customMod, "Custom.asset"), "asset");
    File.WriteAllText(Path.Combine(resources, "Ignored.png"), "not an editor document");
    File.WriteAllText(Path.Combine(projectRoot, "OutsideConfiguredRoots.csv"), "Id\n9\n");

    using var service = new ProjectFileIndexService();
    IReadOnlyList<IndexedProjectFile> snapshot = [];
    var changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    var directoryChanged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    var classifierInvocations = 0;
    service.IndexChanged += (_, args) =>
    {
        snapshot = args.Files;
        if (args.Files.Any(file => file.RelativePath.EndsWith("Watched.csv", StringComparison.OrdinalIgnoreCase))) changed.TrySetResult(true);
        if (args.Files.Any(file => file.RelativePath.EndsWith("NewMod/Data/New.csv", StringComparison.OrdinalIgnoreCase))) directoryChanged.TrySetResult(true);
    };
    await service.StartAsync(projectRoot, AppSettings.DefaultExplorerDirectories, path =>
    {
        classifierInvocations++;
        return path.EndsWith(".editor.json", StringComparison.OrdinalIgnoreCase) ? new ProjectFileClassification(ProjectFileKind.Task, TaskFileEditorKind.Timeline) :
            path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? new ProjectFileClassification(ProjectFileKind.Csv) :
            path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ? new ProjectFileClassification(ProjectFileKind.ScriptableObject) : null;
    });
    Assert(snapshot.Count == 3, "Initial project file index did not find all supported files.");
    Assert(classifierInvocations == 3, "The project index parsed files outside the supported editor extensions.");
    Assert(snapshot.Single(file => file.RelativePath.EndsWith("Native.editor.json", StringComparison.Ordinal)).DisplayName == "Native",
        "Explorer did not treat .editor.json as one display suffix.");
    Assert(snapshot.Single(file => file.RelativePath.EndsWith("Native.editor.json", StringComparison.Ordinal)).FileTypeLabel == "Task · Timeline",
        "Explorer did not expose the Task editor subtype label.");
    Assert(snapshot.Single(file => file.RelativePath.EndsWith("Native.csv", StringComparison.Ordinal)).FileTypeLabel == "CSV",
        "Explorer did not expose the compact CSV type label.");
    Assert(snapshot.Single(file => file.RelativePath.EndsWith("Native.editor.json", StringComparison.Ordinal)).ModName == "Native", "Resources files were not assigned to Native.");
    Assert(snapshot.Single(file => file.RelativePath.EndsWith("Native.csv", StringComparison.Ordinal)).ModName == "Native", "Mods/Native files were not assigned to Native.");
    Assert(snapshot.Single(file => file.RelativePath.EndsWith("Custom.asset", StringComparison.Ordinal)).ModName == "ExampleMod", "Custom mod ownership was not detected.");

    File.WriteAllText(Path.Combine(customMod, "Watched.csv"), "Id\n2\n");
    await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Assert(snapshot.Any(file => file.RelativePath.EndsWith("Watched.csv", StringComparison.OrdinalIgnoreCase)), "FileSystemWatcher did not refresh the project index.");
    var newMod = Path.Combine(projectRoot, "Mods", "NewMod", "Data");
    Directory.CreateDirectory(newMod);
    File.WriteAllText(Path.Combine(newMod, "New.csv"), "Id\n3\n");
    await directoryChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Assert(snapshot.Single(file => file.RelativePath.EndsWith("NewMod/Data/New.csv", StringComparison.OrdinalIgnoreCase)).ModName == "NewMod",
        "FileSystemWatcher did not attach to a newly created mod directory.");
    Console.WriteLine("PASS asynchronous project index, Native/mod ownership, and FileSystemWatcher refresh");
}

static void TestCsvColumnSearchOrdering()
{
    var rows = new[]
    {
        new CsvColumnSearchResult(0, "Battleship"),
        new CsvColumnSearchResult(1, "Ship"),
        new CsvColumnSearchResult(2, "Heavy Enemy"),
        new CsvColumnSearchResult(3, "Combat Audio"),
    };
    var literal = CsvColumnSearchWindow.FindLiteralMatches(rows, "ship");
    Assert(literal.Select(row => row.RowIndex).SequenceEqual(new[] { 1, 0 }),
        "CSV Turn To search did not place an exact value before substring matches.");
    var merged = CsvColumnSearchWindow.MergeVectorResults(rows, literal, new[] { "Heavy Enemy", "Ship", "Combat Audio" });
    Assert(merged.Select(row => row.RowIndex).SequenceEqual(new[] { 1, 0, 2, 3 }),
        "CSV Turn To search did not append deduplicated semantic rows after literal matches.");
    Assert(CsvEditorControl.SupportsVectorSearch(new EditorFieldMetadata { Type = new EditorTypeMetadata { Kind = EditorValueKind.String } }) &&
           !CsvEditorControl.SupportsVectorSearch(new EditorFieldMetadata { Type = new EditorTypeMetadata { Kind = EditorValueKind.Int32 } }) &&
           !CsvEditorControl.SupportsVectorSearch(null),
        "CSV Turn To semantic search was not restricted to exported String columns.");
    Assert(CsvEditorControl.BuildCsvColumnVectorKey(@"C:\Game\ShipCsvData.csv", "DisplayName") == "ShipCsvData-DisplayName",
        "CSV vector partition key did not use the CSV name and column header.");
    Console.WriteLine("PASS editable CSV header search ordering and String-only vector eligibility");
}

static void TestInspectorStrategySelection()
{
    Assert(InspectorControl.ResolveStrategyKind(null) == InspectorStrategyKind.Task &&
           InspectorControl.ResolveStrategyKind(new CsvDocument()) == InspectorStrategyKind.Csv,
        "The shared Inspector did not select its strategy from the active document kind.");
    Assert(InspectorControl.ResolveCsvEditorKind(null) == CsvInspectorEditorKind.Text &&
           InspectorControl.ResolveCsvEditorKind(new EditorFieldMetadata
           {
               Type = new EditorTypeMetadata { Kind = EditorValueKind.Array, ElementType = new EditorTypeMetadata { Kind = EditorValueKind.String } },
           }) == CsvInspectorEditorKind.Array &&
           InspectorControl.ResolveCsvEditorKind(new EditorFieldMetadata
           {
               Type = new EditorTypeMetadata { Kind = EditorValueKind.Boolean },
           }) == CsvInspectorEditorKind.BooleanOptions &&
           InspectorControl.ResolveCsvEditorKind(new EditorFieldMetadata
           {
               Type = new EditorTypeMetadata { Kind = EditorValueKind.Enum, EnumValues = ["Idle", "Run"] },
           }) == CsvInspectorEditorKind.EnumOptions &&
           InspectorControl.ResolveCsvEditorKind(new EditorFieldMetadata
           {
               Type = new EditorTypeMetadata { Kind = EditorValueKind.Vector2 },
           }) == CsvInspectorEditorKind.Vector &&
           InspectorControl.ResolveCsvEditorKind(new EditorFieldMetadata
           {
               Type = new EditorTypeMetadata { Kind = EditorValueKind.Vector3 },
           }) == CsvInspectorEditorKind.Vector &&
           InspectorControl.ResolveCsvEditorKind(new EditorFieldMetadata
           {
               Type = new EditorTypeMetadata { Kind = EditorValueKind.Vector4 },
           }) == CsvInspectorEditorKind.Vector &&
           InspectorControl.ResolveCsvEditorKind(new EditorFieldMetadata
           {
               Type = new EditorTypeMetadata { Kind = EditorValueKind.Color },
           }) == CsvInspectorEditorKind.Color &&
           InspectorControl.ResolveCsvEditorKind(new EditorFieldMetadata
           {
               Type = new EditorTypeMetadata { Kind = EditorValueKind.TaskBlackboardInjection },
           }) == CsvInspectorEditorKind.TaskBlackboardInjection &&
           InspectorControl.ResolveCsvEditorKind(new EditorFieldMetadata
           {
               Type = new EditorTypeMetadata { Kind = EditorValueKind.Int32 },
           }) == CsvInspectorEditorKind.Text,
        "The CSV Inspector did not choose direct value editors from exported field types.");
    var decoded = CsvArrayValueCodec.Decode("Alpha;;Gamma");
    Assert(decoded.SequenceEqual(new[] { "Alpha", string.Empty, "Gamma" }) && CsvArrayValueCodec.Encode(decoded) == "Alpha;;Gamma" &&
           CsvArrayValueCodec.Decode(string.Empty).Count == 0,
        "The CSV Inspector array codec did not preserve the semicolon cell protocol.");
    var cell = new CsvCell { Value = "Alpha;Beta" };
    Assert(InspectorControl.TryApplyCsvValue(cell, CsvArrayValueCodec.Encode(["Beta", "Alpha"])) && cell.Value == "Beta;Alpha" &&
           !InspectorControl.TryApplyCsvValue(cell, "Beta;Alpha"),
        "The CSV Inspector apply path did not write a structured array value back to its cell.");
    Assert(CsvInspectorValueCodec.GetVectorComponentCount(EditorValueKind.Vector2) == 2 &&
           CsvInspectorValueCodec.GetVectorComponentCount(EditorValueKind.Vector3) == 3 &&
           CsvInspectorValueCodec.GetVectorComponentCount(EditorValueKind.Vector4) == 4 &&
           CsvInspectorValueCodec.DecodeVector("1.25;-2", 4).SequenceEqual(new[] { "1.25", "-2", string.Empty, string.Empty }) &&
           CsvInspectorValueCodec.EncodeVector(["1", "2", "3", "4"]) == "1;2;3;4",
        "The CSV Inspector vector component codec did not preserve the runtime semicolon protocol.");
    Assert(CsvInspectorValueCodec.TryParseColor("#10203040", out var color) &&
           color == new CsvInspectorColor(0x10, 0x20, 0x30, 0x40, true) &&
           CsvInspectorValueCodec.TryParseColor("#ABCDEF", out var opaqueColor) &&
           opaqueColor == new CsvInspectorColor(0xAB, 0xCD, 0xEF, 0xFF, false) &&
           !CsvInspectorValueCodec.TryParseColor("#12345G", out _) &&
           CsvInspectorValueCodec.WithRgb("#10203040", 0xAA, 0xBB, 0xCC) == "#AABBCC40" &&
           CsvInspectorValueCodec.WithRgb("#102030", 0xAA, 0xBB, 0xCC) == "#AABBCC",
        "The CSV Inspector color codec did not preserve the Unity #RRGGBB/#RRGGBBAA byte protocol.");
    Console.WriteLine("PASS shared Task/CSV Inspector strategy, special vector/color/blackboard editors, structured codecs, and explicit cell apply path");
}

static void TestMainWindowShortcuts()
{
    Assert(BbxEditor.Wpf.MainWindow.IsSaveShortcut(System.Windows.Input.Key.S, System.Windows.Input.ModifierKeys.Control) &&
           !BbxEditor.Wpf.MainWindow.IsSaveShortcut(System.Windows.Input.Key.S, System.Windows.Input.ModifierKeys.None) &&
           !BbxEditor.Wpf.MainWindow.IsSaveShortcut(System.Windows.Input.Key.O, System.Windows.Input.ModifierKeys.Control),
        "The main-window Ctrl+S shortcut predicate was not exact.");
    Console.WriteLine("PASS main-window Ctrl+S save shortcut routing");
}

static void TestExplorerCurrentDocumentSelection()
{
    var root = Path.Combine(Path.GetTempPath(), "BbxEditorExplorerSelection");
    var firstPath = Path.Combine(root, "First.csv");
    var secondPath = Path.Combine(root, "Second.csv");
    var files = new[]
    {
        new IndexedProjectFile(firstPath, "First.csv", ProjectFileKind.Csv, "Native"),
        new IndexedProjectFile(secondPath, "Second.csv", ProjectFileKind.Csv, "Native"),
    };

    Assert(ReferenceEquals(
               MainViewModel.ResolveCurrentExplorerFile(
                   files,
                   new CsvDocument { FilePath = secondPath.ToUpperInvariant() }),
               files[1]) &&
           MainViewModel.ResolveCurrentExplorerFile(files, new CsvDocument { FilePath = Path.Combine(root, "Missing.csv") }) is null &&
           MainViewModel.ResolveCurrentExplorerFile(files, new DesignPlanDocument
           {
               FilePath = secondPath,
               Title = "Second",
               Markdown = string.Empty,
           }) is null &&
           MainViewModel.ResolveCurrentExplorerFile(files, null) is null,
        "The Explorer did not resolve its highlighted file from the active editor tab path.");
    Console.WriteLine("PASS active editor tab synchronization with the Explorer file highlight");
}

static void TestWorkspaceChromeTheme()
{
    var repositoryRoot = FindRepositoryRoot();
    var themePath = Path.Combine(repositoryRoot, "src", "BbxEditor.Wpf", "Themes", "GrayTheme.xaml");
    var explorerPath = Path.Combine(repositoryRoot, "src", "BbxEditor.Wpf", "Views", "ExplorerControl.xaml");
    var theme = File.ReadAllText(themePath);
    var explorer = File.ReadAllText(explorerPath);
    Assert(theme.Contains("x:Name=\"DocumentTabHeaderWrapPanel\" IsItemsHost=\"True\"", StringComparison.Ordinal) &&
           theme.Contains("<RowDefinition Height=\"Auto\" />", StringComparison.Ordinal) &&
           !theme.Contains("x:Name=\"DocumentTabHeaderScrollViewer\"", StringComparison.Ordinal),
        "The document tab header does not wrap excess tabs into automatically sized rows.");
    Assert(!theme.Contains("ToolTip=\"New Document\"", StringComparison.Ordinal) &&
           explorer.Contains("Header=\"Create\" ToolTip=\"Create a new document\"", StringComparison.Ordinal) &&
           explorer.Contains("VerticalAlignment=\"Center\" Height=\"30\"", StringComparison.Ordinal) &&
           explorer.Contains("Height=\"30\" Padding=\"10,0\" VerticalContentAlignment=\"Center\"", StringComparison.Ordinal) &&
           explorer.Contains("BorderBrush=\"{StaticResource BorderStrongBrush}\" BorderThickness=\"1\"", StringComparison.Ordinal) &&
           explorer.Contains("BorderThickness=\"{Binding BorderThickness, ElementName=ExplorerSearchBox}\"", StringComparison.Ordinal) &&
           explorer.Contains("Padding=\"{Binding Padding, ElementName=ExplorerSearchBox}\"", StringComparison.Ordinal) &&
           explorer.Contains("BorderThickness=\"{Binding BorderThickness, ElementName=DesignPlanSearchBox}\"", StringComparison.Ordinal) &&
           explorer.Contains("Padding=\"{Binding Padding, ElementName=DesignPlanSearchBox}\"", StringComparison.Ordinal) &&
           !explorer.Contains("Text=\"{Binding ExplorerFilterText}\"", StringComparison.Ordinal) &&
           explorer.Contains("Header=\"Timeline\" Command=\"{Binding NewTimelineCommand}\"", StringComparison.Ordinal) &&
           explorer.Contains("Header=\"Behavior Tree\" Command=\"{Binding NewBehaviorTreeCommand}\"", StringComparison.Ordinal),
        "The Explorer toolbar or DPI-safe search placeholder alignment does not match the required layout.");
    Console.WriteLine("PASS wrapped document tabs and height-aligned Explorer Files Create menu");
}

static async Task TestProductionVectorSearchAsync(string root)
{
    var repositoryRoot = FindRepositoryRoot();
    var configuredModelRoot = Environment.GetEnvironmentVariable("BBX_MODEL_DIRECTORY") ??
                              new BbxCommonSettingsService().Load().ModelDirectory;
    var workerExecutable = Environment.GetEnvironmentVariable("BBX_WORKER_EXECUTABLE") ??
                           Path.Combine(repositoryRoot, "src", "BbxEditor.Wpf", "bin", "Debug", "net10.0-windows", "BbxEditor.exe");
    Assert(EmbeddingModelLayout.ResolveModelDirectory(configuredModelRoot) is not null, "The production multilingual MPNet model is unavailable.");
    Assert(File.Exists(workerExecutable), "The debug BbxEditor worker executable is unavailable.");

    var cacheFile = Path.Combine(root, "production-vector-index.json");
    var csvCacheFile = Path.Combine(root, "production-csv-vector-index.tmp.json");
    var statuses = new List<string>();
    var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    var coordinator = new VectorSearchCoordinator(cacheFile, workerExecutable, csvCacheFile);
    coordinator.StatusChanged += (_, args) =>
    {
        lock (statuses) statuses.Add(args.Status);
        if (args.Ready) ready.TrySetResult(true);
    };
    var workerProcessId = 0;
    try
    {
        await coordinator.ApplyConfigurationAsync(true, configuredModelRoot);
        workerProcessId = coordinator.WorkerProcessId ?? throw new InvalidOperationException(
            $"The embedding worker process was not started. {coordinator.Status}");
        var files = new[]
        {
            new IndexedProjectFile(Path.Combine(root, "TaskEnemyDeathEffectData.editor.json"), "TaskEnemyDeathEffectData.editor.json", ProjectFileKind.Task, "Native"),
            new IndexedProjectFile(Path.Combine(root, "ShipCsvData.csv"), "ShipCsvData.csv", ProjectFileKind.Csv, "Native"),
            new IndexedProjectFile(Path.Combine(root, "KeyboardWeaponSettings.asset"), "KeyboardWeaponSettings.asset", ProjectFileKind.ScriptableObject, "Native"),
        };
        var taskName = VectorSearchNameNormalizer.NormalizeTaskName("TaskEnemySpawnTask");
        var activeNames = files.Select(file => VectorSearchNameNormalizer.NormalizeFileName(file.FileName)).Append(taskName).ToArray();
        coordinator.SynchronizeNames(activeNames);
        await ready.Task.WaitAsync(TimeSpan.FromMinutes(2));
        var cache = new VectorIndexCacheStore(cacheFile).Load();
        Assert(cache.Dimension == 768 && cache.Vectors.Count == 4 && cache.Vectors.ContainsKey("Enemy Spawn"),
            "The production worker did not create the combined file and Task vector corpus.");
        var ranking = await coordinator.RankNamesAsync("ship", cache.Vectors.Keys.ToArray());
        Assert(ranking.FirstOrDefault() == "Ship", "The production centered vector search did not rank Ship first for 'ship'.");

        var persistedCacheBeforeTransientIndex = File.ReadAllText(cacheFile);
        var transientIndex = await coordinator.BuildTransientIndexAsync(
            new[] { "Hostile Cruiser", "Friendly Station", "Repair Drone" });
        var transientRanking = transientIndex is null
            ? []
            : await coordinator.RankTransientIndexAsync("hostile cruiser", transientIndex);
        Assert(transientIndex is { Center.Length: 768 } && transientIndex.Vectors.Count == 3 &&
               transientRanking.FirstOrDefault() == "Hostile Cruiser" &&
               File.ReadAllText(cacheFile).Equals(persistedCacheBeforeTransientIndex, StringComparison.Ordinal),
            "Transient Behavior Tree vectors were not centered/ranked correctly or changed the persistent vector cache.");

        const string csvColumnKey = "ShipCsvData-DisplayName";
        var csvValues = new[] { "Heavy Enemy", "Player Ship", "Combat Audio" };
        var csvRanking = await coordinator.RankCsvColumnValuesAsync(csvColumnKey, "large hostile", csvValues);
        var csvCache = new CsvVectorIndexCacheStore(csvCacheFile).Load();
        Assert(csvRanking.Count == csvValues.Length && csvCache.Columns.TryGetValue(csvColumnKey, out var csvColumnCache) &&
               csvColumnCache.Vectors.Count == csvValues.Length && new VectorIndexCacheStore(cacheFile).Load().Vectors.Count == 4,
            "CSV column vectors were not stored in an isolated cache partition.");
        await coordinator.RankCsvColumnValuesAsync(csvColumnKey, "hostile", csvValues.Take(2).ToArray());
        csvCache = new CsvVectorIndexCacheStore(csvCacheFile).Load();
        Assert(csvCache.Columns[csvColumnKey].Vectors.Count == 2 && !csvCache.Columns[csvColumnKey].Vectors.ContainsKey("Combat Audio"),
            "CSV column vector synchronization did not remove a stale cell value.");

        var rapidValues = Enumerable.Range(0, 16)
            .Select(index => $"Rapid candidate {index} " + string.Join(' ', Enumerable.Repeat("hostile cruiser navigation target", 40)))
            .ToArray();
        using (var rapidCancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)))
        {
            var cancellationObserved = false;
            try
            {
                await coordinator.RankCsvColumnValuesAsync("RapidInput-Test", "hostile target", rapidValues, rapidCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                cancellationObserved = true;
            }
            Assert(cancellationObserved, "The rapid-input vector regression did not exercise request cancellation.");
        }
        var recoveredRanking = await coordinator.RankCsvColumnValuesAsync("RapidInput-Test", "navigation", rapidValues);
        Assert(recoveredRanking.Count == rapidValues.Length,
            "CSV vector search did not recover after a canceled rapid-input request.");

        lock (statuses) statuses.Clear();
        ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        coordinator.SynchronizeNames(files.Take(2).Select(file => VectorSearchNameNormalizer.NormalizeFileName(file.FileName)).Append(taskName));
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
        cache = new VectorIndexCacheStore(cacheFile).Load();
        Assert(cache.Vectors.Count == 3 && cache.Vectors.ContainsKey("Enemy Spawn") && !cache.Vectors.ContainsKey("Keyboard Weapon Settings"),
            "Combined corpus synchronization did not retain active Task vectors or remove stale file vectors.");
        string[] secondPassStatuses;
        lock (statuses) secondPassStatuses = statuses.ToArray();
        Assert(secondPassStatuses.All(status => !status.StartsWith("Embedding file names:", StringComparison.Ordinal)),
            "Existing file-name vectors were embedded again instead of being skipped.");
    }
    finally
    {
        await coordinator.DisposeAsync();
    }
    await Task.Delay(500);
    Assert(!IsProcessAlive(workerProcessId), "The embedding worker outlived its owning coordinator.");
    Console.WriteLine("PASS production worker embedding, centered ranking, isolated CSV column cache, stale removal, and synchronized lifetime");
}

static bool IsProcessAlive(int processId)
{
    try
    {
        using var process = System.Diagnostics.Process.GetProcessById(processId);
        return !process.HasExited;
    }
    catch (ArgumentException)
    {
        return false;
    }
}

static string FindRepositoryRoot()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, "BbxEditor.Net.sln"))) return directory.FullName;
    throw new DirectoryNotFoundException("Could not locate the BbxEditor repository root.");
}

static void TestExistingCsvEditorDocument(string metadataRoot, string csvPath)
{
    var metadataResult = BbxMetadataCatalog.LoadFromDirectory(metadataRoot);
    Assert(metadataResult.Success && metadataResult.Value is not null, Join(metadataResult.Diagnostics.Select(item => item.Message)));
    var catalog = new EditorCatalog(new TaskCatalog([], [], []), metadataResult.Value!);
    var openResult = new WorkspaceDocumentService().Open(csvPath, catalog);
    Assert(openResult.Success && openResult.Value is CsvDocument, Join(openResult.Diagnostics.Select(item => item.Message)));
    var document = (CsvDocument)openResult.Value!;
    var metadata = document.Metadata ?? throw new InvalidOperationException("Existing CSV did not bind to exported metadata.");
    Assert(metadata.FullTypeName == "Pak.ShipCsvData", "Existing CSV did not bind to Pak.ShipCsvData metadata.");
    Assert(document.Columns.Count == 7 && document.Rows.Count == 3, "Existing ShipCsvData.csv shape was not read correctly.");
    var offsetColumn = metadata.Columns.Single(item => item.Name == "UpperExhaustOffset");
    Assert(offsetColumn.BindingMemberName == "UpperExhaustOffset" && offsetColumn.Type.Kind == EditorValueKind.Vector2 &&
           document.Rows[0].Cells[5].Value == "-600;48",
        "CSV Vector2 single-cell metadata or value was not applied.");
    Assert(metadataResult.Value!.ScriptableObjectTypes.Count > 0 && metadataResult.Value.Assets.Count > 0,
        "ScriptableObject type metadata or asset index was not loaded.");
    Console.WriteLine($"PASS existing CSV editor document: {Path.GetFileName(csvPath)} ({document.Rows.Count} rows, {document.Columns.Count} columns, {metadata.FullTypeName})");
}

static async Task TestExistingProjectIndexAsync(string metadataRoot, string projectRoot)
{
    var metadata = BbxMetadataCatalog.LoadFromDirectory(metadataRoot).Value ?? throw new InvalidOperationException("Existing metadata could not be loaded.");
    var tasks = TaskCatalog.LoadFromDirectory(TaskMetadataDirectoryResolver.Resolve(metadataRoot)).Value ?? new TaskCatalog([], [], []);
    var catalog = new EditorCatalog(tasks, metadata);
    var documents = new WorkspaceDocumentService();
    var csvDirectory = Path.Combine(projectRoot, "Assets", "Resources", "Data", "Csv");
    foreach (var csvPath in Directory.GetFiles(csvDirectory, "*.csv", SearchOption.TopDirectoryOnly))
    {
        var csvResult = documents.Open(csvPath, catalog);
        Assert(csvResult.Success && csvResult.Value is CsvDocument csvDocument && csvDocument.HeaderComments.Count == 2 &&
               CsvDocumentCodec.GetFieldDescriptions(csvDocument).Count == csvDocument.Columns.Count,
            $"Existing CSV contract comments failed for {Path.GetFileName(csvPath)}: " +
            Join(csvResult.Diagnostics.Select(item => item.Message)));
    }
    var weaponPath = Path.Combine(projectRoot, "Assets", "Resources", "Data", "Csv", "WeaponCsvData.csv");
    var weaponResult = documents.Open(weaponPath, catalog);
    Assert(weaponResult.Success && weaponResult.Value is CsvDocument,
        Join(weaponResult.Diagnostics.Select(item => item.Message)));
    var weaponDocument = (CsvDocument)weaponResult.Value!;
    var blackboardColumn = weaponDocument.Metadata!.Columns.Single(item => item.Name == "FireTaskBlackboard");
    var blackboardIndex = weaponDocument.Columns.IndexOf("FireTaskBlackboard");
    Assert(blackboardColumn.Type.Kind == EditorValueKind.TaskBlackboardInjection && blackboardIndex >= 0 &&
           weaponDocument.Rows[0].Cells[blackboardIndex].Value.StartsWith("FireDelay,float,0;", StringComparison.Ordinal),
        "Existing WeaponCsvData did not use the exported TaskBlackboardInjection single-cell contract.");
    IReadOnlyList<IndexedProjectFile> snapshot = [];
    using var index = new ProjectFileIndexService();
    index.IndexChanged += (_, eventArgs) => snapshot = eventArgs.Files;
    await index.StartAsync(projectRoot, AppSettings.DefaultExplorerDirectories, path => documents.Open(path, catalog).Value switch
    {
        TimelineDocument => new ProjectFileClassification(ProjectFileKind.Task, TaskFileEditorKind.Timeline),
        BehaviorTreeDocument => new ProjectFileClassification(ProjectFileKind.Task, TaskFileEditorKind.BehaviorTree),
        CsvDocument => new ProjectFileClassification(ProjectFileKind.Csv),
        ScriptableObjectDocument => new ProjectFileClassification(ProjectFileKind.ScriptableObject),
        _ => null,
    });
    Assert(snapshot.Count(file => file.Kind == ProjectFileKind.Task) == 15, "Existing project Task index count was unexpected.");
    Assert(snapshot.Count(file => file.Kind == ProjectFileKind.Csv) == 9, "Existing project CSV index count was unexpected.");
    Assert(snapshot.Count(file => file.Kind == ProjectFileKind.ScriptableObject) == 4, "Existing project BbxScriptableObject index count was unexpected.");
    Assert(snapshot.All(file => file.ModName == "Native"), "Resources files were not grouped into the Native official mod.");
    var associatedTargets = new List<CsvAssociationTarget>();
    foreach (var csvFile in snapshot.Where(file => file.Kind == ProjectFileKind.Csv))
    {
        var csvResult = documents.Open(csvFile.FullPath, catalog);
        Assert(csvResult.Success && csvResult.Value is CsvDocument,
            $"Existing associated CSV could not be opened: {csvFile.RelativePath}");
        associatedTargets.AddRange(CsvAssociationTargetResolver.Resolve(
            (CsvDocument)csvResult.Value!, metadata, snapshot, true));
    }
    Assert(associatedTargets.Count == 18 && associatedTargets.All(target => target.CanOpen) &&
           associatedTargets.All(target => target.File!.ModName == "Native"),
        "The existing 9 CSV files did not resolve all 18 Associated navigation targets to Native files.");
    Console.WriteLine("PASS existing project Explorer index: 15 Task, 9 CSV, 4 BbxScriptableObject, all Native");
}

static ScriptableObjectTypeMetadata CreateScriptableMetadata() => new()
{
    TypeName = "Settings",
    FullTypeName = "Game.Settings",
    ScriptGuid = "0123456789abcdef0123456789abcdef",
    Fields =
    [
        new EditorFieldMetadata { Name = "Speed", Type = new EditorTypeMetadata { Kind = EditorValueKind.Single } },
        new EditorFieldMetadata { Name = "Enabled", Type = new EditorTypeMetadata { Kind = EditorValueKind.Boolean } },
        new EditorFieldMetadata
        {
            Name = "Mode",
            Type = new EditorTypeMetadata
            {
                Kind = EditorValueKind.Enum,
                EnumValues = ["Idle", "Active"],
                EnumNumericValues = new Dictionary<string, long>(StringComparer.Ordinal) { ["Idle"] = 0, ["Active"] = 5 },
            },
        },
        new EditorFieldMetadata
        {
            Name = "Row",
            Type = new EditorTypeMetadata { Kind = EditorValueKind.Object },
            Fields = [new EditorFieldMetadata { Name = "Width", Type = new EditorTypeMetadata { Kind = EditorValueKind.Single } }],
        },
        new EditorFieldMetadata
        {
            Name = "Names",
            Type = new EditorTypeMetadata { Kind = EditorValueKind.Array, ElementType = new EditorTypeMetadata { Kind = EditorValueKind.String } },
        },
    ],
};

static CsvRow CreateRow(params string[] values)
{
    var row = new CsvRow();
    foreach (var value in values) row.Cells.Add(new CsvCell { Value = value });
    return row;
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static string Join(IEnumerable<string> messages) => string.Join(Environment.NewLine, messages);
