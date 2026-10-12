using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
internal static class MetadataInspection
{
    public static void Print(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        foreach (var handle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(handle);
            string owner = reader.GetString(type.Name);
            if (!owner.StartsWith("Character`") && !owner.StartsWith("WorldEntity`") && !owner.StartsWith("CharacterView`") && !owner.Contains("RotationView") && !owner.Contains("DirectableCharacter") && !owner.Contains("MotionReport") && !owner.Contains("IdentifiableCharacter") && !new[] { "AutomaticCharacter2DRotationView", "CharacterPublicState", "CharacterState", "FollowingAIControlCharacterMotor", "Motor", "PlayerControlCharacterMotor", "DirectableCharacterMotor", "PlayerMotorRestrictor", "Nullable`1", "Object", "Il2CppObjectBase" }.Contains(owner)) continue;
            Console.WriteLine("TYPE " + owner);
            foreach (var ph in type.GetProperties()) Console.WriteLine("PROPERTY " + reader.GetString(reader.GetPropertyDefinition(ph).Name));
            foreach (var mh in type.GetMethods()) { var name = reader.GetString(reader.GetMethodDefinition(mh).Name); if (!name.StartsWith("set_") && !name.StartsWith("get_")) Console.WriteLine("METHOD " + name); }
        }
    }
}



