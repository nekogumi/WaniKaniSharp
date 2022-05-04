using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using HtmlAgilityPack;


const bool download = true;

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
var classes = new List<ClassDefinition>();

var resourcesIds = (from node in htmlDoc.DocumentNode.SelectNodes(
    "//*[@id='resources']/following-sibling::h1")
                    select new
                    {
                        DataStructureNode = node.SelectSingleNode("./following-sibling::h2[1]"),
                        QueryAllNode = node.SelectSingleNode("./following-sibling::h2[2]"),
                        Identification = new ResourceIdentification(node.Id, node.InnerText),
                    }).ToList();

var skippedDataStructureTables = new[]
{
    "incorrect-answers",
};

var skippedChapterIds = new[]
{
    "common-attributes",
};


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
                          Description: cells[2].UnescapeHTML()
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


    var left = $"({resource.DataStructureNode.XPath} | {resource.DataStructureNode.XPath}/following-sibling::*[self::table or self::h3 or self::h4 or self::h5])";
    var right = $"{resource.QueryAllNode.XPath}/preceding-sibling::*[self::table or self::h2 or self::h3 or self::h4 or self::h5]";
    var nodes = htmlDoc.DocumentNode.SelectNodes($"{left}[count(.|{right}) = count({right})]");

    //Console.WriteLine("-------------------------");
    string? id = null;
    string? label = null;
    foreach (var node in nodes)
    {
        if (node.Name.ToLower() == "table")
        {
            if (!skippedDataStructureTables.Contains(id))
            {
                var fields = (from row in node.SelectNodes("./tbody/tr")
                              let cells = (from cell in row.SelectNodes("./td")
                                           select cell.InnerText).ToArray()
                              select new ParameterDefinition(
                                  Name: cells[0].SnakeToUpperCamelCase(),
                                  Type: FieldTypeConverter(cells[1], resource.Identification, cells[0]),
                                  Description: cells[2].UnescapeHTML()
                              )).ToArray();
                var @class = new ClassDefinition(resource.Identification, id ?? string.Empty, label ?? string.Empty, fields);
                classes.Add(@class);
            }
        }
        else if (!skippedChapterIds.Contains(node.Id))
        {
            id = node.Id;
            label = node.InnerText;
            Console.WriteLine($"{node.Id}: {node.InnerText}");
        }
    }

    resources.Add(new ResourceDefinition(resource.Identification, endpoint, dataStructure));
}

#endregion 

#region Code Generation

{
    using var stream = new StreamWriter(SiblingPath("..\\WaniKaniSharp\\Services.cs"));
    using var writer = new IndentedTextWriter(stream, "    ");

    writer.WriteLine("// AUTO GENERATED FILE -- DO NOT EDIT");
    writer.WriteLine("using System;");
    writer.WriteLine("using System.Collections.Generic;");
    writer.WriteLine("using System.Threading;");
    writer.WriteLine("using System.Threading.Tasks;");
    writer.WriteLine("");
    writer.WriteLine("namespace Nekogumi.WaniKani.Services");
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
}

{

    var skipClasses = new[]
    {
        "ReadingObjectAttributes2",
    };


    var attributeTypes = new Dictionary<string, string>()
    {
        ["SubscriptionObjectAttributes.Type"] = "SubscriptionType",
        ["AssignmentData.SubjectType"] = "SubjectType",
    };
    
    var objectTypeClassNames = new Dictionary<string, string>()
    {
        ["MetadataObjectAttributes"] = "PronunciationAudioMetadataObjectAttributes",
    };

    var classesName = new Dictionary<string, string>();
    foreach (var x in classes)
    {
        var className = x.Id.Replace('-', '_').SnakeToUpperCamelCase();
        if (skipClasses.Contains(className)) continue;
        if (className.EndsWith("DataStructure"))
        {
            className = className.Replace("DataStructure", "Data");
        }
        else
        {
            //.Replace("ObjectAttributes", "")
            //.Replace("Attributes", "")

            //className = "Api" + className;
        }
        classesName[x.Id] = className;
    }




    using var stream = new StreamWriter(SiblingPath("..\\WaniKaniSharp\\Classes.cs"));
    using var writer = new IndentedTextWriter(stream, "    ");

    writer.WriteLine("// AUTO GENERATED FILE -- DO NOT EDIT");
    writer.WriteLine("using System;");
    writer.WriteLine("");
    writer.WriteLine("namespace Nekogumi.WaniKani.Services");
    writer.WriteLine("{");
    writer.Indent++;

    foreach (var group in from @class in classes
                          group @class by @class.Resource.Title)
    {
        writer.WriteLine($"#region {group.Key}");
        writer.WriteLine();

        foreach (var @class in group)
            if (classesName.TryGetValue(@class.Id, out var className))
            {
                var fields = new List<(string fieldName, string type, string desc)>();
                foreach (var field in @class.Fields)
                {
                    var fieldName = field.Name;
                    if (!attributeTypes.TryGetValue(className + "." + fieldName, out var type))
                    {
                        type = field.Type;
                        if (type == "Object")
                        {
                            type = fieldName + "ObjectAttributes";
                            if (objectTypeClassNames.ContainsKey(type))
                                type = objectTypeClassNames[type];
                        }
                        else if (type == "Array of objects")
                        {
                            type = fieldName + "ObjectAttributes";
                            if (!classesName.ContainsValue(type))
                                type = (fieldName.EndsWith("s") ? fieldName[0..^1] : fieldName) + "ObjectAttributes";
                            if (objectTypeClassNames.ContainsKey(type))
                                type = objectTypeClassNames[type];
                            type += "[]";
                        }
                    }
                    fields.Add((fieldName, type, field.Description));
                }

                writer.WriteLine($"/// <summary></summary>");
                foreach (var (fieldName, type, desc) in fields)
                    writer.WriteLine($"/// <param name=\"{fieldName}\">{desc}</param>");

                writer.WriteLine($"public record {className}(");
                writer.Indent++;
                for (int i = 0; i < fields.Count; i++)
                    writer.WriteLine($"{fields[i].type} {fields[i].fieldName}{(i == fields.Count - 1 ? ");" : ",")}");
                writer.Indent--;
                writer.WriteLine();
            }

        writer.WriteLine("#endregion");
        writer.WriteLine();
    }

    writer.Indent--;
    writer.WriteLine("}");
}

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

string FieldTypeConverter(string Type, ResourceIdentification ressource, string parameter)
{

    Dictionary<string, string> exceptions = new()
    {

    };

    if (exceptions.TryGetValue($"{ressource.ClassName}.{parameter}", out var newType))
        return newType;

    Dictionary<string, string> EndpointParameterTypeConverterDict = new()
    {
        ["Integer"] = "int",
        ["null or Integer"] = "int?",
        ["null or String"] = "string?",
        ["String or null"] = "string?",
        ["Boolean"] = "bool",
        ["String"] = "string",
        ["Array of strings"] = "string[]",
        ["Array of integers"] = "int[]",
        ["Date"] = "DateTime",
        ["null or Date"] = "DateTime?",
        //["Array of objects"] = "object[]",
        ["Array"] = "object[]",
        //["Object"] = "object",
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

    public static string UnescapeHTML(this string text)
        => text.Replace("&#39;", "'").Replace("’", "'");

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

record ClassDefinition(ResourceIdentification Resource, string Id, string Name, ParameterDefinition[] Fields);

record ResourceDataStructureDefinition(string ClassName, string Description);

record ResourceDefinition(
    ResourceIdentification Identification,
    EndpointDefinition QueryAll,
    ResourceDataStructureDefinition DataStructure);

#endregion
