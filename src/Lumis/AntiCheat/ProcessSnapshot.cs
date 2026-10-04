namespace Lumis;

internal sealed class ProcessSnapshot
{
    internal ProcessSnapshot(
        int id,
        string name,
        string? executablePath = null,
        string? fileDescription = null,
        string? productName = null,
        string? originalFilename = null)
    {
        Id = id;
        Name = name;
        ExecutablePath = executablePath;
        FileDescription = fileDescription;
        ProductName = productName;
        OriginalFilename = originalFilename;
    }

    internal int Id { get; }

    internal string Name { get; }

    internal string? ExecutablePath { get; }

    internal string? FileDescription { get; }

    internal string? ProductName { get; }

    internal string? OriginalFilename { get; }
}
