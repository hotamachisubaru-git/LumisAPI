namespace Lumis;

internal sealed class AssemblyIntegrityFailure
{
    internal AssemblyIntegrityFailure(string? filePath, string message)
    {
        FilePath = filePath;
        Message = message;
    }

    internal string? FilePath { get; }

    internal string Message { get; }
}
