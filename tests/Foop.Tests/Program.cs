using Foop.Models;
using Foop.Services;

var tests = new (string Name, Action Run)[]
{
    ("Centers a regular window", () => AssertRect(
        new ScreenRect(910, 490, 100, 100),
        WindowPlacementService.CenterWithin(
            new ScreenRect(20, 40, 100, 100),
            new ScreenRect(0, 0, 1920, 1080),
            constrainToWorkArea: true))),
    ("Centers on a monitor with negative coordinates", () => AssertRect(
        new ScreenRect(-1410, 190, 900, 700),
        WindowPlacementService.CenterWithin(
            new ScreenRect(0, 0, 900, 700),
            new ScreenRect(-1920, 0, 1920, 1080),
            constrainToWorkArea: true))),
    ("Constrains oversized windows", () => AssertRect(
        new ScreenRect(0, 40, 1920, 1040),
        WindowPlacementService.CenterWithin(
            new ScreenRect(0, 0, 2600, 1600),
            new ScreenRect(0, 40, 1920, 1040),
            constrainToWorkArea: true))),
    ("Respects an offset work area", () => AssertRect(
        new ScreenRect(4840, 440, 800, 600),
        WindowPlacementService.CenterWithin(
            new ScreenRect(0, 0, 800, 600),
            new ScreenRect(3840, 40, 2800, 1400),
            constrainToWorkArea: true))),
    ("Clamps a restored position into the work area", () => AssertRect(
        new ScreenRect(1120, 680, 800, 400),
        WindowPlacementService.PlaceWithin(
            new ScreenRect(2000, 900, 800, 400),
            new ScreenRect(0, 0, 1920, 1080)))),
    ("Matches auto-move rules by executable path", () => AssertTrue(
        DesktopWindowIdentity.Matches(
            new AutoMoveRule(@"C:\Apps\Editor.exe", "Editor", @"\\.\DISPLAY2"),
            CreateDesktopWindow(@"c:\apps\EDITOR.exe", "DifferentProcess")))),
    ("Falls back to process name when the executable path is unavailable", () => AssertTrue(
        DesktopWindowIdentity.Matches(
            new AutoMoveRule(@"C:\Apps\Editor.exe", "Editor", @"\\.\DISPLAY2"),
            CreateDesktopWindow(string.Empty, "editor")))),
    ("Chooses only one window when a first app instance exposes multiple windows", () =>
    {
        var windows = new[]
        {
            CreateDesktopWindow(@"C:\Apps\Editor.exe", "Editor", processId: 20, handle: 2),
            CreateDesktopWindow(@"C:\Apps\Editor.exe", "Editor", processId: 20, handle: 3)
        };
        var groups = windows.GroupBy(
            DesktopWindowIdentity.GetKey,
            StringComparer.OrdinalIgnoreCase);
        var candidates = AutoMoveService.FindNewFirstInstances(
            groups,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        AssertEqual(1, candidates.Count);
    }),
    ("Skips another instance while the application is already running", () =>
    {
        var window = CreateDesktopWindow(
            @"C:\Apps\Editor.exe",
            "Editor",
            processId: 21,
            handle: 4);
        var groups = new[] { window }.GroupBy(
            DesktopWindowIdentity.GetKey,
            StringComparer.OrdinalIgnoreCase);
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DesktopWindowIdentity.GetKey(window)
        };
        AssertEqual(0, AutoMoveService.FindNewFirstInstances(groups, active).Count);
    }),
    ("Extracts the monitor number for preference badges", () =>
        AssertStringEqual("12", MonitorDescriptor.GetDisplayNumber(@"\\.\DISPLAY12")))
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS  {test.Name}");
}

Console.WriteLine($"{tests.Length} tests passed.");

static void AssertRect(ScreenRect expected, ScreenRect actual)
{
    if (expected != actual)
    {
        throw new InvalidOperationException($"Expected {expected}, actual {actual}.");
    }
}

static void AssertTrue(bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException("Expected condition to be true.");
    }
}

static void AssertEqual(int expected, int actual)
{
    if (expected != actual)
    {
        throw new InvalidOperationException($"Expected {expected}, actual {actual}.");
    }
}

static void AssertStringEqual(string expected, string actual)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected {expected}, actual {actual}.");
    }
}

static DesktopWindow CreateDesktopWindow(
    string executablePath,
    string processName,
    uint processId = 1,
    int handle = 1) =>
    new(
        (nint)handle,
        "Window",
        processName,
        processName,
        executablePath,
        processId,
        IsMinimized: false,
        ListByApplicationName: true);
