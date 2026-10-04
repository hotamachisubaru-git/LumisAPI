namespace Lumis;

internal sealed class FileIntegrityFailure
{
    internal FileIntegrityFailure(string filePath, string message)
    {
        FilePath = filePath;
        Message = message;
    }

    internal string FilePath { get; }

    internal string Message { get; }
}
