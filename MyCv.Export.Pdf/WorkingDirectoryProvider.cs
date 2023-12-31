namespace MyCv.Export.Pdf;

public interface IWorkingDirectoryProvider
{
    string WorkingDirectory { get; }
}

public class CurrentDirectoryProvider : IWorkingDirectoryProvider
{
    public string WorkingDirectory => ".";
}