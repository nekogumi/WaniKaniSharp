using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

var lines = File.ReadAllLines("Attributes.csv");

Print(lines.Length);

var classes = new Dictionary<string, List<(string name, string type, string desc)>>();
var classesChapter = new Dictionary<string, string>();

foreach (var line in lines)
{
    var parts = line.Substring(1, line.Length - 2).Split(new[] { "\";\"" }, StringSplitOptions.None);
    var @class = parts[1];
    var subclass = parts[2];
    var name = parts[4];
    var type = parts[5];
    var desc = parts[6];
    //Console.WriteLine($"{@class}::{subclass}.{name} [{type}]; // {desc}");
    if (@class != "Class")
    {
        if (!classes.TryGetValue(subclass, out var fields))
            classes[subclass] = fields = new List<(string name, string type, string desc)>();
        fields.Add((name, type, desc));
        classesChapter[subclass] = @class;
    }
}

readonly Regex SnakeCaseConverter = new(@"(_[a-z])", RegexOptions.Compiled);

string SnakeToCamel(string name)
{
    name = string.Join("", from part in name.Split('_')
                           select char.ToUpper(part[0]) + part.Substring(1));
    return name;
}

var typeDict = new Dictionary<string, string>()
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

var objectTypeClassNames = new Dictionary<string, string>()
{
    ["MetadataObjectAttributes"] = "PronunciationAudioMetadataObjectAttributes",
};

var unknowns = new HashSet<string>();
var skipClasses = new HashSet<string>()
{
    "ReadingObjectAttributes2",
};

var classesName = new Dictionary<string, string>()
{
    ["common-attributes"] = "SubjectData",
};

var attributeTypes = new Dictionary<string, string>()
{
    ["SubscriptionObjectAttributes.Type"] = "SubscriptionType",
    ["AssignmentData.SubjectType"] = "SubjectType",
};

foreach (var x in classes)
{
    var className = SnakeToCamel(x.Key.Replace('-', '_'));
    if (skipClasses.Contains(className) || classesName.ContainsKey(x.Key)) continue;
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
    classesName[x.Key] = className;
}

using (var writer = new StreamWriter("..\\WaniKaniSharp\\APIClasses.cs"))
{
    writer.WriteLine("// AUTO GENERATED FILE -- DO NOT EDIT");
    writer.WriteLine("using System;");
    writer.WriteLine("");
    writer.WriteLine("namespace Nekogumi.WaniKani.API");
    writer.WriteLine("{");

    foreach (var chapter in new HashSet<string>(classesChapter.Values))
    {
        writer.WriteLine($"    #region {chapter}");
        writer.WriteLine();
        foreach (var x in classes)
        {
            if (!classesName.TryGetValue(x.Key, out var className) || classesChapter[x.Key] != chapter) continue;
            var fields = new List<(string fieldName, string type, string desc)>();
            for (int i = 0; i < x.Value.Count; i++)
            {
                var fieldName = SnakeToCamel(x.Value[i].name);
                if (!attributeTypes.TryGetValue(className + "." + fieldName, out var type)
                    && !typeDict.TryGetValue(x.Value[i].type, out type))
                {
                    type = x.Value[i].type;
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
                            type = (fieldName.EndsWith("s") ? fieldName.Substring(0, fieldName.Length - 1) : fieldName) + "ObjectAttributes";
                        if (objectTypeClassNames.ContainsKey(type))
                            type = objectTypeClassNames[type];
                        type += "[]";
                    }
                    else
                        unknowns.Add(type);
                }
                fields.Add((fieldName, type, x.Value[i].desc));
            }

            writer.WriteLine($"    /// <summary></summary>");
            foreach (var field in fields)
                writer.WriteLine($"    /// <param name=\"{field.fieldName}\">{field.desc}</param>");

            writer.WriteLine($"    public record {className}(");
            for (int i = 0; i < fields.Count; i++)
                writer.WriteLine($"        {fields[i].type} {fields[i].fieldName}{(i == fields.Count - 1 ? ");" : ",")}");
            writer.WriteLine($"    ");
        }

        writer.WriteLine("    #endregion");
        writer.WriteLine();

    }
    writer.WriteLine("}");
}

foreach (var type in unknowns)
{
    Print(type);
}
