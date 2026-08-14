using BbxDeployer.Core;
using BbxDeployer.Services;
using BbxDeployer.ViewModels;
using BbxDeployer.Views;

namespace BbxDeployer.Tests;

internal static class Program
{
    private static async Task<int> Main()
    {
        var tests = new (string Name, Func<Task> Run)[]
        {
            ("Discovers Unity projects with arbitrary names", DiscoversArbitraryGameNames),
            ("Creates contexts from project root folders", CreatesContextFromProjectRoot),
            ("Creates bootstrap contexts for empty project roots", CreatesBootstrapContext),
            ("Discovers installed Unity versions from Hub settings", DiscoversUnityEditors),
            ("Discovers editors from the current Unity Hub installation", DiscoversCurrentHubEditors),
            ("Maps project-relative directory templates", MapsProjectRelativeTemplates),
            ("Creates shared documentation and Codex default items", CreatesSharedConfigDefaults),
            ("Loads the main window", LoadsMainWindow),
            ("Loads the settings window", LoadsSettingsWindow),
            ("Loads the unified transfer directory editor", LoadsSyncItemDialog),
            ("Loads the project editor", LoadsProjectDialog),
            ("Loads the standard root settings file", LoadsStandardRootSettings),
            ("Preview reports incomplete configuration", PreviewReportsIncompleteConfiguration),
            ("Promotes a destination to source", PromotesDestinationToSource),
            ("Targets card commands use their own project", TargetCardCommandsUseProjectParameter),
            ("Applies root and nested gitignore rules", AppliesNestedGitIgnoreRules),
            ("Syncs the Git-ignored deployer project list", SyncsGitIgnoredDeployerProjectList),
            ("Ignores legacy imported gitignore files", IgnoresLegacyImportedIgnoreFiles),
            ("Prunes large ignored directories during preview", PrunesIgnoredDirectories),
            ("Reports preview progress by file and target count", ReportsPreviewFileCountProgress),
            ("Calculates sync progress by file count", CalculatesSyncProgressByFileCount),
            ("Reports sync validation and copy progress", ReportsSyncValidationAndCopyProgress),
            ("Always includes configured transfer directories", AlwaysIncludesConfiguredDirectories),
            ("Expands multiple whitelist paths with separate blacklists", ExpandsMultipleWhitelistPaths),
            ("Plans a single-file whitelist path", PlansSingleFileWhitelist),
            ("Manual blacklist overrides gitignore negation", ManualBlacklistHasPriority),
            ("Does not sync Unity companion meta files", DoesNotSyncCompanionMeta),
            ("Classifies a destination missing most paths as New Project", ClassifiesNewProject),
            ("Classifies stale destination files as Wait for Sync", ClassifiesWaitForSync),
            ("Classifies matching destination files as Synchronized", ClassifiesSynchronized),
            ("Classifies newer destination files as Warning", ClassifiesWarning),
            ("Maps preview statuses to distinct UI colors", MapsPreviewStatusColors),
            ("Skips Unity discovery when no project is new", SkipsUnityDiscoveryWithoutNewProject),
            ("Defaults new projects to the main Unity version", DefaultsToMainUnityVersion),
            ("Bootstraps an empty Unity project with a different game name", BootstrapsEmptyUnityProject),
            ("Creates a new project with the selected Unity Editor", CreatesProjectWithSelectedEditor),
            ("Reports Unity project creation failures", ReportsUnityCreationFailure),
            ("Preserves existing Unity bootstrap files", PreservesBootstrapFiles),
            ("Blocks unsafe nested Unity bootstrap paths", BlocksUnsafeBootstrapPath),
            ("Overlay sync overwrites and preserves destination-only files", OverlaySyncWorks),
            ("Rejects source changes after preview", RejectsChangedSource),
            ("Persists split settings round trip", PersistsSettings),
            ("Migrates legacy combined settings", MigratesLegacySettings),
            ("Allows editing and removing every transfer directory", AllowsEditingEveryTransferDirectory),
            ("Blocks missing BbxCommon package dependencies", BlocksMissingDependencies),
            ("Rejects relative path traversal", RejectsPathTraversal)
        };

        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception exception)
            {
                failed++;
                Console.WriteLine($"FAIL {test.Name}");
                Console.WriteLine(exception);
            }
        }

        Console.WriteLine($"{tests.Length - failed}/{tests.Length} tests passed.");
        return failed == 0 ? 0 : 1;
    }

    private static Task DiscoversArbitraryGameNames()
    {
        using var workspace = new TestWorkspace();
        workspace.CreateUnityProject("FirstGame");
        workspace.CreateUnityProject("AnotherTotallyDifferentName");
        Directory.CreateDirectory(Path.Combine(workspace.Root, "Tools"));

        var projects = new ProjectLocator().DiscoverUnityProjects(workspace.Root);
        Assert.Equal(2, projects.Count);
        Assert.True(projects.Any(path => path.EndsWith("FirstGame", StringComparison.Ordinal)));
        Assert.True(projects.Any(path => path.EndsWith("AnotherTotallyDifferentName", StringComparison.Ordinal)));
        return Task.CompletedTask;
    }

    private static Task CreatesContextFromProjectRoot()
    {
        using var workspace = new TestWorkspace();
        var expected = workspace.CreateRepository("DifferentRepository", "DifferentGameName");

        var actual = new ProjectLocator().CreateContextFromProjectRoot(expected.RepositoryRoot);

        Assert.Equal(expected.RepositoryRoot, actual.RepositoryRoot);
        Assert.Equal(expected.UnityProjectRoot, actual.UnityProjectRoot);
        Assert.Equal("DifferentGameName", actual.DisplayName);
        return Task.CompletedTask;
    }

    private static Task CreatesBootstrapContext()
    {
        using var workspace = new TestWorkspace();
        var root = Path.Combine(workspace.Root, "EmptyGame");
        Directory.CreateDirectory(root);

        var actual = new ProjectLocator().CreateDestinationContextFromProjectRoot(root);

        Assert.Equal(root, actual.RepositoryRoot);
        Assert.Equal(Path.Combine(root, "EmptyGame"), actual.UnityProjectRoot);
        Assert.Equal("EmptyGame", actual.DisplayName);
        return Task.CompletedTask;
    }

    private static Task DiscoversUnityEditors()
    {
        using var workspace = new TestWorkspace();
        var hubSettings = Path.Combine(workspace.Root, "UnityHub");
        var editorRoot = Path.Combine(workspace.Root, "Editors");
        Directory.CreateDirectory(hubSettings);
        foreach (var version in new[] { "2021.3.0f1", "2022.2.7f1c1" })
        {
            var editorDirectory = Path.Combine(editorRoot, version, "Editor");
            Directory.CreateDirectory(editorDirectory);
            File.WriteAllText(Path.Combine(editorDirectory, "Unity.exe"), string.Empty);
        }

        File.WriteAllText(
            Path.Combine(hubSettings, "secondaryInstallPath.json"),
            System.Text.Json.JsonSerializer.Serialize(editorRoot));

        var editors = new UnityEditorLocator(
                hubSettings,
                includeDefaultRoots: false)
            .DiscoverInstalledEditors();

        Assert.Equal(2, editors.Count);
        Assert.Equal("2022.2.7f1c1", editors[0].Version);
        Assert.True(editors.All(editor => File.Exists(editor.ExecutablePath)));
        return Task.CompletedTask;
    }

    private static Task DiscoversCurrentHubEditors()
    {
        var hubSettings = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UnityHub");
        var secondaryPathFile = Path.Combine(
            hubSettings,
            "secondaryInstallPath.json");
        if (!File.Exists(secondaryPathFile))
        {
            return Task.CompletedTask;
        }

        var editorRoot = System.Text.Json.JsonSerializer.Deserialize<string>(
            File.ReadAllText(secondaryPathFile));
        if (string.IsNullOrWhiteSpace(editorRoot) || !Directory.Exists(editorRoot))
        {
            return Task.CompletedTask;
        }

        var expectedVersions = Directory
            .EnumerateDirectories(editorRoot)
            .Where(directory => File.Exists(Path.Combine(
                directory,
                "Editor",
                "Unity.exe")))
            .Select(directory => new DirectoryInfo(directory).Name)
            .ToList();
        if (expectedVersions.Count == 0)
        {
            return Task.CompletedTask;
        }

        var discovered = new UnityEditorLocator().DiscoverInstalledEditors();
        foreach (var version in expectedVersions)
        {
            Assert.True(discovered.Any(editor => editor.Version.Equals(
                version,
                StringComparison.OrdinalIgnoreCase)));
        }

        return Task.CompletedTask;
    }

    private static Task MapsProjectRelativeTemplates()
    {
        var repositoryItem = new SyncItem();
        SyncPathTemplate.ApplyProjectRelativePath(repositoryItem, "Tools/TaskEditor");
        Assert.Equal(PathBaseKind.RepositoryRoot, repositoryItem.SourceBase);
        Assert.Equal("Tools/TaskEditor", repositoryItem.SourceRelativePath);
        Assert.Equal("Tools/TaskEditor", SyncPathTemplate.ToProjectRelativePath(repositoryItem));

        var gameItem = new SyncItem();
        SyncPathTemplate.ApplyProjectRelativePath(
            gameItem,
            "{GameProject}/Assets/Scripts/BbxCommon");
        Assert.Equal(PathBaseKind.UnityProjectRoot, gameItem.SourceBase);
        Assert.Equal("Assets/Scripts/BbxCommon", gameItem.SourceRelativePath);
        Assert.Equal(
            "{GameProject}/Assets/Scripts/BbxCommon",
            SyncPathTemplate.ToProjectRelativePath(gameItem));
        return Task.CompletedTask;
    }

    private static Task CreatesSharedConfigDefaults()
    {
        var items = new ProjectLocator().CreateDefaultSyncItems();
        var bbxCommon = items.Single(item => item.Id == "bbxcommon-source");
        var codex = items.Single(item => item.Id == "codex-project-config");

        Assert.Contains(
            "{GameProject}/AutoDoc/UIItem",
            bbxCommon.WhitelistPaths.Select(path => path.RelativePath));
        Assert.Contains(
            "{GameProject}/.codex",
            codex.WhitelistPaths.Select(path => path.RelativePath));
        Assert.Contains(
            "{GameProject}/AGENTS.md",
            codex.WhitelistPaths.Select(path => path.RelativePath));
        Assert.Contains(
            "{GameProject}/AutoDoc/CleanupTempDocs.bat",
            codex.WhitelistPaths.Select(path => path.RelativePath));
        return Task.CompletedTask;
    }

    private static Task LoadsSettingsWindow()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var dialog = new SettingsDialog
                {
                    DataContext = new MainViewModel()
                };
                dialog.Show();
                dialog.UpdateLayout();
                dialog.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new InvalidOperationException("Settings window failed to load.", failure);
        }

        return Task.CompletedTask;
    }

    private static Task LoadsSyncItemDialog()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var item = CreateToolsItem();
                item.WhitelistPaths =
                [
                    new SyncPathEntry
                    {
                        RelativePath = "Tools",
                        ManualExcludePatterns = ["Temp/"]
                    },
                    new SyncPathEntry
                    {
                        RelativePath = "{GameProject}/Assets/Scripts/BbxCommon",
                        ManualExcludePatterns = ["Generated/"]
                    }
                ];
                var dialog = new SyncItemDialog(
                    item,
                    new ProjectContext
                    {
                        DisplayName = "Source",
                        RepositoryRoot = "D:\\Source",
                        UnityProjectRoot = "D:\\Source\\Game"
                    },
                    new DialogService());

                Assert.Equal(2, dialog.WhitelistPaths.Count);
                Assert.Equal(1, dialog.WhitelistPaths[0].ManualRules.Count);
                Assert.Equal(1, dialog.WhitelistPaths[1].ManualRules.Count);
                dialog.Show();
                dialog.UpdateLayout();
                dialog.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new InvalidOperationException(
                "Transfer directory editor failed to load.",
                failure);
        }

        return Task.CompletedTask;
    }

    private static Task LoadsMainWindow()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                window.UpdateLayout();
                window.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new InvalidOperationException("Main window failed to load.", failure);
        }

        return Task.CompletedTask;
    }

    private static Task LoadsProjectDialog()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var dialog = new ProjectDialog(
                    new ProjectContext
                    {
                        DisplayName = "NewGame",
                        RepositoryRoot = "C:\\Games\\NewGame",
                        UnityProjectRoot = "C:\\Games\\NewGame\\NewGame"
                    });
                dialog.Show();
                dialog.UpdateLayout();
                dialog.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new InvalidOperationException("Project dialog failed to load.", failure);
        }

        return Task.CompletedTask;
    }

    private static Task PreviewReportsIncompleteConfiguration()
    {
        var viewModel = new MainViewModel();

        Assert.True(viewModel.PreviewCommand.CanExecute(null));
        viewModel.PreviewCommand.Execute(null);

        Assert.Equal("Preview configuration incomplete.", viewModel.StatusText);
        Assert.True(viewModel.Messages.Any(
            message => message.Contains("main project root", StringComparison.OrdinalIgnoreCase)));
        Assert.True(viewModel.Messages.Any(
            message => message.Contains("destination project", StringComparison.OrdinalIgnoreCase)));
        return Task.CompletedTask;
    }

    private static async Task LoadsStandardRootSettings()
    {
        var repository = new SettingsRepository();

        Assert.Equal(
            Path.Combine(AppContext.BaseDirectory, "BbxDeployer.sync-items.json"),
            repository.SyncItemsPath);
        Assert.Equal(
            Path.Combine(AppContext.BaseDirectory, "BbxDeployer.projects.json"),
            repository.ProjectsPath);
        var settings = await repository.LoadAsync();

        Assert.NotNull(settings);
        Assert.NotNull(settings!.Source);
        Assert.Equal("D:\\Git\\Chaos-Combat", settings.Source!.RepositoryRoot);
        Assert.Equal("D:\\Git\\PressAnyKey", settings.Targets.Single().RepositoryRoot);
        Assert.True(settings.SyncItems.Any(item => item.Id == "codex-project-config"));
        Assert.Contains(
            "{GameProject}/AutoDoc/UIItem",
            settings.SyncItems
                .Single(item => item.Id == "bbxcommon-source")
                .WhitelistPaths
                .Select(path => path.RelativePath));
        foreach (var item in settings.SyncItems.Where(item =>
                     item.Id is "bbxcommon-source" or "codex-project-config"))
        {
            foreach (var path in item.WhitelistPaths)
            {
                var expanded = item.Clone();
                SyncPathTemplate.ApplyProjectRelativePath(expanded, path.RelativePath);
                var sourceBase = PathService.ResolveBase(settings.Source, expanded.SourceBase);
                var sourcePath = PathService.ResolveInside(
                    sourceBase,
                    expanded.SourceRelativePath);
                Assert.True(
                    Directory.Exists(sourcePath) || File.Exists(sourcePath),
                    $"Configured source path does not exist: {sourcePath}");
            }
        }
    }

    private static Task PromotesDestinationToSource()
    {
        using var workspace = new TestWorkspace();
        var main = workspace.CreateRepository("MainRepository", "MainGame");
        var destination = workspace.CreateRepository("DestinationRepository", "OtherGame");
        var viewModel = new MainViewModel
        {
            SourceRepositoryRoot = main.RepositoryRoot,
            SourceUnityProjectRoot = main.UnityProjectRoot
        };
        var destinationViewModel = new TargetProjectViewModel(destination);
        viewModel.Targets.Add(destinationViewModel);

        viewModel.SelectAsSourceCommand.Execute(destinationViewModel);

        Assert.Equal(destination.RepositoryRoot, viewModel.SourceRepositoryRoot);
        Assert.Equal(destination.UnityProjectRoot, viewModel.SourceUnityProjectRoot);
        Assert.Equal(1, viewModel.Targets.Count);
        Assert.Equal(main.RepositoryRoot, viewModel.Targets.Single().RepositoryRoot);
        return Task.CompletedTask;
    }

    private static Task TargetCardCommandsUseProjectParameter()
    {
        var first = new TargetProjectViewModel(new ProjectContext
        {
            DisplayName = "First",
            RepositoryRoot = "C:\\Projects\\First",
            UnityProjectRoot = "C:\\Projects\\First\\Game"
        });
        var second = new TargetProjectViewModel(new ProjectContext
        {
            DisplayName = "Second",
            RepositoryRoot = "C:\\Projects\\Second",
            UnityProjectRoot = "C:\\Projects\\Second\\Game"
        });
        var viewModel = new MainViewModel();
        viewModel.Targets.Add(first);
        viewModel.Targets.Add(second);
        viewModel.SelectedTarget = first;

        Assert.False(viewModel.RemoveTargetCommand.CanExecute(null));
        Assert.True(viewModel.RemoveTargetCommand.CanExecute(second));
        Assert.True(viewModel.EditTargetCommand.CanExecute(second));
        viewModel.RemoveTargetCommand.Execute(second);

        Assert.Equal(1, viewModel.Targets.Count);
        Assert.True(ReferenceEquals(first, viewModel.Targets.Single()));
        Assert.True(ReferenceEquals(first, viewModel.SelectedTarget));
        return Task.CompletedTask;
    }

    private static async Task AppliesNestedGitIgnoreRules()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "GameA");
        var target = workspace.CreateRepository("Target", "GameB");
        var tools = Path.Combine(source.RepositoryRoot, "Tools");
        Directory.CreateDirectory(Path.Combine(tools, "Debug"));
        Directory.CreateDirectory(Path.Combine(tools, "Retriever", ".venv"));
        Directory.CreateDirectory(Path.Combine(tools, "Retriever", "src"));
        await File.WriteAllTextAsync(Path.Combine(source.RepositoryRoot, ".gitignore"),
            "[Dd]ebug/\n*.py[cod]\n!keep.pyc\n");
        await File.WriteAllTextAsync(Path.Combine(tools, "Retriever", ".gitignore"), ".venv/\n");
        await File.WriteAllTextAsync(Path.Combine(tools, "keep.txt"), "keep");
        await File.WriteAllTextAsync(Path.Combine(tools, "module.pyc"), "drop");
        await File.WriteAllTextAsync(Path.Combine(tools, "keep.pyc"), "keep");
        await File.WriteAllTextAsync(Path.Combine(tools, "Debug", "drop.txt"), "drop");
        await File.WriteAllTextAsync(Path.Combine(tools, "Retriever", ".venv", "drop.bin"), "drop");
        await File.WriteAllTextAsync(Path.Combine(tools, "Retriever", "src", "keep.cs"), "keep");

        var item = CreateToolsItem();
        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        var included = preview.Files.Select(file => file.TargetRelativePath).ToHashSet();
        Assert.Contains("Tools/keep.txt", included);
        Assert.Contains("Tools/keep.pyc", included);
        Assert.Contains("Tools/Retriever/src/keep.cs", included);
        Assert.DoesNotContain("Tools/module.pyc", included);
        Assert.DoesNotContain("Tools/Debug/drop.txt", included);
        Assert.DoesNotContain("Tools/Retriever/.venv/drop.bin", included);
        Assert.True(preview.ExcludedFileCount >= 1);
    }

    private static async Task SyncsGitIgnoredDeployerProjectList()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "GameA");
        var target = workspace.CreateRepository("Target", "GameB");
        var deployer = Path.Combine(source.RepositoryRoot, "Tools", "BbxDeployer");
        Directory.CreateDirectory(deployer);
        await File.WriteAllTextAsync(
            Path.Combine(deployer, ".gitignore"),
            "/BbxDeployer.projects.json\n/BbxDeployer.projects.json.*.tmp\n");
        await File.WriteAllTextAsync(
            Path.Combine(deployer, "BbxDeployer.projects.json"),
            "{}");
        await File.WriteAllTextAsync(
            Path.Combine(deployer, "BbxDeployer.projects.json.save.tmp"),
            "temporary");
        await File.WriteAllTextAsync(
            Path.Combine(deployer, "BbxDeployer.sync-items.json"),
            """{"SyncItems":[]}""");
        await File.WriteAllTextAsync(
            Path.Combine(deployer, "BbxDeployer.exe"),
            "application");

        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [CreateToolsItem()],
            null,
            CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        var included = preview.Files
            .Select(file => file.TargetRelativePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Tools/BbxDeployer/BbxDeployer.exe", included);
        Assert.Contains(
            "Tools/BbxDeployer/BbxDeployer.sync-items.json",
            included);
        Assert.Contains(
            "Tools/BbxDeployer/BbxDeployer.projects.json",
            included);
        Assert.DoesNotContain(
            "Tools/BbxDeployer/BbxDeployer.projects.json.save.tmp",
            included);

        var customTools = CreateToolsItem();
        customTools.Id = "custom-tools";
        var customPreview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [customTools],
            null,
            CancellationToken.None);
        Assert.False(customPreview.Files.Any(file =>
            file.TargetRelativePath.Equals(
                "Tools/BbxDeployer/BbxDeployer.projects.json",
                StringComparison.OrdinalIgnoreCase)));

        var manuallyExcludedTools = CreateToolsItem();
        manuallyExcludedTools.ManualExcludePatterns.Add(
            "BbxDeployer/BbxDeployer.projects.json");
        var manuallyExcludedPreview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [manuallyExcludedTools],
            null,
            CancellationToken.None);
        Assert.False(manuallyExcludedPreview.Files.Any(file =>
            file.TargetRelativePath.Equals(
                "Tools/BbxDeployer/BbxDeployer.projects.json",
                StringComparison.OrdinalIgnoreCase)));
    }

    private static async Task AlwaysIncludesConfiguredDirectories()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "GameA");
        var target = workspace.CreateRepository("Target", "GameB");
        var tools = Path.Combine(source.RepositoryRoot, "Tools");
        Directory.CreateDirectory(tools);
        await File.WriteAllTextAsync(Path.Combine(tools, "included.txt"), "included");

        var item = CreateToolsItem();
        item.Enabled = false;
        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        Assert.True(preview.Items.Single().Enabled);
        Assert.True(preview.Files.Any(
            file => file.TargetRelativePath == "Tools/included.txt"));
    }

    private static async Task ExpandsMultipleWhitelistPaths()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "GameA");
        var target = workspace.CreateRepository("Target", "GameB");
        var first = Path.Combine(source.RepositoryRoot, "Tools", "First");
        var second = Path.Combine(source.UnityProjectRoot, "Assets", "Shared");
        Directory.CreateDirectory(Path.Combine(first, "Ignored"));
        Directory.CreateDirectory(Path.Combine(second, "Hidden"));
        await File.WriteAllTextAsync(Path.Combine(first, "keep.txt"), "keep");
        await File.WriteAllTextAsync(Path.Combine(first, "Ignored", "drop.txt"), "drop");
        await File.WriteAllTextAsync(Path.Combine(second, "keep.txt"), "keep");
        await File.WriteAllTextAsync(Path.Combine(second, "Hidden", "drop.txt"), "drop");

        var item = new SyncItem
        {
            DisplayName = "Shared Paths",
            WhitelistPaths =
            [
                new SyncPathEntry
                {
                    RelativePath = "Tools/First",
                    ManualExcludePatterns = ["Ignored/"]
                },
                new SyncPathEntry
                {
                    RelativePath = "{GameProject}/Assets/Shared",
                    ManualExcludePatterns = ["Hidden/"]
                }
            ]
        };
        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        Assert.Equal(2, preview.Items.Count);
        var included = preview.Files
            .Select(file => file.TargetRelativePath)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Tools/First/keep.txt", included);
        Assert.Contains("Assets/Shared/keep.txt", included);
        Assert.DoesNotContain("Tools/First/Ignored/drop.txt", included);
        Assert.DoesNotContain("Assets/Shared/Hidden/drop.txt", included);
    }

    private static async Task PlansSingleFileWhitelist()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "GameA");
        var target = workspace.CreateRepository("Target", "GameB");
        var agentsPath = Path.Combine(source.UnityProjectRoot, "AGENTS.md");
        await File.WriteAllTextAsync(agentsPath, "shared agent instructions");
        var item = new SyncItem
        {
            Id = "codex-project-config",
            DisplayName = "Codex Project Configuration",
            WhitelistPaths =
            [
                new SyncPathEntry
                {
                    RelativePath = "{GameProject}/AGENTS.md"
                }
            ]
        };

        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        Assert.Equal(1, preview.Files.Count);
        Assert.Equal("AGENTS.md", preview.Files.Single().TargetRelativePath);
        Assert.Equal(agentsPath, preview.Files.Single().SourcePath);
    }

    private static async Task ManualBlacklistHasPriority()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "GameA");
        var target = workspace.CreateRepository("Target", "GameB");
        var tools = Path.Combine(source.RepositoryRoot, "Tools");
        Directory.CreateDirectory(Path.Combine(tools, "blocked"));
        await File.WriteAllTextAsync(
            Path.Combine(source.RepositoryRoot, ".gitignore"),
            "blocked/\n!blocked/keep.txt\n");
        await File.WriteAllTextAsync(Path.Combine(tools, "blocked", "keep.txt"), "keep");

        var item = CreateToolsItem();
        item.ManualExcludePatterns.Add("blocked/");
        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);

        Assert.False(preview.Files.Any(
            file => file.TargetRelativePath.EndsWith("blocked/keep.txt", StringComparison.Ordinal)));
    }

    private static async Task IgnoresLegacyImportedIgnoreFiles()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "GameA");
        var target = workspace.CreateRepository("Target", "GameB");
        var tools = Path.Combine(source.RepositoryRoot, "Tools");
        var nested = Path.Combine(tools, "Nested");
        Directory.CreateDirectory(nested);
        var nestedIgnore = Path.Combine(nested, ".gitignore");
        var additionalIgnore = Path.Combine(source.RepositoryRoot, "sync.gitignore");
        await File.WriteAllTextAsync(nestedIgnore, "*.tmp\n");
        await File.WriteAllTextAsync(additionalIgnore, "!Nested/keep.tmp\n");
        await File.WriteAllTextAsync(Path.Combine(nested, "keep.tmp"), "keep");
        await File.WriteAllTextAsync(Path.Combine(nested, "drop.tmp"), "drop");

        var item = CreateToolsItem();
        item.AdditionalIgnoreFiles.Add(new IgnoreFileReference
        {
            FilePath = additionalIgnore,
            BaseDirectory = tools
        });
        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        var included = preview.Files.Select(file => file.TargetRelativePath).ToHashSet();
        Assert.DoesNotContain("Tools/Nested/keep.tmp", included);
        Assert.DoesNotContain("Tools/Nested/drop.tmp", included);
        Assert.True(preview.RuleFiles.Any(snapshot =>
            snapshot.Path.Equals(nestedIgnore, StringComparison.OrdinalIgnoreCase)));
        Assert.False(preview.RuleFiles.Any(snapshot =>
            snapshot.Path.Equals(additionalIgnore, StringComparison.OrdinalIgnoreCase)));
    }

    private static async Task PrunesIgnoredDirectories()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "GameA");
        var target = workspace.CreateRepository("Target", "GameB");
        var tools = Path.Combine(source.RepositoryRoot, "Tools");
        var ignored = Path.Combine(tools, ".venv");
        Directory.CreateDirectory(ignored);
        await File.WriteAllTextAsync(
            Path.Combine(source.RepositoryRoot, ".gitignore"),
            ".venv/\n");
        await File.WriteAllTextAsync(Path.Combine(tools, "keep.txt"), "keep");
        for (var index = 0; index < 500; index++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(ignored, $"ignored-{index}.bin"),
                "ignored");
        }

        var progressUpdates = new List<PreviewProgress>();
        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [CreateToolsItem()],
            new InlineProgress<PreviewProgress>(progressUpdates.Add),
            CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        Assert.Equal(1, preview.Files.Count);
        Assert.True(progressUpdates.Any(update =>
            !update.IsIndeterminate
            && update.Message.EndsWith("1 file checked.", StringComparison.Ordinal)));
        var completed = progressUpdates.Last(update => !update.IsIndeterminate);
        Assert.Equal(preview.Files.Count, completed.TotalFiles);
        Assert.Equal(completed.TotalFiles, completed.CompletedFiles);
        Assert.Equal(100d, completed.Percentage);
    }

    private static async Task DoesNotSyncCompanionMeta()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "SourceGame");
        var target = workspace.CreateRepository("Target", "TargetGame");
        var library = Path.Combine(source.UnityProjectRoot, "Assets", "Scripts", "Shared");
        Directory.CreateDirectory(library);
        await File.WriteAllTextAsync(Path.Combine(library, "Code.cs"), "// code");
        await File.WriteAllTextAsync(library + ".meta", "guid: test");

        var item = new SyncItem
        {
            Id = "custom-shared",
            DisplayName = "Shared",
            SourceBase = PathBaseKind.UnityProjectRoot,
            SourceRelativePath = "Assets/Scripts/Shared",
            TargetBase = PathBaseKind.UnityProjectRoot,
            TargetRelativePath = "Assets/Scripts/Shared",
            IncludeCompanionMeta = true
        };

        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        Assert.False(preview.Files.Any(
            file => file.TargetRelativePath == "Assets/Scripts/Shared.meta"));
    }

    private static async Task ReportsPreviewFileCountProgress()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "GameA");
        var firstTarget = workspace.CreateRepository("FirstTarget", "GameB");
        var secondTarget = workspace.CreateRepository("SecondTarget", "GameC");
        var tools = Path.Combine(source.RepositoryRoot, "Tools");
        await File.WriteAllTextAsync(Path.Combine(tools, "first.txt"), "first");
        await File.WriteAllTextAsync(Path.Combine(tools, "second.txt"), "second");
        var updates = new List<PreviewProgress>();

        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [firstTarget, secondTarget],
            [CreateToolsItem()],
            new InlineProgress<PreviewProgress>(updates.Add),
            CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        Assert.False(updates.First().IsIndeterminate);
        Assert.Equal(0d, updates.First().Percentage);
        Assert.True(updates.Any(update =>
            update.Percentage > 0 && update.Percentage < 100));
        for (var index = 1; index < updates.Count; index++)
        {
            Assert.True(
                updates[index].Percentage >= updates[index - 1].Percentage,
                "Preview progress must move forward without resetting.");
        }

        var determinate = updates.Where(update => !update.IsIndeterminate).ToList();
        Assert.True(determinate.Count > 0);
        Assert.Equal(preview.Files.Count * preview.Targets.Count, determinate.Last().TotalFiles);
        Assert.Equal(determinate.Last().TotalFiles, determinate.Last().CompletedFiles);
        Assert.Equal(100d, determinate.Last().Percentage);
    }

    private static Task CalculatesSyncProgressByFileCount()
    {
        var progress = new SyncProgress
        {
            CompletedFiles = 1,
            TotalFiles = 4,
            CompletedBytes = 900,
            TotalBytes = 1000
        };

        Assert.Equal(25d, progress.Percentage);
        return Task.CompletedTask;
    }

    private static async Task ReportsSyncValidationAndCopyProgress()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("ProgressSource", "SourceGame");
        var target = workspace.CreateRepository("ProgressTarget", "TargetGame");
        var sourceTools = Path.Combine(source.RepositoryRoot, "Tools");
        Directory.CreateDirectory(sourceTools);
        await File.WriteAllTextAsync(Path.Combine(sourceTools, "first.txt"), "first");
        await File.WriteAllTextAsync(Path.Combine(sourceTools, "second.txt"), "second");

        var ignoreLoader = new IgnoreRuleLoader();
        var preview = await CreatePlanner(ignoreLoader).CreatePreviewAsync(
            source,
            [target],
            [CreateToolsItem()],
            null,
            CancellationToken.None);
        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));

        var updates = new List<SyncProgress>();
        var result = await new SyncExecutor(ignoreLoader).ExecuteAsync(
            preview,
            new InlineProgress<SyncProgress>(updates.Add),
            CancellationToken.None);

        Assert.True(result.Targets.Single().Succeeded);
        Assert.True(updates.Count > 2);
        Assert.Equal("Preview Validation", updates.First().SyncItemName);
        Assert.Equal(0d, updates.First().Percentage);
        Assert.True(updates.Any(update =>
            update.SyncItemName == "Preview Validation"
            && update.Percentage > 0
            && update.Percentage <= 10));
        Assert.True(updates.Any(update =>
            update.SyncItemName == "Shared Tools"
            && update.Percentage > 10));
        Assert.Equal(100d, updates.Last().Percentage);
        for (var index = 1; index < updates.Count; index++)
        {
            Assert.True(
                updates[index].Percentage >= updates[index - 1].Percentage,
                "Sync progress must move forward without resetting.");
        }
    }

    private static async Task ClassifiesNewProject()
    {
        using var workspace = new TestWorkspace();
        var fixture = new PreviewStatusFixture(workspace);

        var target = await PreviewTarget(fixture, fixture.NewProject);

        Assert.Equal(TargetSyncStatus.NewProject, target.Status);
        Assert.Equal(1, target.ExistingPathCount);
        Assert.Equal(2, target.MissingPathCount);
    }

    private static async Task ClassifiesWaitForSync()
    {
        using var workspace = new TestWorkspace();
        var fixture = new PreviewStatusFixture(workspace);

        var target = await PreviewTarget(fixture, fixture.WaitForSync);

        Assert.Equal(TargetSyncStatus.WaitForSync, target.Status);
        Assert.Equal(2, target.ExistingPathCount);
        Assert.Equal(1, target.MissingPathCount);
        Assert.True(target.FilesNeedingSyncCount > 0);
    }

    private static async Task ClassifiesSynchronized()
    {
        using var workspace = new TestWorkspace();
        var fixture = new PreviewStatusFixture(workspace);

        var target = await PreviewTarget(fixture, fixture.Synchronized);

        Assert.Equal(TargetSyncStatus.Synchronized, target.Status);
        Assert.Equal(0, target.FilesNeedingSyncCount);
        Assert.Equal(0, target.RiskFiles.Count);
    }

    private static async Task ClassifiesWarning()
    {
        using var workspace = new TestWorkspace();
        var fixture = new PreviewStatusFixture(workspace);

        var target = await PreviewTarget(fixture, fixture.Warning);

        Assert.Equal(TargetSyncStatus.Warning, target.Status);
        Assert.Equal(1, target.RiskFiles.Count);
        Assert.Equal("SharedB/b.txt", target.RiskFiles.Single().TargetRelativePath);
        Assert.True(
            target.RiskFiles.Single().TargetLastWriteTimeUtc
            > target.RiskFiles.Single().SourceLastWriteTimeUtc);
    }

    private static Task MapsPreviewStatusColors()
    {
        var colors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var status in Enum.GetValues<TargetSyncStatus>())
        {
            var preview = new TargetPreview
            {
                Target = new ProjectContext
                {
                    DisplayName = "Target",
                    RepositoryRoot = "C:/Target",
                    UnityProjectRoot = "C:/Target/Game"
                },
                Status = status
            };
            if (status == TargetSyncStatus.Warning)
            {
                preview.RiskFiles.Add(new TargetRiskFile
                {
                    TargetRelativePath = "Shared/file.txt",
                    SourceLastWriteTimeUtc = DateTime.UnixEpoch,
                    TargetLastWriteTimeUtc = DateTime.UnixEpoch.AddMinutes(1)
                });
            }

            var viewModel = new TargetProjectViewModel(preview.Target);
            viewModel.ApplyPreview(preview);
            colors.Add(viewModel.StatusColor);

            if (status == TargetSyncStatus.Warning)
            {
                Assert.True(viewModel.HasRiskFiles);
            }
        }

        Assert.Equal(4, colors.Count);
        return Task.CompletedTask;
    }

    private static async Task SkipsUnityDiscoveryWithoutNewProject()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "SourceGame");
        var target = workspace.CreateRepository("Target", "TargetGame");
        await File.WriteAllTextAsync(
            Path.Combine(source.RepositoryRoot, "Tools", "shared.txt"),
            "shared");
        await File.WriteAllTextAsync(
            Path.Combine(target.RepositoryRoot, "Tools", "shared.txt"),
            "shared");
        var sourceFile = new FileInfo(Path.Combine(source.RepositoryRoot, "Tools", "shared.txt"));
        File.SetLastWriteTimeUtc(
            Path.Combine(target.RepositoryRoot, "Tools", "shared.txt"),
            sourceFile.LastWriteTimeUtc);
        var editorLocator = new FakeUnityEditorLocator([]);

        var preview = await CreatePlanner(
                unityEditorLocator: editorLocator)
            .CreatePreviewAsync(
                source,
                [target],
                [CreateToolsItem()],
                null,
                CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        Assert.Equal(TargetSyncStatus.Synchronized, preview.Targets.Single().Status);
        Assert.Equal(0, editorLocator.DiscoveryCount);
        Assert.Equal(0, preview.UnityEditors.Count);
    }

    private static async Task DefaultsToMainUnityVersion()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "SourceGame");
        await File.WriteAllTextAsync(
            Path.Combine(source.RepositoryRoot, "Tools", "shared.txt"),
            "shared");
        var targetRepository = Path.Combine(workspace.Root, "NewTarget");
        Directory.CreateDirectory(targetRepository);
        var target = new ProjectContext
        {
            DisplayName = "NewGame",
            RepositoryRoot = targetRepository,
            UnityProjectRoot = Path.Combine(targetRepository, "NewGame")
        };
        var editors = new[]
        {
            new UnityEditorInstallation
            {
                Version = "2021.3.0f1",
                ExecutablePath = "C:\\Unity\\2021\\Unity.exe"
            },
            new UnityEditorInstallation
            {
                Version = "2022.2.7f1c1",
                ExecutablePath = "C:\\Unity\\2022\\Unity.exe"
            }
        };
        var editorLocator = new FakeUnityEditorLocator(editors);

        var preview = await CreatePlanner(
                unityEditorLocator: editorLocator)
            .CreatePreviewAsync(
                source,
                [target],
                [CreateToolsItem()],
                null,
                CancellationToken.None);
        var targetPreview = preview.Targets.Single();

        Assert.Equal(1, editorLocator.DiscoveryCount);
        Assert.Equal(2, preview.UnityEditors.Count);
        Assert.True(targetPreview.RequiresUnityProjectCreation);
        Assert.Equal("2022.2.7f1c1", targetPreview.Target.UnityEditorVersion);

        var viewModel = new TargetProjectViewModel(target);
        var changed = 0;
        viewModel.UnityEditorVersionChanged += (_, _) => changed++;
        viewModel.ApplyPreview(targetPreview, preview.UnityEditors);
        Assert.True(viewModel.IsUnityVersionSelectionVisible);
        Assert.Equal("2022.2.7f1c1", viewModel.SelectedUnityEditor?.Version);
        Assert.Equal(0, changed);

        viewModel.SelectedUnityEditor = viewModel.AvailableUnityEditors.First(editor =>
            editor.Version == "2021.3.0f1");
        Assert.Equal(1, changed);
        Assert.Equal("2021.3.0f1", viewModel.Model.UnityEditorVersion);
    }

    private static async Task BootstrapsEmptyUnityProject()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("SourceRepository", "SourceGame");
        var sourceTools = Path.Combine(source.RepositoryRoot, "Tools");
        await File.WriteAllTextAsync(Path.Combine(sourceTools, "shared.txt"), "shared");
        await File.WriteAllTextAsync(
            Path.Combine(source.UnityProjectRoot, "ProjectSettings", "CustomSettings.asset"),
            "source settings");

        var targetRepository = Path.Combine(workspace.Root, "EmptyRepository");
        Directory.CreateDirectory(targetRepository);
        var target = new ProjectContext
        {
            DisplayName = "CompletelyDifferentGame",
            RepositoryRoot = targetRepository,
            UnityProjectRoot = Path.Combine(targetRepository, "CompletelyDifferentGame")
        };
        var item = CreateToolsItem();
        var ignoreLoader = new IgnoreRuleLoader();

        var preview = await CreatePlanner(ignoreLoader).CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);
        var targetPreview = preview.Targets.Single();

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        Assert.True(targetPreview.RequiresUnityBootstrap);
        Assert.Equal(TargetSyncStatus.NewProject, targetPreview.Status);
        Assert.True(targetPreview.UnityBootstrapFiles.Any(
            file => file.TargetRelativePath == "ProjectSettings/ProjectVersion.txt"));
        Assert.True(targetPreview.UnityBootstrapFiles.Any(
            file => file.TargetRelativePath == "Packages/manifest.json"));

        var result = await new SyncExecutor(ignoreLoader).ExecuteAsync(
            preview,
            null,
            CancellationToken.None);

        Assert.True(result.Targets.Single().Succeeded);
        Assert.True(new ProjectLocator().IsUnityProject(target.UnityProjectRoot));
        Assert.Equal(
            "shared",
            await File.ReadAllTextAsync(Path.Combine(targetRepository, "Tools", "shared.txt")));
        Assert.Equal(
            "source settings",
            await File.ReadAllTextAsync(Path.Combine(
                target.UnityProjectRoot,
                "ProjectSettings",
                "CustomSettings.asset")));

        var followUp = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);
        Assert.False(followUp.Targets.Single().RequiresUnityBootstrap);
        Assert.Equal(TargetSyncStatus.Synchronized, followUp.Targets.Single().Status);
    }

    private static async Task PreservesBootstrapFiles()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("SourceRepository", "SourceGame");
        await File.WriteAllTextAsync(
            Path.Combine(source.UnityProjectRoot, "ProjectSettings", "CustomSettings.asset"),
            "source settings");
        await File.WriteAllTextAsync(
            Path.Combine(source.RepositoryRoot, "Tools", "shared.txt"),
            "shared");

        var targetRepository = Path.Combine(workspace.Root, "TargetRepository");
        var targetUnityRoot = Path.Combine(targetRepository, "TargetGame");
        Directory.CreateDirectory(Path.Combine(targetUnityRoot, "ProjectSettings"));
        await File.WriteAllTextAsync(
            Path.Combine(targetUnityRoot, "ProjectSettings", "CustomSettings.asset"),
            "target settings");
        var target = new ProjectContext
        {
            DisplayName = "TargetGame",
            RepositoryRoot = targetRepository,
            UnityProjectRoot = targetUnityRoot
        };
        var ignoreLoader = new IgnoreRuleLoader();
        var preview = await CreatePlanner(ignoreLoader).CreatePreviewAsync(
            source,
            [target],
            [CreateToolsItem()],
            null,
            CancellationToken.None);

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        Assert.False(preview.Targets.Single().UnityBootstrapFiles.Any(
            file => file.TargetRelativePath == "ProjectSettings/CustomSettings.asset"));
        await new SyncExecutor(ignoreLoader).ExecuteAsync(
            preview,
            null,
            CancellationToken.None);

        Assert.Equal(
            "target settings",
            await File.ReadAllTextAsync(Path.Combine(
                targetUnityRoot,
                "ProjectSettings",
                "CustomSettings.asset")));
    }

    private static async Task CreatesProjectWithSelectedEditor()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("SourceRepository", "SourceGame");
        var sourceManifest = Path.Combine(
            source.UnityProjectRoot,
            "Packages",
            "manifest.json");
        var sourceLock = Path.Combine(
            source.UnityProjectRoot,
            "Packages",
            "packages-lock.json");
        await File.WriteAllTextAsync(
            sourceManifest,
            """{"dependencies":{"com.example.shared":"1.0.0"}}""");
        await File.WriteAllTextAsync(sourceLock, """{"dependencies":{"source":{}}}""");
        await File.WriteAllTextAsync(
            Path.Combine(source.RepositoryRoot, "Tools", "shared.txt"),
            "shared");

        var editorRoot = Path.Combine(workspace.Root, "Editors");
        var editorPath = Path.Combine(
            editorRoot,
            "2022.2.7f1c1",
            "Editor",
            "Unity.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(editorPath)!);
        await File.WriteAllTextAsync(editorPath, string.Empty);
        var editorLocator = new UnityEditorLocator(
            additionalRoots: [editorRoot],
            includeDefaultRoots: false);

        var targetRepository = Path.Combine(workspace.Root, "NewRepository");
        Directory.CreateDirectory(targetRepository);
        var target = new ProjectContext
        {
            DisplayName = "NewGame",
            RepositoryRoot = targetRepository,
            UnityProjectRoot = Path.Combine(targetRepository, "NewGame"),
            UnityEditorVersion = "2022.2.7f1c1"
        };
        var ignoreLoader = new IgnoreRuleLoader();
        var preview = await CreatePlanner(ignoreLoader, editorLocator).CreatePreviewAsync(
            source,
            [target],
            [CreateToolsItem()],
            null,
            CancellationToken.None);
        var targetPreview = preview.Targets.Single();

        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        Assert.True(targetPreview.RequiresUnityProjectCreation);
        Assert.False(targetPreview.RequiresUnityBootstrap);
        Assert.True(targetPreview.UnityBootstrapFiles.All(file => file.Overwrite));

        var creator = new FakeUnityProjectCreator();
        var result = await new SyncExecutor(ignoreLoader, creator).ExecuteAsync(
            preview,
            null,
            CancellationToken.None);

        Assert.True(result.Targets.Single().Succeeded);
        Assert.Equal(1, creator.CallCount);
        Assert.Equal(
            await File.ReadAllTextAsync(sourceManifest),
            await File.ReadAllTextAsync(Path.Combine(
                target.UnityProjectRoot,
                "Packages",
                "manifest.json")));
        Assert.Equal(
            await File.ReadAllTextAsync(sourceLock),
            await File.ReadAllTextAsync(Path.Combine(
                target.UnityProjectRoot,
                "Packages",
                "packages-lock.json")));
        Assert.Equal(
            "shared",
            await File.ReadAllTextAsync(Path.Combine(
                target.RepositoryRoot,
                "Tools",
                "shared.txt")));
    }

    private static async Task ReportsUnityCreationFailure()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("SourceRepository", "SourceGame");
        await File.WriteAllTextAsync(
            Path.Combine(source.RepositoryRoot, "Tools", "shared.txt"),
            "shared");
        var editorRoot = Path.Combine(workspace.Root, "Editors");
        var editorPath = Path.Combine(editorRoot, "2022.2.7f1c1", "Editor", "Unity.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(editorPath)!);
        await File.WriteAllTextAsync(editorPath, string.Empty);

        var targetRepository = Path.Combine(workspace.Root, "NewRepository");
        Directory.CreateDirectory(targetRepository);
        var target = new ProjectContext
        {
            DisplayName = "NewGame",
            RepositoryRoot = targetRepository,
            UnityProjectRoot = Path.Combine(targetRepository, "NewGame"),
            UnityEditorVersion = "2022.2.7f1c1"
        };
        var ignoreLoader = new IgnoreRuleLoader();
        var preview = await CreatePlanner(
                ignoreLoader,
                new UnityEditorLocator(
                    additionalRoots: [editorRoot],
                    includeDefaultRoots: false))
            .CreatePreviewAsync(
                source,
                [target],
                [CreateToolsItem()],
                null,
                CancellationToken.None);

        var result = await new SyncExecutor(
                ignoreLoader,
                new FakeUnityProjectCreator(fail: true))
            .ExecuteAsync(preview, null, CancellationToken.None);

        Assert.False(result.Targets.Single().Succeeded);
        Assert.True(result.Targets.Single().Error?.Contains(
            "simulated Unity failure",
            StringComparison.Ordinal) == true);
        Assert.False(File.Exists(Path.Combine(targetRepository, "Tools", "shared.txt")));
    }

    private static async Task BlocksUnsafeBootstrapPath()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("SourceRepository", "SourceGame");
        await File.WriteAllTextAsync(
            Path.Combine(source.RepositoryRoot, "Tools", "shared.txt"),
            "shared");
        var targetRepository = Path.Combine(workspace.Root, "TargetRepository");
        Directory.CreateDirectory(targetRepository);
        var target = new ProjectContext
        {
            DisplayName = "NestedGame",
            RepositoryRoot = targetRepository,
            UnityProjectRoot = Path.Combine(targetRepository, "Nested", "NestedGame")
        };

        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [CreateToolsItem()],
            null,
            CancellationToken.None);

        Assert.True(preview.HasBlockingErrors);
        Assert.True(preview.Targets.Single().Errors.Any(
            error => error.Contains("bootstrap-compatible", StringComparison.Ordinal)));
    }

    private static async Task OverlaySyncWorks()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "SourceGame");
        var target = workspace.CreateRepository("Target", "TargetGame");
        var sourcePayload = Path.Combine(source.RepositoryRoot, "Payload");
        var targetPayload = Path.Combine(target.RepositoryRoot, "Payload");
        Directory.CreateDirectory(sourcePayload);
        Directory.CreateDirectory(targetPayload);
        await File.WriteAllTextAsync(Path.Combine(sourcePayload, "replace.txt"), "new");
        await File.WriteAllTextAsync(Path.Combine(sourcePayload, "added.txt"), "added");
        await File.WriteAllTextAsync(Path.Combine(targetPayload, "replace.txt"), "old");
        await File.WriteAllTextAsync(Path.Combine(targetPayload, "target-only.txt"), "keep");

        var item = new SyncItem
        {
            DisplayName = "Payload",
            SourceBase = PathBaseKind.RepositoryRoot,
            SourceRelativePath = "Payload",
            TargetBase = PathBaseKind.RepositoryRoot,
            TargetRelativePath = "Payload"
        };
        var ignoreLoader = new IgnoreRuleLoader();
        var preview = await CreatePlanner(ignoreLoader).CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);
        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));

        var result = await new SyncExecutor(ignoreLoader).ExecuteAsync(
            preview,
            null,
            CancellationToken.None);

        Assert.True(result.Targets.Single().Succeeded);
        Assert.Equal("new", await File.ReadAllTextAsync(Path.Combine(targetPayload, "replace.txt")));
        Assert.Equal("added", await File.ReadAllTextAsync(Path.Combine(targetPayload, "added.txt")));
        Assert.Equal("keep", await File.ReadAllTextAsync(Path.Combine(targetPayload, "target-only.txt")));
    }

    private static async Task RejectsChangedSource()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "SourceGame");
        var target = workspace.CreateRepository("Target", "TargetGame");
        var payload = Path.Combine(source.RepositoryRoot, "Payload");
        Directory.CreateDirectory(payload);
        var sourceFile = Path.Combine(payload, "file.txt");
        await File.WriteAllTextAsync(sourceFile, "before");

        var item = new SyncItem
        {
            DisplayName = "Payload",
            SourceBase = PathBaseKind.RepositoryRoot,
            SourceRelativePath = "Payload",
            TargetBase = PathBaseKind.RepositoryRoot,
            TargetRelativePath = "Payload"
        };
        var ignoreLoader = new IgnoreRuleLoader();
        var preview = await CreatePlanner(ignoreLoader).CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);
        await File.WriteAllTextAsync(sourceFile, "changed after preview");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SyncExecutor(ignoreLoader).ExecuteAsync(
                preview,
                null,
                CancellationToken.None));
    }

    private static async Task PersistsSettings()
    {
        using var workspace = new TestWorkspace();
        var directory = Path.Combine(workspace.Root, "settings");
        var syncItemsPath = Path.Combine(directory, "BbxDeployer.sync-items.json");
        var projectsPath = Path.Combine(directory, "BbxDeployer.projects.json");
        var legacyPath = Path.Combine(directory, "BbxDeployer.settings.json");
        var repository = new SettingsRepository(
            syncItemsPath,
            projectsPath,
            legacyPath);
        var settings = new AppSettings
        {
            Source = new ProjectContext
            {
                DisplayName = "Source",
                RepositoryRoot = "C:/Repo",
                UnityProjectRoot = "C:/Repo/Game"
            },
            Targets =
            [
                new ProjectContext
                {
                    DisplayName = "Target",
                    RepositoryRoot = "C:/Target",
                    UnityProjectRoot = "C:/Target/Game",
                    UnityEditorVersion = "2022.2.7f1c1"
                }
            ],
            SyncItems =
            [
                new SyncItem
                {
                    DisplayName = "Tools",
                    SourceRelativePath = "Tools",
                    TargetRelativePath = "Tools",
                    ManualExcludePatterns = ["Nested/blacklist/"],
                    WhitelistPaths =
                    [
                        new SyncPathEntry
                        {
                            RelativePath = "Tools/First",
                            ManualExcludePatterns = ["Cache/"]
                        },
                        new SyncPathEntry
                        {
                            RelativePath = "{GameProject}/Assets/Shared",
                            ManualExcludePatterns = ["Generated/"]
                        }
                    ]
                }
            ]
        };

        await repository.SaveAsync(settings);
        var loaded = await repository.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal("Nested/blacklist/", loaded!.SyncItems.Single().ManualExcludePatterns.Single());
        Assert.Equal(2, loaded.SyncItems.Single().WhitelistPaths.Count);
        Assert.Equal(
            "Generated/",
            loaded.SyncItems.Single().WhitelistPaths[1].ManualExcludePatterns.Single());
        Assert.Equal("2022.2.7f1c1", loaded.Targets.Single().UnityEditorVersion);
        Assert.True(File.Exists(syncItemsPath));
        Assert.True(File.Exists(projectsPath));
        Assert.False(File.Exists(legacyPath));
        Assert.False((await File.ReadAllTextAsync(syncItemsPath)).Contains(
            "C:/Repo",
            StringComparison.Ordinal));
        Assert.False((await File.ReadAllTextAsync(projectsPath)).Contains(
            "ManualExcludePatterns",
            StringComparison.Ordinal));

        var stableTimestamp = DateTime.UtcNow.AddMinutes(-10);
        File.SetLastWriteTimeUtc(syncItemsPath, stableTimestamp);
        File.SetLastWriteTimeUtc(projectsPath, stableTimestamp);
        await repository.SaveAsync(settings);
        Assert.Equal(stableTimestamp, File.GetLastWriteTimeUtc(syncItemsPath));
        Assert.Equal(stableTimestamp, File.GetLastWriteTimeUtc(projectsPath));
    }

    private static async Task MigratesLegacySettings()
    {
        using var workspace = new TestWorkspace();
        var directory = Path.Combine(workspace.Root, "migration");
        Directory.CreateDirectory(directory);
        var syncItemsPath = Path.Combine(directory, "BbxDeployer.sync-items.json");
        var projectsPath = Path.Combine(directory, "BbxDeployer.projects.json");
        var legacyPath = Path.Combine(directory, "BbxDeployer.settings.json");
        await File.WriteAllTextAsync(
            legacyPath,
            """
            {
              "Source": {
                "DisplayName": "Legacy Source",
                "RepositoryRoot": "C:/Legacy",
                "UnityProjectRoot": "C:/Legacy/Game"
              },
              "Targets": [],
              "SyncItems": [
                {
                  "Id": "legacy-tools",
                  "DisplayName": "Legacy Tools",
                  "SourceRelativePath": "Tools",
                  "TargetRelativePath": "Tools"
                }
              ]
            }
            """);
        var repository = new SettingsRepository(
            syncItemsPath,
            projectsPath,
            legacyPath);

        var loaded = await repository.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal("C:/Legacy", loaded!.Source!.RepositoryRoot);
        Assert.Equal("legacy-tools", loaded.SyncItems.Single().Id);
        Assert.True(File.Exists(syncItemsPath));
        Assert.True(File.Exists(projectsPath));
        Assert.False(File.Exists(legacyPath));
    }

    private static Task AllowsEditingEveryTransferDirectory()
    {
        var viewModel = new MainViewModel();
        var builtIn = new SyncItemViewModel(new SyncItem
        {
            DisplayName = "Built-in",
            IsBuiltIn = true
        });
        var custom = new SyncItemViewModel(new SyncItem
        {
            DisplayName = "Custom"
        });
        viewModel.SyncItems.Add(builtIn);
        viewModel.SyncItems.Add(custom);

        viewModel.SelectedSyncItem = builtIn;
        Assert.True(viewModel.EditSyncItemCommand.CanExecute(null));
        Assert.True(viewModel.RemoveSyncItemCommand.CanExecute(null));
        viewModel.RemoveSyncItemCommand.Execute(null);
        Assert.False(viewModel.SyncItems.Contains(builtIn));

        viewModel.SelectedSyncItem = custom;
        Assert.True(viewModel.EditSyncItemCommand.CanExecute(null));
        Assert.True(viewModel.RemoveSyncItemCommand.CanExecute(null));
        return Task.CompletedTask;
    }

    private static async Task BlocksMissingDependencies()
    {
        using var workspace = new TestWorkspace();
        var source = workspace.CreateRepository("Source", "SourceGame");
        var target = workspace.CreateRepository("Target", "TargetGame");
        var common = Path.Combine(source.UnityProjectRoot, "Assets", "Scripts", "BbxCommon");
        Directory.CreateDirectory(common);
        await File.WriteAllTextAsync(Path.Combine(common, "Code.cs"), "// code");
        await File.WriteAllTextAsync(common + ".meta", "guid: test");

        var item = new SyncItem
        {
            Id = "bbxcommon-source",
            DisplayName = "BbxCommon Source",
            SourceBase = PathBaseKind.UnityProjectRoot,
            SourceRelativePath = "Assets/Scripts/BbxCommon",
            TargetBase = PathBaseKind.UnityProjectRoot,
            TargetRelativePath = "Assets/Scripts/BbxCommon",
            IncludeCompanionMeta = true
        };

        var preview = await CreatePlanner().CreatePreviewAsync(
            source,
            [target],
            [item],
            null,
            CancellationToken.None);

        Assert.True(preview.HasBlockingErrors);
        Assert.True(preview.Targets.Single().Errors.Any(
            error => error.Contains("com.unity.entities", StringComparison.Ordinal)));
    }

    private static Task RejectsPathTraversal()
    {
        using var workspace = new TestWorkspace();
        var basePath = Path.Combine(workspace.Root, "base");
        Directory.CreateDirectory(basePath);
        Assert.Throws<InvalidOperationException>(
            () => PathService.ResolveInside(basePath, "../outside"));
        return Task.CompletedTask;
    }

    private static SyncItem CreateToolsItem()
    {
        return new SyncItem
        {
            Id = "shared-tools",
            DisplayName = "Shared Tools",
            SourceBase = PathBaseKind.RepositoryRoot,
            SourceRelativePath = "Tools",
            TargetBase = PathBaseKind.RepositoryRoot,
            TargetRelativePath = "Tools",
            UseGitIgnoreFiles = true
        };
    }

    private static SyncPlanner CreatePlanner(
        IgnoreRuleLoader? ignoreRuleLoader = null,
        IUnityEditorLocator? unityEditorLocator = null)
    {
        var locator = new ProjectLocator();
        return new SyncPlanner(
            ignoreRuleLoader ?? new IgnoreRuleLoader(),
            new PathInclusionEvaluator(),
            locator,
            new ProjectValidator(locator),
            unityEditorLocator ?? new FakeUnityEditorLocator([]));
    }

    private static async Task<TargetPreview> PreviewTarget(
        PreviewStatusFixture fixture,
        ProjectContext target)
    {
        var preview = await CreatePlanner().CreatePreviewAsync(
            fixture.Source,
            [target],
            fixture.Items,
            null,
            CancellationToken.None);
        Assert.False(preview.HasBlockingErrors, JoinErrors(preview));
        return preview.Targets.Single();
    }

    private static string JoinErrors(SyncPreview preview)
    {
        return string.Join(
            Environment.NewLine,
            preview.Errors.Concat(preview.Targets.SelectMany(target => target.Errors)));
    }
}

internal sealed class TestWorkspace : IDisposable
{
    public TestWorkspace()
    {
        Root = Path.Combine(
            Path.GetTempPath(),
            "BbxDeployerTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public ProjectContext CreateRepository(string repositoryName, string gameName)
    {
        var repositoryRoot = Path.Combine(Root, repositoryName);
        Directory.CreateDirectory(Path.Combine(repositoryRoot, "Tools"));
        var unityRoot = CreateUnityProject(Path.Combine(repositoryName, gameName));
        return new ProjectContext
        {
            DisplayName = gameName,
            RepositoryRoot = repositoryRoot,
            UnityProjectRoot = unityRoot
        };
    }

    public string CreateUnityProject(string relativePath)
    {
        var root = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.Combine(root, "Assets"));
        Directory.CreateDirectory(Path.Combine(root, "Packages"));
        Directory.CreateDirectory(Path.Combine(root, "ProjectSettings"));
        File.WriteAllText(
            Path.Combine(root, "Packages", "manifest.json"),
            """{"dependencies":{}}""");
        File.WriteAllText(
            Path.Combine(root, "ProjectSettings", "ProjectVersion.txt"),
            "m_EditorVersion: 2022.2.7f1c1\n");
        return root;
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, true);
        }
    }
}

internal static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "Expected true.");
        }
    }

    public static void False(bool condition, string? message = null)
    {
        True(!condition, message ?? "Expected false.");
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }
    }

    public static void NotNull(object? value)
    {
        True(value is not null, "Expected a non-null value.");
    }

    public static void Contains<T>(T expected, IEnumerable<T> values)
    {
        True(values.Contains(expected), $"Expected collection to contain '{expected}'.");
    }

    public static void DoesNotContain<T>(T expected, IEnumerable<T> values)
    {
        False(values.Contains(expected), $"Expected collection not to contain '{expected}'.");
    }

    public static void Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    public static async Task ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}

internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
