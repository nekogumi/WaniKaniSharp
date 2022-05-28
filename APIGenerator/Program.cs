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

    resources.Add(new ResourceDefinition(resource.Identification, endpoint, dataStructure));

}


var skippedDataStructureTables = new[]
{
    "incorrect-answers",
};

var skippedChapterIds = new[]
{
    "common-attributes",
    "when-content_type-is-code-image-svg-xml-code",
};

var mergedClasses = new Dictionary<string, string>
{
    ["subject-data-structure"] = "subject-data-structure",
    ["radical-attributes"] = "subject-data-structure",
    ["kanji-attributes"] = "subject-data-structure",
    ["vocabulary-attributes"] = "subject-data-structure",
    ["reading-object-attributes"] = "reading-object-attributes",
    ["reading-object-attributes-2"] = "reading-object-attributes",
    ["character-image-metadata-object-attributes"] = "character-image-metadata-object-attributes",
    ["when-content_type-is-code-image-png-code"] = "character-image-metadata-object-attributes",
};

var classesDeclarations = new List<ClassDeclaration>();
foreach (var resource in resourcesIds)
{
    var left = $"({resource.DataStructureNode.XPath} | {resource.DataStructureNode.XPath}/following-sibling::*[self::table or self::h3 or self::h4 or self::h5])";
    var right = $"{resource.QueryAllNode.XPath}/preceding-sibling::*[self::table or self::h2 or self::h3 or self::h4 or self::h5]";
    var nodes = htmlDoc.DocumentNode.SelectNodes($"{left}[count(.|{right}) = count({right})]");

    string id = string.Empty;
    string label = string.Empty;
    foreach (var node in nodes)
    {
        if (node.Name.ToLower() == "table")
        {
            if (!skippedDataStructureTables.Contains(id))
            {
                if (mergedClasses.TryGetValue(id, out var mergedClassId))
                {
                    var @class = classesDeclarations.FirstOrDefault(c => c.Id == mergedClassId);
                    if (@class is null)
                        classesDeclarations.Add(new ClassDeclaration(resource.Identification, mergedClassId, label, new List<HtmlNode> { node }));
                    else
                        @class.Tables.Add(node);
                }
                else
                    classesDeclarations.Add(new ClassDeclaration(resource.Identification, id, label, new List<HtmlNode> { node }));
            }
        }
        else if (!skippedChapterIds.Contains(node.Id))
        {
            id = node.Id;
            label = node.InnerText;
            //Console.WriteLine($"{node.Id}: {node.InnerText}");
        }
    }
}

var classes = new List<ClassDefinition>();
foreach (var @class in classesDeclarations)
{
    var fieldsQuery = from node in @class.Tables
                      from row in node.SelectNodes("./tbody/tr")
                      let cells = (from cell in row.SelectNodes("./td")
                                   select cell.InnerText).ToArray()
                      let fieldName = cells[0].SnakeToUpperCamelCase()
                      group new ParameterDefinition(
                          Name: fieldName,
                          Type: FieldTypeConverter(cells[1], @class.ClassName, fieldName),
                          Description: cells[2].UnescapeHTML()
                      ) by fieldName;

    var fields = new List<ParameterDefinition>();
    foreach (var fieldGroup in fieldsQuery)
    {
        if (fieldGroup.Count() == 1)
            fields.Add(fieldGroup.First());
        else
        {
            var types = (from field in fieldGroup
                         orderby field.Type
                         select field.Type).Distinct().ToArray();
            if (types.Length == 1)
                fields.Add(fieldGroup.First());
            else if (types.Length == 2 && types[0] + "?" == types[1])
                fields.Add(new ParameterDefinition(fieldGroup.Key, types[1], fieldGroup.First().Description));
            else
                fields.AddRange(fieldGroup);
        }
    }

    classes.Add(new ClassDefinition(@class.Resource, @class.Id, @class.Label, fields.ToArray()));
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
        {
            var fields = new List<(string fieldName, string type, string desc)>();
            foreach (var field in from field in @class.Fields
                                  orderby field.Name
                                  select field)
            {
                var fieldName = field.Name;
                var type = field.Type;
                fields.Add((fieldName, type, field.Description));
            }

            writer.WriteLine($"/// <summary></summary>");
            foreach (var (fieldName, type, desc) in fields)
                writer.WriteLine($"/// <param name=\"{fieldName}\">{desc}</param>");

            writer.WriteLine($"public record {@class.ClassName}(");
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
        ["Assignments.levels"] = "IEnumerable<int>?",
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

string FieldTypeConverter(string type, string @classId, string fieldName)
{

    Dictionary<string, string> exceptions = new()
    {
        ["SubscriptionObjectAttributes.Type"] = "SubscriptionType",
        ["AssignmentData.SubjectType"] = "SubjectType",
        ["StudyMaterialData.SubjectType"] = "SubjectType",
        ["CharacterImageObjectAttributes.Metadata"] = "CharacterImageMetadataObjectAttributes",
        ["PronunciationAudioObjectAttributes.Metadata"] = "PronunciationAudioMetadataObjectAttributes",
    };

    if (exceptions.TryGetValue($"{classId}.{fieldName}", out var newType))
        return newType;

    Dictionary<string, string> FieldTypeConverterDict = new()
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
        ["Array"] = "object[]",
    };

    if (FieldTypeConverterDict.TryGetValue(type, out newType))
        type = newType;
    else if (type == "Object")
    {
        type = fieldName + "ObjectAttributes";
    }
    else if (type == "Array of objects")
    {
        type = fieldName + "ObjectAttributes";
        if (classesDeclarations.FirstOrDefault(c => c.ClassName == type) is null)
            type = (fieldName.EndsWith("s") ? fieldName[0..^1] : fieldName) + "ObjectAttributes";
        type += "[]";
    }

    return type;
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

record ClassDefinition(ResourceIdentification Resource, string Id, string Name, ParameterDefinition[] Fields)
{
    public string ClassName
    {
        get
        {
            var className = Id.Replace('-', '_').SnakeToUpperCamelCase();
            if (className.EndsWith("DataStructure"))
                className = className.Replace("DataStructure", "Data");
            return className;
        }
    }
}

record ClassDeclaration(
    ResourceIdentification Resource,
    string Id,
    string Label,
    List<HtmlNode> Tables)
{
    public string ClassName
    {
        get
        {
            var className = Id.Replace('-', '_').SnakeToUpperCamelCase();
            if (className.EndsWith("DataStructure"))
                className = className.Replace("DataStructure", "Data");
            return className;
        }
    }
}

record ResourceDataStructureDefinition(string ClassName, string Description);

record ResourceDefinition(
    ResourceIdentification Identification,
    EndpointDefinition QueryAll,
    ResourceDataStructureDefinition DataStructure);

#endregion
