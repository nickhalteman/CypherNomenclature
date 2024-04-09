using Godot;
using Godot.NativeInterop;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

public class SerializeAttribute : Attribute
{
    public SerializeAttribute()
    {
    }
}

public struct StupidConvert
{
    private MethodInfo stupidConvertMethodInfo;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object Convert(object obj)
    {
        return stupidConvertMethodInfo.Invoke(null, new object[] { obj });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Convert(object obj, Type type)
    {
        return genericStupidConvert.MakeGenericMethod(type).Invoke(null, new object[] { obj });
    }

    private static MethodInfo genericStupidConvert = typeof(StupidConvert).GetMethod(nameof(GenericStupidConvert), BindingFlags.NonPublic | BindingFlags.Static);
    private static T GenericStupidConvert<T>(object obj)
    {
        return (T)obj;
    }

    public static StupidConvert GetStupidConvert(Type type)
    {
        return new StupidConvert { stupidConvertMethodInfo = genericStupidConvert.MakeGenericMethod(type) };
    }
}



[Autoload(500)]
public abstract class Serializer
{
    private struct SerializerInfo
    {
        public MethodInfo checkType;
        public Type type;
    }

    
    
    private static List<SerializerInfo> serializerInfo = new List<SerializerInfo>();
    protected static Serializer countSerializer = new PrimitiveSerializer(typeof(uint));
    public static void _Ready()
    {
        foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if(type.IsSubclassOf(typeof(Serializer)))
            {
                MethodInfo checkType = type.GetMethod("CheckType", new Type[]{ typeof(Type) });

                if(checkType == null)
                {
                    GD.PrintErr($"Type: {type.FullName} doesn't implement CheckType");
                }

                serializerInfo.Add(new SerializerInfo()
                {
                    checkType = checkType,
                    type = type
                });

            }
        }

        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if(type.GetCustomAttribute<SerializeAttribute>() != null)
            {
                GetSerializer(type);
            }
        }
    }

    public static Serializer GetSerializer<T>()
    {
        return GetSerializer(typeof(T));
    }

    private static Dictionary<Type,Serializer> typeSerializerCache = new Dictionary<Type,Serializer>();
    public static Serializer GetSerializer(Type type)
    {
        Serializer ret = null;
        if(typeSerializerCache.TryGetValue(type,out ret))
        {
            return ret;
        }

        foreach (SerializerInfo info in serializerInfo)
        {
            if ((bool)info.checkType.Invoke(null, new object[] { type }))
            {
                ret = (Serializer)Activator.CreateInstance(info.type, new object[] { type });
                typeSerializerCache[type] = ret;
                return ret;
            }
        }

        GD.PrintErr($"Couldn't create Serializer for Type: {type.FullName}");
        return null;
    }

    public Type serializerType { get; private set; }
    protected Serializer(Type type)
    {
        serializerType = type;
    }

    public abstract void Serialize(object obj, MemoryStream stream);
    public abstract object Deserialize(MemoryStream stream);

    public abstract string SerializeString(object obj, string linePrefix);
}

public class CSteamIDSerializer : Serializer
{
    private static Serializer ulongSerializer = new PrimitiveSerializer(typeof(ulong));
    public static bool CheckType(Type type)
    {
        return type == typeof(CSteamID);
    }
    public CSteamIDSerializer(Type type) : base(type)
    {
    }

    public override object Deserialize(MemoryStream stream)
    {
        return (CSteamID)(ulong)ulongSerializer.Deserialize(stream);
    }

    public override void Serialize(object obj, MemoryStream stream)
    {
        ulongSerializer.Serialize(((CSteamID)obj).m_SteamID, stream);
    }

    public override string SerializeString(object obj, string linePrefix)
    {
        return ((CSteamID)obj).ToString();
    }
}

public class PrimitiveSerializer : Serializer
{
    public static HashSet<Type> validTypes = new HashSet<Type>()
    {
        typeof(bool),typeof(sbyte),typeof(byte),typeof(short),typeof(ushort),typeof(int),typeof(uint),typeof(long),typeof(ulong),typeof(Half),typeof(float),typeof(double)
    };

    public static bool CheckType(Type type)
    {
        return validTypes.Contains(type);
    }

    public override object Deserialize(MemoryStream stream)
    {
        return deserializePrimative.Invoke(null,new object[] { stream });
    }

    public override void Serialize(object obj, MemoryStream stream)
    {
        serializePrimative.Invoke(null,new object[] { obj, stream});
    }

    public PrimitiveSerializer(Type type) : base(type)
    {
        deserializePrimative = typeof(PrimitiveSerializer).GetMethod(nameof(DeserializePrimitive)).MakeGenericMethod(type);
        serializePrimative = typeof(PrimitiveSerializer).GetMethod(nameof(SerializePrimitive)).MakeGenericMethod(type);
    }

    private MethodInfo serializePrimative;
    public static void SerializePrimitive<T>(T data, MemoryStream stream) where T : struct
    {
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<T>()];
        MemoryMarshal.Write<T>(buffer, ref data);
        stream.Write(buffer);
    }

    private MethodInfo deserializePrimative;

    public static T DeserializePrimitive<T>(MemoryStream stream) where T : struct
    {
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<T>()];
        stream.Read(buffer);
        return MemoryMarshal.Read<T>(buffer);
    }

    public override string SerializeString(object obj, string linePrefix)
    {
        return obj.ToString();
    }
}

public class EnumSerializer : Serializer
{
    private Serializer underlyingSerializer;
    private StupidConvert stupidConvertSerialize;
    private StupidConvert stupidConvertDeserialize;


    public EnumSerializer(Type type) : base(type)
    {
        underlyingSerializer = new PrimitiveSerializer(type.GetEnumUnderlyingType());

        stupidConvertSerialize = StupidConvert.GetStupidConvert(underlyingSerializer.serializerType);
        stupidConvertDeserialize = StupidConvert.GetStupidConvert(serializerType);
    }

    public static bool CheckType(Type type)
    {
        return type.IsEnum && PrimitiveSerializer.CheckType(type.GetEnumUnderlyingType());
    }


    public override object Deserialize(MemoryStream stream)
    {
        object underlyingObject = underlyingSerializer.Deserialize(stream);
        if (Enum.IsDefined(serializerType, underlyingObject))
        {
            return stupidConvertDeserialize.Convert(underlyingObject);
        }
        throw new Exception("EnumSerializer: enum underlying value not defined");
    }

    public override void Serialize(object obj, MemoryStream stream)
    {
        underlyingSerializer.Serialize(stupidConvertSerialize.Convert(obj),stream);
    }

    public override string SerializeString(object obj, string linePrefix)
    {
        return obj.ToString();
    }
}

public class StringSerializer : Serializer
{
    public StringSerializer(Type type) : base(type)
    {
    }

    public static bool CheckType(Type type)
    {
        return type == typeof(string);
    }

    public override void Serialize(object data, MemoryStream stream)
    {
        BinaryWriter writer = new BinaryWriter(stream,Encoding.Unicode);
        writer.Write((string)data);
    }
    public override string Deserialize(MemoryStream stream)
    {
        BinaryReader reader = new BinaryReader(stream, Encoding.Unicode);
        return reader.ReadString();
    }

    public override string SerializeString(object obj, string linePrefix)
    {
        return obj.ToString();
    }
}

public class ListSerializer : Serializer
{
    public static bool CheckType(Type type)
    {
        return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }

    private Serializer elementSerializer;
    public ListSerializer(Type type) : base(type)
    {
        elementSerializer = GetSerializer(type.GenericTypeArguments[0]);
    }

    public override void Serialize(object data, MemoryStream stream)
    {
        IList list = (IList)data;
        countSerializer.Serialize((uint)list.Count, stream);
        foreach (object item in list)
        {
            elementSerializer.Serialize(item, stream);
        }
    }
    public override object Deserialize(MemoryStream stream)
    {
        IList list = Activator.CreateInstance(serializerType) as IList;
        uint count = (uint)countSerializer.Deserialize(stream);

        for (uint i = 0; i < count; i++)
        {
            list.Add(elementSerializer.Deserialize(stream));
        }

        return list;
    }

    public override string SerializeString(object obj, string linePrefix)
    {
        string ret = $"[\n";

        if(obj == null)
        {
            GD.Print("obj is null...");
        }
        GD.Print(obj.GetType().FullName);

        IList list = (IList)obj;
        foreach (object item in list)
        {
            ret += $"{linePrefix}\t{elementSerializer.SerializeString(item, linePrefix + "\t")},\n";
        }
        return $"{ret}{linePrefix}]";

    }
}

public class ArraySerializer : Serializer
{
    public static bool CheckType(Type type)
    {
        return type.IsArray;
    }

    private Serializer elementSerializer;
    public ArraySerializer(Type type) : base(type)
    {
        elementSerializer = GetSerializer(type.GetElementType());
    }

    public override void Serialize(object data, MemoryStream stream)
    {
        Array array = (Array)data;
        countSerializer.Serialize((uint)array.Length, stream);
        foreach (object item in array)
        {
            elementSerializer.Serialize(item, stream);
        }
    }
    public override object Deserialize(MemoryStream stream)
    {
        uint count = (uint)countSerializer.Deserialize(stream);
        Array array = Array.CreateInstance(elementSerializer.serializerType, count);

        for (uint i = 0; i < count; i++)
        {
            array.SetValue(elementSerializer.Deserialize(stream),i);
        }

        return array;
    }

    public override string SerializeString(object obj, string linePrefix)
    {
        string ret = $"[\n";
        Array array = (Array)obj;
        foreach (object item in array)
        {
            ret += $"{linePrefix}\t{elementSerializer.SerializeString(item, linePrefix + "\t")},\n";
        }
        return $"{ret}{linePrefix}]";
    }
}


public class HashSetSerializer : Serializer {
    public static bool CheckType(Type type)
    {
        return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>);
    }

    private PropertyInfo countProperty;
    private MethodInfo addMethod;
    private Serializer elementSerializer;
    public HashSetSerializer(Type type) : base(type)
    {
        elementSerializer = GetSerializer(type.GenericTypeArguments[0]);
        countProperty = type.GetProperty("Count");
        addMethod = type.GetMethod("Add");
    }

    public override void Serialize(object data, MemoryStream stream)
    {

        uint count = Convert.ToUInt32(countProperty.GetValue(data));
        countSerializer.Serialize(count, stream);
        IEnumerable enumerable = (IEnumerable)data;
        foreach (object item in enumerable)
        {
            elementSerializer.Serialize(item, stream);
        }
    }
    public override object Deserialize(MemoryStream stream)
    {
        object hashSet = Activator.CreateInstance(serializerType);
        uint count = (uint)countSerializer.Deserialize(stream);
        for (uint i = 0; i < count; i++)
        {
            addMethod.Invoke(hashSet, new object[] { elementSerializer.Deserialize(stream) });
        }
        return hashSet;
    }

    public override string SerializeString(object obj, string linePrefix)
    {
        string ret = $"{{\n";
        IEnumerable enumerable = (IEnumerable)obj;
        foreach (object item in enumerable)
        {
            ret += $"{linePrefix}\t{elementSerializer.SerializeString(item, linePrefix + "\t")},\n";
        }
        return $"{ret}{linePrefix}}}";
    }
}

public class DictionarySerializer : Serializer {

    private Serializer keySerializer;
    private Serializer valueSerializer;
    public DictionarySerializer(Type type) : base(type)
    {
        keySerializer = GetSerializer(type.GenericTypeArguments[0]);
        valueSerializer = GetSerializer(type.GenericTypeArguments[1]);
    }

    public static bool CheckType(Type type)
    {
        return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }

    public override void Serialize(object obj, MemoryStream stream)
    {
        IDictionary dictionary = (IDictionary)obj;
        countSerializer.Serialize((uint)dictionary.Count, stream);
        foreach(object key in dictionary.Keys)
        {
            keySerializer.Serialize(key, stream);
            valueSerializer.Serialize(dictionary[key], stream);
        }
    }

    public override object Deserialize(MemoryStream stream)
    {
        IDictionary dictionary = (IDictionary)Activator.CreateInstance(serializerType);
        uint count = (uint)countSerializer.Deserialize(stream);
        for(uint i = 0; i < count; i++)
        {
            object key = keySerializer.Deserialize(stream);
            dictionary[key] = valueSerializer.Deserialize(stream);
        }
        return dictionary;
    }
    public override string SerializeString(object obj, string linePrefix)
    {
        string ret = "{\n";
        IDictionary dictionary = (IDictionary)obj;
        foreach (object key in dictionary.Keys)
        {
            ret += $"{linePrefix}\t{keySerializer.SerializeString(key, linePrefix + "\t")}: {valueSerializer.SerializeString(dictionary[key],linePrefix + "\t")},\n";
        }
        return ret + linePrefix + "}";
    }
}

public class CustomSerializer : Serializer
{
    private struct MemberData : IComparable<MemberData>
    {
        public Serializer serializer;
        public MemberAccessor accessor;

        public int CompareTo(MemberData other)
        {
            return accessor.CompareTo(other.accessor);
        }
    }
    private abstract class MemberAccessor : IComparable<MemberAccessor>
    {
        public abstract object GetValue(object obj);
        public abstract void SetValue(object obj, object value);
        public abstract string Name { get; }
        public abstract Type MemberType { get; }
        public int CompareTo(MemberAccessor other)
        {
            return Name.CompareTo(other.Name);
        }
        public static MemberAccessor GetAccessor(MemberInfo memberInfo)
        {
            if(memberInfo.MemberType == MemberTypes.Field)
            {
                return new FieldAccessor(memberInfo);
            } else if(memberInfo.MemberType == MemberTypes.Property)
            {
                return new PropertyAccessor(memberInfo);
            }
            return null;
        }
    }
    private class FieldAccessor : MemberAccessor
    {
        private FieldInfo fieldInfo;
        public override object GetValue(object obj)
        {
            return fieldInfo.GetValue(obj);
        }
        public override void SetValue(object obj, object value)
        {
            fieldInfo.SetValue(obj, value);
        }
        public FieldAccessor(MemberInfo memberInfo)
        {
            fieldInfo = memberInfo as FieldInfo;
        }
        public override string Name => fieldInfo.Name;
        public override Type MemberType => fieldInfo.FieldType;
    }

    private class PropertyAccessor : MemberAccessor
    {
        private PropertyInfo propertyInfo;
        public override object GetValue(object obj)
        {
            return propertyInfo.GetValue(obj);
        }
        public override void SetValue(object obj, object value)
        {
            propertyInfo.SetValue(obj, value);
        }
        public PropertyAccessor(MemberInfo memberInfo)
        {
            propertyInfo = memberInfo as PropertyInfo;
        }
        public override string Name => propertyInfo.Name;
        public override Type MemberType => propertyInfo.PropertyType;
    }

    private List<MemberData> typeMemberData = new List<MemberData>();
    public CustomSerializer(Type type) : base(type)
    {
        foreach(MemberInfo memberInfo in type.GetMembers())
        {
            if(memberInfo.GetCustomAttribute<SerializeAttribute>() == null)
            {
                continue;
            }

            MemberAccessor memberAccessor = MemberAccessor.GetAccessor(memberInfo);
            if(memberAccessor == null)
            {
                continue;
            }

            typeMemberData.Add(new MemberData()
            {
                accessor = memberAccessor,
                serializer = GetSerializer(memberAccessor.MemberType)
            });
        }

        typeMemberData.Sort();
    }

    public static bool CheckType(Type type)
    {
        return type.GetCustomAttribute<SerializeAttribute>() != null;
    }

    public override object Deserialize(MemoryStream stream)
    {

        object ret = Activator.CreateInstance(serializerType);

        foreach(MemberData memberData in typeMemberData)
        {
            memberData.accessor.SetValue(ret,memberData.serializer.Deserialize(stream));
        }

        if (serializerType.GetInterface(nameof(IOnDeserialize)) != null)
        {
            object thing = (ret as IOnDeserialize).OnDeserialize();
            if (thing != null)
            {
                ret = thing;
            }

        }


        return ret;
    }

    public override void Serialize(object obj, MemoryStream stream)
    {
        foreach(MemberData memberData in typeMemberData)
        {
            memberData.serializer.Serialize(memberData.accessor.GetValue(obj), stream);
        }
    }

    public override string SerializeString(object obj, string linePrefix)
    {
        string ret = "{\n";
        foreach(MemberData memberData in typeMemberData)
        {
            ret += $"{linePrefix}\t{memberData.accessor.Name}: {memberData.serializer.SerializeString(memberData.accessor.GetValue(obj), linePrefix + "\t")},\n";
        }
        return ret + linePrefix + "}";
    }
}