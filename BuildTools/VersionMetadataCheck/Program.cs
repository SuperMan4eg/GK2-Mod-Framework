using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.IO.Compression;

// Inspect PE metadata without loading or executing the plugin or its dependencies.
try
{
    if (args.Length != 2) throw new Exception("Usage: VersionMetadataCheck <DLL-or-ZIP> <expected-version>");
    string path = args[0], expected = args[1];
    byte[] bytes;
    if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
    {
        using var zip = ZipFile.OpenRead(path);
        var entries = zip.Entries.Where(e => e.FullName.Replace('\\', '/') == "BepInEx/plugins/GK2.Framework.dll").ToArray();
        if (entries.Length != 1) throw new Exception("Expected exactly one Framework DLL in ZIP");
        using var source = entries[0].Open();
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        bytes = buffer.ToArray();
    }
    else bytes = File.ReadAllBytes(path);
    using var stream = new MemoryStream(bytes);
    using var pe = new PEReader(stream);
    var reader = pe.GetMetadataReader();
    var assembly = reader.GetAssemblyDefinition();
    string? plugin = null, constant = null, file = null, product = null;
    foreach (var handle in assembly.GetCustomAttributes())
    {
        var attribute = reader.GetCustomAttribute(handle);
        string name = AttributeTypeName(attribute);
        var blob = reader.GetBlobReader(attribute.Value);
        if (blob.ReadUInt16() != 1) throw new Exception("Invalid attribute prolog");
        if (name == "System.Reflection.AssemblyFileVersionAttribute") file = blob.ReadSerializedString();
        if (name == "System.Reflection.AssemblyInformationalVersionAttribute") product = blob.ReadSerializedString();
    }
    foreach (var handle in reader.TypeDefinitions)
    {
        var type = reader.GetTypeDefinition(handle);
        if (reader.GetString(type.Namespace) != "GK2.Framework" || reader.GetString(type.Name) != "FrameworkPlugin") continue;
        foreach (var attributeHandle in type.GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(attributeHandle);
            if (AttributeTypeName(attribute) != "BepInEx.BepInPlugin") continue;
            var blob = reader.GetBlobReader(attribute.Value);
            if (blob.ReadUInt16() != 1) throw new Exception("Invalid BepInPlugin prolog");
            var guid = blob.ReadSerializedString();
            var title = blob.ReadSerializedString();
            if (guid != "ru.superman4eg.gk2.framework" || title != "GK2 Mod Framework") throw new Exception("Unexpected plugin identity");
            if (plugin != null) throw new Exception("Duplicate BepInPlugin attribute");
            plugin = blob.ReadSerializedString();
        }
        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            if (reader.GetString(field.Name) != "PluginVersion") continue;
            var value = reader.GetConstant(field.GetDefaultValue());
            if (value.TypeCode != ConstantTypeCode.String) throw new Exception("PluginVersion is not a string constant");
            constant = System.Text.Encoding.Unicode.GetString(reader.GetBlobBytes(value.Value));
        }
    }
    string expectedAssembly = expected + ".0";
    if (plugin != expected || constant != expected || assembly.Version.ToString() != expectedAssembly || file != expectedAssembly || product?.Split('+')[0] != expected)
        throw new Exception($"Version mismatch: expected={expected}; assembly={assembly.Version}; file={file}; product={product}; BepInPlugin={plugin}; PluginVersion={constant}");
    Console.WriteLine($"VERSION_METADATA_PASS: {expected}; assembly/file={expectedAssembly}; BepInPlugin/PluginVersion/product match; {Path.GetFileName(path)}");
    return 0;

    string AttributeTypeName(CustomAttribute attribute)
    {
        EntityHandle owner = attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
            _ => throw new Exception("Unsupported attribute constructor")
        };
        if (owner.Kind == HandleKind.TypeReference)
        {
            var t = reader.GetTypeReference((TypeReferenceHandle)owner);
            return reader.GetString(t.Namespace) + "." + reader.GetString(t.Name);
        }
        if (owner.Kind == HandleKind.TypeDefinition)
        {
            var t = reader.GetTypeDefinition((TypeDefinitionHandle)owner);
            return reader.GetString(t.Namespace) + "." + reader.GetString(t.Name);
        }
        throw new Exception("Unsupported attribute owner");
    }
}
catch (Exception error)
{
    Console.Error.WriteLine("VERSION_METADATA_FAIL: " + error.Message);
    return 1;
}
