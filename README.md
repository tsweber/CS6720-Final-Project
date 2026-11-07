# Conditional Logic Repair

A small C# console app that uses **Roslyn** to try operator changes and find one that passes a few tests. The active project consists of one code file, `Program.cs`, and one project file.

## Run

Install a .NET 8 SDK (8.0.301 or later in the .NET 8 series), open this folder in a terminal, and run:

```powershell
dotnet run
```

The first run restores the Roslyn NuGet package. You can also open `ConditionalRepair.csproj` in Visual Studio.

## What it demonstrates

Four built-in examples contain incorrect conditions:

| Example | Buggy condition | Expected replacement |
| --- | --- | --- |
| Less than | `x > y` | `x < y` |
| Either number positive | `x > 0 && y > 0` | `x > 0 || y > 0` |
| Equality | `x != y` | `x == y` |
| Less than or equal | `x >= y` | `x <= y` |

The program prints how many tests the original condition passes, generates alternatives by changing one operator, and displays the first alternative that passes every test. Extra parentheses in the output preserve expression grouping.

Supported swaps work both ways: `>` / `<`, `>=` / `<=`, `&&` / `||`, and `==` / `!=`.

## How the code works

1. **Examples and tests:** Each example has a condition and test cases in the form `(x, y, expected result)`.
2. **MakeCandidates:** Roslyn finds binary expressions and replaces one operator at a time.
3. **CountPassed:** Roslyn compiles each condition into a small method, runs the test inputs, and counts correct results.

To edit the `examples` array near the top of `Program.cs`. For example, `new(2, 5, true)` means the condition should return `true` when `x` is 2 and `y` is 5.

## References

Roslyn reference: [Microsoft's syntax transformation guide](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/syntax-transformation).
