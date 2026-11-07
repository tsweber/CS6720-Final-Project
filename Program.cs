using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Each example has a buggy condition and a few expected answers.
// All conditions use two integer inputs, x and y, and return a Boolean.
Example[] examples =
[
    new("Is x less than y?", "x > y",
        [new(2, 5, true), new(5, 2, false), new(3, 3, false)]),
    new("Is either number positive?", "x > 0 && y > 0",
        [new(1, 1, true), new(1, -1, true), new(-1, 1, true), new(-1, -1, false)]),
    new("Are the numbers equal?", "x != y",
        [new(3, 3, true), new(2, 5, false), new(5, 2, false)]),
    new("Is x less than or equal to y?", "x >= y",
        [new(2, 5, true), new(5, 2, false), new(3, 3, true)])
];

// Roslyn needs references to the .NET libraries when compiling a candidate.
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path)).ToArray();

Console.WriteLine("Conditional Logic Repair");
var repaired = 0;
foreach (var example in examples)
{
    Console.WriteLine($"\n{example.Name}");
    var originalPassed = CountPassed(example.Condition, example.Tests);
    Console.WriteLine($"  Before: {example.Condition}  ({originalPassed}/{example.Tests.Length} tests pass)");
    if (originalPassed == example.Tests.Length)
    {
        Console.WriteLine("  Already passes these tests; no change needed.");
        continue;
    }

    var found = false;
    foreach (var candidate in MakeCandidates(example.Condition))
    {
        if (CountPassed(candidate, example.Tests) != example.Tests.Length) continue;
        Console.WriteLine($"  After:  {candidate}  ({example.Tests.Length}/{example.Tests.Length} tests pass)");
        repaired++;
        found = true;
        break;
    }
    if (!found) Console.WriteLine("  No single operator swap passed all tests.");
}
Console.WriteLine($"\nFound a test-passing replacement for {repaired}/{examples.Length} examples.");

// Change one operator at a time, always starting from the original condition.
static IEnumerable<string> MakeCandidates(string condition)
{
    var root = SyntaxFactory.ParseExpression(condition);
    foreach (var expression in root.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>())
    {
        var replacementKind = expression.Kind() switch
        {
            SyntaxKind.GreaterThanExpression => SyntaxKind.LessThanExpression,
            SyntaxKind.LessThanExpression => SyntaxKind.GreaterThanExpression,
            SyntaxKind.GreaterThanOrEqualExpression => SyntaxKind.LessThanOrEqualExpression,
            SyntaxKind.LessThanOrEqualExpression => SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.LogicalAndExpression => SyntaxKind.LogicalOrExpression,
            SyntaxKind.LogicalOrExpression => SyntaxKind.LogicalAndExpression,
            SyntaxKind.EqualsExpression => SyntaxKind.NotEqualsExpression,
            SyntaxKind.NotEqualsExpression => SyntaxKind.EqualsExpression,
            _ => SyntaxKind.None
        };
        if (replacementKind == SyntaxKind.None) continue;

        // Parentheses keep the original grouping when && and || change precedence.
        var replacement = SyntaxFactory.BinaryExpression(replacementKind,
            SyntaxFactory.ParenthesizedExpression(expression.Left.WithoutTrivia()),
            SyntaxFactory.ParenthesizedExpression(expression.Right.WithoutTrivia()));
        var candidate = root.ReplaceNode(expression, SyntaxFactory.ParenthesizedExpression(replacement));
        yield return candidate.NormalizeWhitespace().ToFullString();
    }
}

// Compile a tiny method, call it with each test's inputs, and count correct answers.
int CountPassed(string condition, TestCase[] tests)
{
    var source = "public static class Candidate { public static bool Check(int x, int y) => "
        + condition + "; }";
    var compilation = CSharpCompilation.Create("Candidate_" + Guid.NewGuid().ToString("N"),
        [CSharpSyntaxTree.ParseText(source)], references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    using var stream = new MemoryStream();
    if (!compilation.Emit(stream).Success) return -1;

    var assembly = Assembly.Load(stream.ToArray());
    var method = assembly.GetType("Candidate")!.GetMethod("Check")!;
    var passed = 0;
    foreach (var test in tests)
    {
        try
        {
            var actual = (bool)method.Invoke(null, [test.X, test.Y])!;
            if (actual == test.Expected) passed++;
        }
        catch (TargetInvocationException) { /* A thrown exception fails this test. */ }
    }
    return passed;
}

record TestCase(int X, int Y, bool Expected);
record Example(string Name, string Condition, TestCase[] Tests);
