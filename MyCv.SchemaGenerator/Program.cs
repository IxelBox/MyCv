
using MyCv.Model;
using NJsonSchema;

Console.WriteLine("The Schema from MyCv:");

var schema = JsonSchema.FromType<SideStructure>();
var schemaJson = schema.ToJson();
Console.WriteLine(schemaJson);
var path = Path.GetFullPath("./mycv.json");
await File.WriteAllTextAsync(path, schemaJson);
Console.WriteLine($"Write Schema into: '{path}'.");
