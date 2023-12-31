using MyCv.Export.Pdf;
using MyCv.Model;
using YamlDotNet.Serialization;

var deserializer = new DeserializerBuilder().Build();
var data = deserializer.Deserialize<SideStructure>(File.ReadAllText("test.yaml"));

var memoryStream = new MemoryStream();
new PdfExporter(new CurrentDirectoryProvider()).Create(data, memoryStream);
File.WriteAllBytes("test.pdf", memoryStream.ToArray());