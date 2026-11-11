using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using FbxImporter.ViewModels;
using ReactiveHistory;
using ReactiveUI;
using SoulsAssetPipeline.FLVERImporting;
using SoulsFormats;

namespace FbxImporter.Tests;

[CollectionDefinition("Logger", DisableParallelization = true)]
public class LoggerCollection { }

// Drives MainWindowViewModel as the app does, answering only the file picker. The log and
// RxApp.DefaultExceptionHandler are global, so these tests run one at a time.
[Collection("Logger")]
public class ErrorHandlingTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "errors_" + Guid.NewGuid().ToString("N"));
    private readonly IObserver<Exception> _defaultHandler = RxApp.DefaultExceptionHandler;
    private readonly IScheduler _mainScheduler = RxApp.MainThreadScheduler;
    private readonly List<Exception> _unhandled = new();
    private readonly MainWindowViewModel _vm;
    private string? _nextPath;

    public ErrorHandlingTests()
    {
        Directory.CreateDirectory(_folder);
        Logger.Instance.Lines.Clear();
        // Deliver command errors at once, as the app's UI thread does between steps.
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(e => _unhandled.Add(e));
        _vm = new MainWindowViewModel();
        _vm.GetFilePath.RegisterHandler(ctx => ctx.SetOutput(_nextPath));
    }

    public void Dispose()
    {
        RxApp.DefaultExceptionHandler = _defaultHandler;
        RxApp.MainThreadScheduler = _mainScheduler;
        foreach (string file in Directory.GetFiles(_folder)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_folder, true);
    }

    private static async Task Run(ReactiveCommand<Unit, Unit> command)
    {
        try { await command.Execute(); }
        catch { /* the command's own error path is what the tests look at */ }
    }

    private List<string> Errors => Logger.Instance.Lines.Where(x => x.StartsWith("Error:")).ToList();

    private static void AssertNoTrace() =>
        Assert.DoesNotContain(Logger.Instance.Lines, x => x.Contains("   at "));

    [Fact]
    public async Task OpeningAMissingFlverSaysFileNotFound()
    {
        _nextPath = Path.Combine(_folder, "missing.flver");

        await Run(_vm.OpenFlverCommand);

        string error = Assert.Single(Errors);
        Assert.StartsWith("Error: File not found: ", error);
        Assert.Contains("missing.flver", error);
        Assert.Empty(_unhandled);
    }

    [Fact]
    public async Task OpeningAFileThatIsNotAFlverSaysSo()
    {
        _nextPath = Path.Combine(_folder, "text.flver");
        File.WriteAllText(_nextPath, "not a flver");

        await Run(_vm.OpenFlverCommand);

        Assert.Contains(Errors, x => x.Contains("is not a flver file", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(_unhandled);
    }

    [Fact]
    public async Task ImportingAMissingFbxSaysFileNotFoundInOneLine()
    {
        _nextPath = Path.Combine(_folder, "missing.fbx");

        await Run(_vm.ImportFbxCommand);

        string error = Assert.Single(Errors);
        Assert.StartsWith("Error: File not found: ", error);
        Assert.Contains("missing.fbx", error);
        AssertNoTrace();
        Assert.Empty(_unhandled);
    }

    [Fact]
    public async Task AFailedSaveAsIsLoggedAndSaveKeepsThePreviousPath()
    {
        _vm.Flver = new FlverViewModel(new FLVER2(), new FLVER2MaterialInfoBank(), new StackHistory());
        string good = Path.Combine(_folder, "good.flver");
        string bad = Path.Combine(_folder, "bad.flver");
        File.WriteAllText(bad, "");
        File.SetAttributes(bad, FileAttributes.ReadOnly);

        _nextPath = good;
        await Run(_vm.SaveFlverAsCommand);
        File.Delete(good);

        _nextPath = bad;
        await Run(_vm.SaveFlverAsCommand);

        Assert.Empty(_unhandled);
        string error = Assert.Single(Errors);
        Assert.Contains("bad.flver", error);
        AssertNoTrace();

        await Run(_vm.SaveFlverCommand);

        Assert.True(File.Exists(good));
        Assert.Empty(_unhandled);
    }
}
