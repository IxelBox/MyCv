namespace MyCv.Model.Providers;

public interface IFileExporter
{
    Task Create(SideStructure data, Stream stream);
}