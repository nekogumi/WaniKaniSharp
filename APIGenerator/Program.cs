using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using HtmlAgilityPack;


const bool download = false;

#region Load HTML file

var sourcePath = SiblingPath("api_source.html");

string source;
if (download || !File.Exists(sourcePath))
{
    HttpClient client = new();
    var task = client.GetStringAsync("https://docs.api.wanikani.com/20170710/");
    task.Wait();
    source = task.Result;
    File.WriteAllText(sourcePath, source);
}
else
    source = File.ReadAllText(sourcePath);

var htmlDoc = new HtmlDocument();
htmlDoc.LoadHtml(source);

#endregion

#region Parse HTML File

var resources = new List<ResourceDefinition>();
var resourcesIds = (from node in htmlDoc.DocumentNode.SelectNodes(
    "//*[@id='resources']/following-sibling::h1")
                    select new
                    {
                        DataStructureNode = node.SelectSingleNode("./following-sibling::h2[1]"),
                        QueryAllNode = node.SelectSingleNode("./following-sibling::h2[2]"),
                        Identification = new ResourceIdentification(node.Id, node.InnerText),
                    }).ToList();

foreach (var resource in resourcesIds)
{
    var title = resource.QueryAllNode.InnerText;
    var command = resource.QueryAllNode.SelectSingleNode(
        "./following-sibling::*[@id='http-request']/following-sibling::*[1]"
        ).InnerText.Split('/')[^1];

    ParameterDefinition[] parameters;

    if (resource.QueryAllNode.SelectSingleNode(
        "./following-sibling::*[@id='http-request']/following-sibling::*[2]"
        ).Id == "query-parameters")
    {
        var parametersTable = resource.QueryAllNode.SelectSingleNode(
            "./following-sibling::*[@id='http-request']/following-sibling::table[1]");
        parameters = (from row in parametersTable.SelectNodes(".//tr[position()>1]")
                      let cells = (from cell in row.SelectNodes("./td")
                                   select cell.InnerText).ToArray()
                      select new ParameterDefinition(
                          Name: cells[0],
                          Type: EndpointParameterTypeConverter(cells[1], resource.Identification, cells[0]),
                          Description: cells[2]
                      )).ToArray();
    }
    else
        parameters = Array.Empty<ParameterDefinition>();

    var endpoint = new EndpointDefinition(command, title, parameters);

    Console.WriteLine($"{resource.Identification.Title} ({resource.Identification.ClassName})");
    //endpoint.Print();

    var className = resource.DataStructureNode.Id[..^"-data-structure".Length].SnakeToUpperCamelCase();
    var dataStructure = new ResourceDataStructureDefinition(className, resource.DataStructureNode.InnerText);

    Console.WriteLine($"    {className} {resource.DataStructureNode.InnerText[..^" Data Structure".Length]}");


    resources.Add(new ResourceDefinition(resource.Identification, endpoint, dataStructure));
}

#endregion 

#region Code Generation

using var stream = new StreamWriter(SiblingPath("..\\WaniKaniSharp\\API.cs"));
using var writer = new IndentedTextWriter(stream, "    ");

writer.WriteLine("// AUTO GENERATED FILE -- DO NOT EDIT");
writer.WriteLine("using System;");
writer.WriteLine("using System.Collections.Generic;");
writer.WriteLine("using System.Threading;");
writer.WriteLine("using System.Threading.Tasks;");
writer.WriteLine("");
writer.WriteLine("namespace Nekogumi.WaniKani.API");
writer.WriteLine("{");
writer.Indent++;

writer.WriteLine("partial class WaniKaniConnection");
writer.WriteLine("{");
writer.Indent++;

var isCollections = new HashSet<string>() { "User", "Summary" };

foreach (var resource in resources)
{
    var responseType = (isCollections.Contains(resource.Identification.ClassName) ? "Response" : "ResponseCollection")
        + "<" + resource.DataStructure.ClassName + "Data>";
    writer.WriteLine($"public Task<{responseType}> Query{resource.Identification.ClassName}Async(");
    writer.Indent++;
    writer.Indent++;
    foreach (var parameter in resource.QueryAll.Parameters)
        writer.WriteLine($"{parameter.Type} {parameter.Name} = null,");

    writer.WriteLine("CacheStrategy cacheStrategy = CacheStrategy.Cache,");
    writer.WriteLine("CancellationToken cancellationToken = default)");
    writer.Indent--;
    if (!isCollections.Contains(resource.Identification.ClassName))
    {
        writer.WriteLine($"=> QueryCollectionAsync<{resource.DataStructure.ClassName}Data>(\"{resource.QueryAll.Command}\", cacheStrategy, cancellationToken");
        writer.Indent++;
        foreach (var parameter in resource.QueryAll.Parameters)
            writer.WriteLine($", (\"{parameter.Name}\", {parameter.Name})");
        writer.WriteLine(");");
        writer.Indent--;
    }
    else
    {
        writer.WriteLine($"=> GetJsonAsync<{responseType}>(\"{resource.QueryAll.Command}\", cacheStrategy, cancellationToken);");
    }
    writer.Indent--;
    writer.WriteLine();


    if (!isCollections.Contains(resource.Identification.ClassName))
    {
        responseType = "Response<" + resource.DataStructure.ClassName + "Data>";
        writer.WriteLine($"public Task<{responseType}> Query{resource.DataStructure.ClassName}Async(");
        writer.Indent++;
        writer.Indent++;
        writer.WriteLine("long id,");
        writer.WriteLine("CacheStrategy cacheStrategy = CacheStrategy.Cache,");
        writer.WriteLine("CancellationToken cancellationToken = default)");
        writer.Indent--;
        writer.WriteLine($"=> GetJsonAsync<{responseType}>($\"{resource.QueryAll.Command}/{{id}}\", cacheStrategy, cancellationToken);");
        writer.Indent--;
        writer.WriteLine();
    }

}

writer.Indent--;
writer.WriteLine("}");

writer.Indent--;
writer.WriteLine("}");

#endregion

#region Type conversion

string EndpointParameterTypeConverter(string Type, ResourceIdentification ressource, string parameter)
{

    Dictionary<string, string> exceptions = new()
    {
        ["Subjects.levels"] = "IEnumerable<int>?",
    };

    if (exceptions.TryGetValue($"{ressource.ClassName}.{parameter}", out var newType))
        return newType;

    Dictionary<string, string> EndpointParameterTypeConverterDict = new()
    {
        ["Array of integers"] = "IEnumerable<long>?",
        ["Array of strings"] = "IEnumerable<string>?",
        ["Date"] = "DateTime?",
        ["Boolean"] = "bool?",
        ["(not required)"] = "bool?",
        ["Integer"] = "long?",
    };

    if (EndpointParameterTypeConverterDict.TryGetValue(Type, out newType))
        return newType;
    return Type;
}

#endregion

#region Utils

string SiblingPath(string filename, [CallerFilePath] string? currentFilepath = null)
    => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(currentFilepath ?? "")) ?? "", filename);

static class TextUtils
{
    //public static readonly Regex SnakeCaseConverter = new(@"(_[a-z])", RegexOptions.Compiled);

    public static string SnakeToUpperCamelCase(this string name)
    {
        name = string.Join("", from part in name.Split("_-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                               select char.ToUpper(part[0]) + part[1..]);
        return name;
    }

}

#endregion

#region Data Model

record ResourceIdentification(string Id, string Title)
{
    public string ClassName => Id.SnakeToUpperCamelCase();
}

record ParameterDefinition(string Name, string Type, string Description);

record EndpointDefinition(string Command, string Title, ParameterDefinition[] Parameters)
{
    public void Print()
    {
        Console.WriteLine($"  ENDPOINT: [{Command}] {Title}");
        foreach (var parameter in Parameters)
            Console.WriteLine($"    {parameter.Name} ({parameter.Type}): {parameter.Description[..32]}...");
    }
}

record ResourceDataStructureDefinition(string ClassName, string Description);

record ResourceDefinition(
    ResourceIdentification Identification,
    EndpointDefinition QueryAll,
    ResourceDataStructureDefinition DataStructure);

#endregion
