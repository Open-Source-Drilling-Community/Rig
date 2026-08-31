namespace ServiceTest;

[TestFixture]
public sealed class GeneratedClientContractTests
{
    [Test]
    public void Concurrency_tokens_use_round_trip_timestamp_format()
    {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !directory.GetFiles("Rig.sln").Any())
            directory = directory.Parent;
        Assert.That(directory, Is.Not.Null, "Unable to locate the Rig solution root.");

        string generatedClient = File.ReadAllText(Path.Combine(directory!.FullName, "ModelSharedOut", "RigMergedModel.cs"));
        string generator = File.ReadAllText(Path.Combine(directory.FullName, "ModelSharedOut", "Program.cs"));
        string[] tokenSerializations = generatedClient.Split('\n')
            .Where(line => line.Contains("expectedModifiedUtc", StringComparison.Ordinal) && line.Contains("ToString(", StringComparison.Ordinal))
            .ToArray();

        Assert.That(generator, Does.Contain("ParameterDateTimeFormat = \"O\""));
        Assert.That(tokenSerializations, Has.Length.EqualTo(3));
        Assert.That(tokenSerializations.All(line => line.Contains("ToString(\"O\"", StringComparison.Ordinal)), Is.True);
        Assert.That(tokenSerializations.Any(line => line.Contains("ToString(\"s\"", StringComparison.Ordinal)), Is.False);
    }
}
