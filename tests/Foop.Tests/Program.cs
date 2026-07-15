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
            constrainToWorkArea: true)))
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
