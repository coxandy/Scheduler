using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.DefinitionBlockTests;

public class FileDefinitionTests
{
    private static string NoEmailExitDefinition => """
        "ExitDefinition": {
            "IsActive": true,
            "Success": { "SendEmail": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] },
            "Failure": { "SendEmail": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] }
        }
        """;

    /// <summary>
    /// Builds a single-file task JSON. transformsJson is a comma-separated list of
    /// transform objects to embed inside the Transforms array (empty string = no transforms).
    /// </summary>
    private static string BuildTaskJson(
        string jsonSafeSourcePath, string sourceFileName,
        string jsonSafeTargetPath, string targetFileName,
        string action,
        string transformsJson = "") => $$"""
        {
            "FileDefinition": {
                "Files": [
                    {
                        "SourceFilePath": "{{jsonSafeSourcePath}}",
                        "SourceFileName": "{{sourceFileName}}",
                        "TargetFilePath": "{{jsonSafeTargetPath}}",
                        "TargetFileName": "{{targetFileName}}",
                        "Action": "{{action}}",
                        "Transforms": [{{transformsJson}}]
                    }
                ]
            },
            {{NoEmailExitDefinition}}
        }
        """;

    // -------------------------------------------------------------------------
    // Copy
    // -------------------------------------------------------------------------

    /// <summary>
    /// Copy creates the file at the target path with identical content.
    /// The source file remains in place after the operation.
    /// </summary>
    [Fact]
    public async Task FileDefinition_Copy_CreatesFileAtTarget()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "source.txt"), "Hello World");
            var targetDir = Path.Combine(tempDir, "target");
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");
            var jsonSafeTgt = targetDir.Replace("\\", "\\\\");

            await CreateTask(BuildTaskJson(jsonSafeSrc, "source.txt", jsonSafeTgt, "output.txt", "Copy")).Run();

            Assert.True(File.Exists(Path.Combine(targetDir, "output.txt")));
            Assert.Equal("Hello World", File.ReadAllText(Path.Combine(targetDir, "output.txt")));
            Assert.True(File.Exists(Path.Combine(tempDir, "source.txt")), "Source file should still exist after Copy");
        }
        finally { Directory.Delete(tempDir, true); }
    }

    /// <summary>
    /// Copy auto-creates the target directory when it does not exist.
    /// </summary>
    [Fact]
    public async Task FileDefinition_Copy_CreatesTargetDirectoryWhenMissing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "source.txt"), "data");
            var targetDir = Path.Combine(tempDir, "new", "nested", "dir");
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");
            var jsonSafeTgt = targetDir.Replace("\\", "\\\\");

            await CreateTask(BuildTaskJson(jsonSafeSrc, "source.txt", jsonSafeTgt, "output.txt", "Copy")).Run();

            Assert.True(Directory.Exists(targetDir));
            Assert.True(File.Exists(Path.Combine(targetDir, "output.txt")));
        }
        finally { Directory.Delete(tempDir, true); }
    }

    // -------------------------------------------------------------------------
    // Move
    // -------------------------------------------------------------------------

    /// <summary>
    /// Move places the file at the target path and removes it from the source path.
    /// </summary>
    [Fact]
    public async Task FileDefinition_Move_MovesFileToTargetAndRemovesSource()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "source.txt"), "Move me");
            var targetDir = Path.Combine(tempDir, "target");
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");
            var jsonSafeTgt = targetDir.Replace("\\", "\\\\");

            await CreateTask(BuildTaskJson(jsonSafeSrc, "source.txt", jsonSafeTgt, "moved.txt", "Move")).Run();

            Assert.True(File.Exists(Path.Combine(targetDir, "moved.txt")));
            Assert.Equal("Move me", File.ReadAllText(Path.Combine(targetDir, "moved.txt")));
            Assert.False(File.Exists(Path.Combine(tempDir, "source.txt")), "Source file should be gone after Move");
        }
        finally { Directory.Delete(tempDir, true); }
    }

    // -------------------------------------------------------------------------
    // Delete
    // -------------------------------------------------------------------------

    /// <summary>
    /// Delete removes the source file from disk.
    /// </summary>
    [Fact]
    public async Task FileDefinition_Delete_RemovesSourceFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var srcFile = Path.Combine(tempDir, "todelete.txt");
            File.WriteAllText(srcFile, "Delete me");
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");

            await CreateTask(BuildTaskJson(jsonSafeSrc, "todelete.txt", jsonSafeSrc, "todelete.txt", "Delete")).Run();

            Assert.False(File.Exists(srcFile));
        }
        finally { Directory.Delete(tempDir, true); }
    }

    // -------------------------------------------------------------------------
    // Error handling
    // -------------------------------------------------------------------------

    /// <summary>
    /// Any action on a missing source file throws FileNotFoundException.
    /// </summary>
    [Fact]
    public async Task FileDefinition_MissingSourceFile_ThrowsFileNotFoundException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");

            // Source file is never created
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => CreateTask(BuildTaskJson(jsonSafeSrc, "missing.txt", jsonSafeSrc, "output.txt", "Copy")).Run());
        }
        finally { Directory.Delete(tempDir, true); }
    }

    // -------------------------------------------------------------------------
    // Transform: ToLower
    // -------------------------------------------------------------------------

    /// <summary>
    /// ToLower converts every line to lowercase and writes the result to the target.
    /// ToUpper is omitted as it exercises the identical dispatch path (lines.Select);
    /// the Chained test also covers ToLower providing a second pass.
    /// </summary>
    [Fact]
    public async Task FileDefinition_Transform_ToLower_ConvertsAllLinesToLowercase()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "input.txt"), "HELLO WORLD\nFOO BAR");
            var targetDir = Path.Combine(tempDir, "out");
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");
            var jsonSafeTgt = targetDir.Replace("\\", "\\\\");

            var transforms = """{ "TransformType": "ToLower", "TransformParams": [] }""";
            await CreateTask(BuildTaskJson(jsonSafeSrc, "input.txt", jsonSafeTgt, "output.txt", "Transform", transforms)).Run();

            var result = File.ReadAllLines(Path.Combine(targetDir, "output.txt"));
            Assert.Equal("hello world", result[0]);
            Assert.Equal("foo bar", result[1]);
        }
        finally { Directory.Delete(tempDir, true); }
    }

    // -------------------------------------------------------------------------
    // Transform: Replace
    // -------------------------------------------------------------------------

    /// <summary>
    /// Replace substitutes all literal occurrences of TransformParams[0] with TransformParams[1]
    /// in every line.
    /// </summary>
    [Fact]
    public async Task FileDefinition_Transform_Replace_SubstitutesText()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "input.txt"), "foo,bar\nfoo,baz");
            var targetDir = Path.Combine(tempDir, "out");
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");
            var jsonSafeTgt = targetDir.Replace("\\", "\\\\");

            var transforms = """{ "TransformType": "Replace", "TransformParams": ["foo", "qux"] }""";
            await CreateTask(BuildTaskJson(jsonSafeSrc, "input.txt", jsonSafeTgt, "output.txt", "Transform", transforms)).Run();

            var result = File.ReadAllLines(Path.Combine(targetDir, "output.txt"));
            Assert.Equal("qux,bar", result[0]);
            Assert.Equal("qux,baz", result[1]);
        }
        finally { Directory.Delete(tempDir, true); }
    }

    // -------------------------------------------------------------------------
    // Transform: RegexMatch
    // -------------------------------------------------------------------------

    /// <summary>
    /// RegexMatch keeps only the lines that match the given pattern,
    /// discarding non-matching lines.
    /// </summary>
    [Fact]
    public async Task FileDefinition_Transform_RegexMatch_FiltersOnlyMatchingLines()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "input.txt"),
                "ERROR: disk full\nINFO: all good\nERROR: timeout\nDEBUG: skip me");
            var targetDir = Path.Combine(tempDir, "out");
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");
            var jsonSafeTgt = targetDir.Replace("\\", "\\\\");

            var transforms = """{ "TransformType": "RegexMatch", "TransformParams": ["^ERROR:"] }""";
            await CreateTask(BuildTaskJson(jsonSafeSrc, "input.txt", jsonSafeTgt, "output.txt", "Transform", transforms)).Run();

            var result = File.ReadAllLines(Path.Combine(targetDir, "output.txt"));
            Assert.Equal(2, result.Length);
            Assert.Equal("ERROR: disk full", result[0]);
            Assert.Equal("ERROR: timeout", result[1]);
        }
        finally { Directory.Delete(tempDir, true); }
    }

    // -------------------------------------------------------------------------
    // Transform: RegexReplace
    // -------------------------------------------------------------------------

    /// <summary>
    /// RegexReplace rewrites each line by replacing regex pattern matches.
    /// Demonstrates reformatting a date column from YYYY-MM-DD to DD/MM/YYYY.
    /// </summary>
    [Fact]
    public async Task FileDefinition_Transform_RegexReplace_ReplacesMatchedPatterns()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "input.txt"), "2026-01-15,100\n2026-02-20,200");
            var targetDir = Path.Combine(tempDir, "out");
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");
            var jsonSafeTgt = targetDir.Replace("\\", "\\\\");

            // Pattern: capture YYYY-MM-DD groups, rewrite as DD/MM/YYYY
            // JSON \\d = regex \d (JSON unescapes \\d → \d)
            var transforms = """{ "TransformType": "RegexReplace", "TransformParams": ["(\\d{4})-(\\d{2})-(\\d{2})", "$3/$2/$1"] }""";
            await CreateTask(BuildTaskJson(jsonSafeSrc, "input.txt", jsonSafeTgt, "output.txt", "Transform", transforms)).Run();

            var result = File.ReadAllLines(Path.Combine(targetDir, "output.txt"));
            Assert.Equal("15/01/2026,100", result[0]);
            Assert.Equal("20/02/2026,200", result[1]);
        }
        finally { Directory.Delete(tempDir, true); }
    }

    // -------------------------------------------------------------------------
    // Chained transforms
    // -------------------------------------------------------------------------

    /// <summary>
    /// Transforms are applied in declaration order. Here ToLower runs first,
    /// then RegexMatch filters lines by the lowercased content.
    /// </summary>
    [Fact]
    public async Task FileDefinition_Transform_Chained_AppliedInDeclarationOrder()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "input.txt"), "Hello World\nFoo Bar\nSkip This");
            var targetDir = Path.Combine(tempDir, "out");
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");
            var jsonSafeTgt = targetDir.Replace("\\", "\\\\");

            // Step 1: ToLower → "hello world", "foo bar", "skip this"
            // Step 2: RegexMatch "^(hello|foo)" → retains first two lines only
            var transforms = """
                { "TransformType": "ToLower",    "TransformParams": [] },
                { "TransformType": "RegexMatch", "TransformParams": ["^(hello|foo)"] }
                """;
            await CreateTask(BuildTaskJson(jsonSafeSrc, "input.txt", jsonSafeTgt, "output.txt", "Transform", transforms)).Run();

            var result = File.ReadAllLines(Path.Combine(targetDir, "output.txt"));
            Assert.Equal(2, result.Length);
            Assert.Equal("hello world", result[0]);
            Assert.Equal("foo bar", result[1]);
        }
        finally { Directory.Delete(tempDir, true); }
    }

    // -------------------------------------------------------------------------
    // Multiple files
    // -------------------------------------------------------------------------

    /// <summary>
    /// All entries in the Files array are processed. Each file is copied independently.
    /// </summary>
    [Fact]
    public async Task FileDefinition_MultipleFiles_AllProcessedInOrder()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"filetest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "file1.txt"), "Content One");
            File.WriteAllText(Path.Combine(tempDir, "file2.txt"), "Content Two");
            var targetDir = Path.Combine(tempDir, "target");
            var jsonSafeSrc = tempDir.Replace("\\", "\\\\");
            var jsonSafeTgt = targetDir.Replace("\\", "\\\\");

            var json = $$"""
                {
                    "FileDefinition": {
                        "Files": [
                            {
                                "SourceFilePath": "{{jsonSafeSrc}}",
                                "SourceFileName": "file1.txt",
                                "TargetFilePath": "{{jsonSafeTgt}}",
                                "TargetFileName": "copy1.txt",
                                "Action": "Copy",
                                "Transforms": []
                            },
                            {
                                "SourceFilePath": "{{jsonSafeSrc}}",
                                "SourceFileName": "file2.txt",
                                "TargetFilePath": "{{jsonSafeTgt}}",
                                "TargetFileName": "copy2.txt",
                                "Action": "Copy",
                                "Transforms": []
                            }
                        ]
                    },
                    {{NoEmailExitDefinition}}
                }
                """;

            await CreateTask(json).Run();

            Assert.True(File.Exists(Path.Combine(targetDir, "copy1.txt")));
            Assert.True(File.Exists(Path.Combine(targetDir, "copy2.txt")));
            Assert.Equal("Content One", File.ReadAllText(Path.Combine(targetDir, "copy1.txt")));
            Assert.Equal("Content Two", File.ReadAllText(Path.Combine(targetDir, "copy2.txt")));
        }
        finally { Directory.Delete(tempDir, true); }
    }
}
