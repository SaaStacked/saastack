using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using SystemConvert = System.Convert;

namespace Common.Extensions;

/// <summary>
///     Mapping methods for copying objects to dictionaries and visa-versa.
///     Conversion patterns using (relatively expensive) reflection rather than other techniques (e.g. via JSON), but
///     employing caches for similar conversions at runtime.
///     Based on conversion patterns found in <see href="https://github.com/ServiceStack/ServiceStack" />
/// </summary>
public static class MappingExtensions
{
#if COMMON_PROJECT
    private static readonly ConcurrentDictionary<string, AssignmentDefinition> AssignmentDefinitionCache = new();
#endif
#if COMMON_PROJECT || !GENERATORS_WORKERS_PROJECT
    private static readonly ConcurrentDictionary<Type, ObjectDictionaryDefinition> ObjectDictionaryDefinitionCache =
        new();
#endif

#if COMMON_PROJECT
    /// <summary>
    ///     Auto-maps the <see cref="source" /> to a new instance of the <see cref="TTarget" /> instance
    /// </summary>
    public static TTarget Convert<TSource, TTarget>(this TSource source)
    {
        if (source.NotExists())
        {
            return default!;
        }

        if (source is TTarget target)
        {
            return target;
        }

        return (TTarget)ConvertToType(source, typeof(TTarget))!;
    }
#endif

#if !GENERATORS_WORKERS_PROJECT
    /// <summary>
    ///     Constructs a new instance of the <see cref="TObject" /> with the <see cref="values" />
    /// </summary>
    public static TObject FromObjectDictionary<TObject>(this IReadOnlyDictionary<string, object?> values)
    {
        return (TObject)FromObjectDictionary(values, typeof(TObject))!;
    }
#endif

#if !GENERATORS_WORKERS_PROJECT
    /// <summary>
    ///     Converts the instance of the <see cref="TObject" /> to a <see cref="IReadOnlyDictionary{String,Object}" />
    /// </summary>
    public static IReadOnlyDictionary<string, object?> ToObjectDictionary<TObject>(this TObject instance)
    {
        if (instance.NotExists())
        {
            return new Dictionary<string, object?>();
        }

        if (instance is IReadOnlyDictionary<string, object?> readOnlyDictionary)
        {
            return readOnlyDictionary.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        if (instance is IDictionary<string, object?> objectDictionary)
        {
            return new Dictionary<string, object?>(objectDictionary);
        }

        if (instance is IDictionary<string, string> stringDictionary)
        {
            return stringDictionary.ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
        }

        if (instance is IDictionary dictionary)
        {
            return ToObjectDictionary(dictionary);
        }

        if (instance is IEnumerable<KeyValuePair<string, object?>> objectKeyValuePairs)
        {
            return objectKeyValuePairs.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        if (instance is IEnumerable<KeyValuePair<string, string>> stringKeyValuePairs)
        {
            return stringKeyValuePairs.ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
        }

        var instanceType = instance.GetType();
        if (TryGetKeyValuePairsTypes(instanceType, out var pairType, out _, out _)
            && instance is IEnumerable enumerable)
        {
            return ToObjectDictionary(enumerable, pairType);
        }

        if (TryGetKeyValuePairTypes(instanceType, out _, out _))
        {
            return ToObjectDictionaryFromKeyValuePair(instance, instanceType);
        }

        var definition = GetObjectDictionaryDefinition(instanceType);
        var values = new Dictionary<string, object?>();
        foreach (var field in definition.Fields)
        {
            values[field.Name] = field.GetValue(instance);
        }

        return values;
    }
#endif

#if COMMON_PROJECT
    /// <summary>
    ///     Populates the public properties of the <see cref="target" /> instance with the values of matching public properties
    ///     of the
    ///     <see cref="source" /> instance, whether those values have default or non-default values.
    /// </summary>
    public static void PopulateWith<TType>(this TType target, TType source)
    {
        if (source.NotExists())
        {
            return;
        }

        var assignmentDefinition = GetAssignmentDefinition(target!.GetType(), source.GetType());
        assignmentDefinition.Populate(target, source);
    }

    /// <summary>
    ///     Populates the public properties of the <see cref="target" /> instance with the values of matching properties of the
    ///     <see cref="source" /> instance, whether those values have default or non-default values.
    /// </summary>
    public static void PopulateWith<TType>(this TType target, IReadOnlyDictionary<string, object?> source)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (source.NotExists())
        {
            return;
        }

        PopulateInstanceInternal(source, target, target.GetType());
    }
#endif

#if COMMON_PROJECT
    private static object? ConvertToType(object? source, Type targetType)
    {
        if (source.NotExists())
        {
            return GetDefaultValue(targetType);
        }

        if (targetType == typeof(object) || targetType.IsInstanceOfType(source))
        {
            return source;
        }

        if (TryGetObjectDictionary(source, out var sourceDictionary))
        {
            return FromObjectDictionary(sourceDictionary, targetType);
        }

        if (TryConvertCollection(targetType, source, out var convertedCollection))
        {
            return convertedCollection;
        }

        if (source is string stringValue
            && targetType != typeof(string)
            && targetType is { IsValueType: false, IsEnum: false }
            && !IsOptionalType(targetType))
        {
            return stringValue.FromJson(targetType);
        }

        if (!targetType.IsValueType
            && targetType != typeof(string)
            && targetType is { IsAbstract: false, IsInterface: false })
        {
            return CreateObject(targetType, source.ToObjectDictionary());
        }

        return ConvertValue(targetType, source);
    }

    private static AssignmentDefinition GetAssignmentDefinition(Type targetType, Type sourceType)
    {
        var cacheKey = $"{sourceType.FullName}>{targetType.FullName}";
        return AssignmentDefinitionCache.GetOrAdd(cacheKey, CreateAssignmentDefinition(targetType, sourceType));
    }

    private static AssignmentDefinition CreateAssignmentDefinition(Type targetType, Type sourceType)
    {
        var definition = new AssignmentDefinition();
        var readableMembers = GetMembers(sourceType, true);
        var writableMembers = GetMembers(targetType, false);

        foreach (var member in readableMembers)
        {
            if (!writableMembers.TryGetValue(member.Key, out var targetMember))
            {
                continue;
            }

            definition.Add(member.Value, targetMember);
        }

        return definition;
    }

    private static Dictionary<string, AssignmentMember> GetMembers(Type type, bool isReadable)
    {
        var members = new Dictionary<string, AssignmentMember>(StringComparer.Ordinal);

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (isReadable)
            {
                var getter = property.GetGetMethod(true);
                if (getter.Exists() && !getter.IsStatic)
                {
                    members[property.Name] = new AssignmentMember(property);
                }

                continue;
            }

            var setter = property.GetSetMethod(true);
            if (setter.Exists() && !setter.IsStatic)
            {
                members[property.Name] = new AssignmentMember(property);
            }
        }

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (isReadable || !field.IsInitOnly)
            {
                members[field.Name] = new AssignmentMember(field);
            }
        }

        return members;
    }
#endif

#if COMMON_PROJECT || !GENERATORS_WORKERS_PROJECT

    private static object? FromObjectDictionary(IEnumerable<KeyValuePair<string, object?>>? values, Type objectType)
    {
        if (values.NotExists())
        {
            return null;
        }

        var lookup = values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        if (TryConvertDictionary(objectType, lookup, out var convertedDictionary))
        {
            return convertedDictionary;
        }

        if (typeof(IDictionary).IsAssignableFrom(objectType)
            && objectType is { IsAbstract: false, IsInterface: false })
        {
            var dictionary = (IDictionary)Activator.CreateInstance(objectType)!;
            foreach (var pair in lookup)
            {
                dictionary[pair.Key] = pair.Value;
            }

            return dictionary;
        }

        return CreateObject(objectType, lookup);
    }

    private static object CreateObject(Type objectType, IReadOnlyDictionary<string, object?> values)
    {
        var lookup = values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var constructor = SelectConstructor(objectType, lookup);

        object instance;
        if (constructor.Exists())
        {
            var arguments = constructor.GetParameters()
                .Select(parameter => GetConstructorValue(parameter, lookup))
                .ToArray();
            instance = constructor.Invoke(arguments);
        }
        else
        {
            instance = Activator.CreateInstance(objectType)!;
        }

        PopulateInstanceInternal(lookup, instance, objectType);
        return instance;
    }

    private static void PopulateInstanceInternal(IEnumerable<KeyValuePair<string, object?>> values, object target,
        Type targetType)
    {
        var definition = GetObjectDictionaryDefinition(targetType);
        foreach (var pair in values)
        {
            if (!TryGetFieldDefinition(definition, pair.Key, out var field))
            {
                continue;
            }

            field.SetValue(target, pair.Value);
        }
    }

    private static bool TryGetFieldDefinition(ObjectDictionaryDefinition definition, string key,
        out ObjectDictionaryFieldDefinition field)
    {
        if (definition.FieldsMap.TryGetValue(key, out field!))
        {
            return true;
        }

        var pascalCaseKey = key.ToPascalCase();
        return definition.FieldsMap.TryGetValue(pascalCaseKey, out field!);
    }

    private static ConstructorInfo? SelectConstructor(Type objectType, IReadOnlyDictionary<string, object?> values)
    {
        if (objectType.IsValueType)
        {
            return null;
        }

        return objectType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .OrderByDescending(constructor => constructor.GetParameters()
                .Count(parameter => parameter.Name.Exists() && values.ContainsKey(parameter.Name)))
            .ThenBy(constructor => constructor.GetParameters()
                .Count(parameter => !parameter.HasDefaultValue
                                    && (parameter.Name.HasNoValue() || !values.ContainsKey(parameter.Name))))
            .ThenBy(constructor => constructor.GetParameters().Length)
            .FirstOrDefault();
    }

    private static object? GetConstructorValue(ParameterInfo parameter, IReadOnlyDictionary<string, object?> values)
    {
        if (parameter.Name.Exists() && values.TryGetValue(parameter.Name, out var value))
        {
            return ConvertValue(parameter.ParameterType, value);
        }

        if (parameter.HasDefaultValue)
        {
            return parameter.DefaultValue;
        }

        return GetDefaultValue(parameter.ParameterType);
    }

    private static ObjectDictionaryDefinition GetObjectDictionaryDefinition(Type type)
    {
        return ObjectDictionaryDefinitionCache.GetOrAdd(type, CreateObjectDictionaryDefinition);
    }

    private static ObjectDictionaryDefinition CreateObjectDictionaryDefinition(Type type)
    {
        var definition = new ObjectDictionaryDefinition();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var getter = property.GetGetMethod(true);
            if (getter.NotExists() || getter.IsStatic)
            {
                continue;
            }

            var setter = property.GetSetMethod(true);
            definition.Add(new ObjectDictionaryFieldDefinition(
                property.Name,
                property.PropertyType,
                instance => property.GetValue(instance),
                setter.NotExists() || setter.IsStatic
                    ? null
                    : (instance, value) => property.SetValue(instance, value)));
        }

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            definition.Add(new ObjectDictionaryFieldDefinition(
                field.Name,
                field.FieldType,
                instance => field.GetValue(instance),
                field.IsInitOnly
                    ? null
                    : (instance, value) => field.SetValue(instance, value)));
        }

        return definition;
    }

    private static IReadOnlyDictionary<string, object?> ToObjectDictionary(IDictionary dictionary)
    {
        var values = new Dictionary<string, object?>();
        foreach (var key in dictionary.Keys)
        {
            var name = ConvertValue(typeof(string), key)?.ToString();
            if (name.HasNoValue())
            {
                continue;
            }

            values[name] = dictionary[key];
        }

        return values;
    }

    private static IReadOnlyDictionary<string, object?> ToObjectDictionary(IEnumerable values, Type pairType)
    {
        var keyProperty = pairType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public)!;
        var valueProperty = pairType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)!;

        var dictionary = new Dictionary<string, object?>();
        foreach (var entry in values)
        {
            var key = ConvertValue(typeof(string), keyProperty.GetValue(entry))?.ToString();
            if (key.HasNoValue())
            {
                continue;
            }

            dictionary[key] = valueProperty.GetValue(entry);
        }

        return dictionary;
    }

    private static IReadOnlyDictionary<string, object?> ToObjectDictionaryFromKeyValuePair(object instance,
        Type pairType)
    {
        var keyProperty = pairType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public)!;
        var valueProperty = pairType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)!;

        var key = ConvertValue(typeof(string), keyProperty.GetValue(instance))?.ToString() ?? "Key";
        var value = valueProperty.GetValue(instance);

        return new Dictionary<string, object?>
        {
            [key] = value
        };
    }

    private static object? ConvertValue(Type targetType, object? value)
    {
        if (value == DBNull.Value)
        {
            value = null;
        }

        if (value.TryGetOptionalValue(out var descriptor))
        {
            value = descriptor!.IsNone
                ? null
                : descriptor.ContainedValue;
        }

        if (value.NotExists())
        {
            return GetDefaultValue(targetType);
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (IsOptionalType(targetType))
        {
            var containedType = targetType.GenericTypeArguments[0];
            var convertedValue = ConvertValue(containedType, value);
            return Activator.CreateInstance(targetType, convertedValue);
        }

        var nullableType = Nullable.GetUnderlyingType(targetType);
        if (nullableType.Exists())
        {
            return ConvertValue(nullableType, value);
        }

        var sourceType = value.GetType();
        var implicitCast = GetCastMethod(sourceType, targetType, "op_Implicit");
        if (implicitCast.Exists())
        {
            return implicitCast.Invoke(null, [value]);
        }

        var explicitCast = GetCastMethod(sourceType, targetType, "op_Explicit");
        if (explicitCast.Exists())
        {
            return explicitCast.Invoke(null, [value]);
        }

        if (TryGetObjectDictionary(value, out var dictionary))
        {
            if (TryConvertDictionary(targetType, dictionary, out var convertedDictionary))
            {
                return convertedDictionary;
            }

            if (targetType is { IsAbstract: false, IsInterface: false })
            {
                return CreateObject(targetType, dictionary);
            }
        }

        if (TryConvertCollection(targetType, value, out var convertedCollection))
        {
            return convertedCollection;
        }

        if (targetType.IsEnum)
        {
            return value is string or Enum
                ? Enum.Parse(targetType, value.ToString()!, true)
                : Enum.ToObject(targetType, value);
        }

        if (sourceType.IsEnum && targetType != typeof(string))
        {
            return SystemConvert.ChangeType(value, Enum.GetUnderlyingType(sourceType), CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(Guid) && value is string guidValue)
        {
            return Guid.Parse(guidValue);
        }

        if (targetType == typeof(DateTime) && value is string dateTimeValue)
        {
            return DateTime.Parse(dateTimeValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (targetType == typeof(DateTimeOffset) && value is string dateTimeOffsetValue)
        {
            return DateTimeOffset.Parse(dateTimeOffsetValue, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        }

        if (targetType == typeof(TimeSpan) && value is string timeSpanValue)
        {
            return TimeSpan.Parse(timeSpanValue, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(string))
        {
            return value.ToString();
        }

        if (value is string jsonString
            && targetType is { IsValueType: false, IsAbstract: false, IsInterface: false })
        {
            return jsonString.FromJson(targetType);
        }

        return SystemConvert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static MethodInfo? GetCastMethod(Type sourceType, Type targetType, string methodName)
    {
        foreach (var method in sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name == methodName
                && method.ReturnType == targetType
                && method.GetParameters().FirstOrDefault()?.ParameterType == sourceType)
            {
                return method;
            }
        }

        foreach (var method in targetType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name == methodName
                && method.ReturnType == targetType
                && method.GetParameters().FirstOrDefault()?.ParameterType == sourceType)
            {
                return method;
            }
        }

        return null;
    }

    private static object? GetDefaultValue(Type targetType)
    {
        return targetType.IsValueType || IsOptionalType(targetType)
            ? Activator.CreateInstance(targetType)
            : null;
    }

    private static bool TryGetObjectDictionary(object value, out IReadOnlyDictionary<string, object?> dictionary)
    {
        switch (value)
        {
            case IReadOnlyDictionary<string, object?> readOnlyDictionary:
                dictionary = readOnlyDictionary.ToDictionary(pair => pair.Key, pair => pair.Value);
                return true;

            case IDictionary<string, object?> genericDictionary:
                dictionary = new Dictionary<string, object?>(genericDictionary);
                return true;

            case IDictionary<string, string> genericStringDictionary:
                dictionary = genericStringDictionary.ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
                return true;

            case IDictionary nonGenericDictionary:
                dictionary = ToObjectDictionary(nonGenericDictionary);
                return true;

            default:
                dictionary = new Dictionary<string, object?>();
                return false;
        }
    }

    private static bool TryConvertDictionary(Type targetType, IReadOnlyDictionary<string, object?> values,
        out object? converted)
    {
        converted = null;
        if (!targetType.IsGenericType)
        {
            return false;
        }

        var genericType = targetType.GetGenericTypeDefinition();
        if (genericType != typeof(Dictionary<,>)
            && genericType != typeof(IDictionary<,>)
            && genericType != typeof(IReadOnlyDictionary<,>))
        {
            return false;
        }

        var keyType = targetType.GenericTypeArguments[0];
        var valueType = targetType.GenericTypeArguments[1];
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType)!;
        foreach (var pair in values)
        {
            var key = ConvertValue(keyType, pair.Key);
            if (key.NotExists())
            {
                continue;
            }

            dictionary[key] = ConvertValue(valueType, pair.Value);
        }

        converted = dictionary;
        return true;
    }

    private static bool TryConvertCollection(Type targetType, object value, out object? converted)
    {
        converted = null;
        if (targetType == typeof(string) || value is string || value is not IEnumerable enumerable)
        {
            return false;
        }

        if (value is IDictionary dictionary && TryConvertDictionaryCollection(targetType, dictionary, out converted))
        {
            return true;
        }

        var elementType = GetCollectionElementType(targetType);
        if (elementType.NotExists())
        {
            return false;
        }

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in enumerable.Cast<object?>())
        {
            list.Add(ConvertValue(elementType, item));
        }

        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            converted = array;
            return true;
        }

        if (targetType.IsAssignableFrom(listType))
        {
            converted = list;
            return true;
        }

        if (targetType is { IsAbstract: false, IsInterface: false })
        {
            var instance = Activator.CreateInstance(targetType);
            if (instance is IList targetList)
            {
                foreach (var item in list)
                {
                    targetList.Add(item);
                }

                converted = instance;
                return true;
            }
        }

        converted = list;
        return true;
    }

    private static bool TryConvertDictionaryCollection(Type targetType, IDictionary values, out object? converted)
    {
        converted = null;
        if (!TryGetKeyValuePairsTypes(targetType, out _, out var keyType, out var valueType))
        {
            return false;
        }

        var concreteType = targetType.IsInterface || targetType.IsAbstract
            ? typeof(List<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType))
            : targetType;

        if (Activator.CreateInstance(concreteType) is not IList list)
        {
            return false;
        }

        var pairType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
        foreach (var key in values.Keys)
        {
            var convertedKey = ConvertValue(keyType, key);
            if (convertedKey.NotExists())
            {
                continue;
            }

            var convertedValue = ConvertValue(valueType, values[key]);
            list.Add(Activator.CreateInstance(pairType, convertedKey, convertedValue)!);
        }

        converted = list;
        return true;
    }

    private static Type? GetCollectionElementType(Type targetType)
    {
        if (targetType.IsArray)
        {
            return targetType.GetElementType();
        }

        if (!targetType.IsGenericType)
        {
            return null;
        }

        var genericType = targetType.GetGenericTypeDefinition();
        return genericType == typeof(IEnumerable<>)
               || genericType == typeof(ICollection<>)
               || genericType == typeof(IList<>)
               || genericType == typeof(List<>)
               || genericType == typeof(IReadOnlyCollection<>)
               || genericType == typeof(IReadOnlyList<>)
            ? targetType.GenericTypeArguments[0]
            : null;
    }

    private static bool TryGetKeyValuePairsTypes(Type type, out Type pairType, out Type keyType, out Type valueType)
    {
        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(IEnumerable<>)
                || genericType == typeof(ICollection<>)
                || genericType == typeof(IList<>)
                || genericType == typeof(List<>)
                || genericType == typeof(IReadOnlyCollection<>)
                || genericType == typeof(IReadOnlyList<>))
            {
                var candidatePairType = type.GenericTypeArguments[0];
                if (TryGetKeyValuePairTypes(candidatePairType, out keyType, out valueType))
                {
                    pairType = candidatePairType;
                    return true;
                }
            }
        }

        foreach (var @interface in type.GetInterfaces())
        {
            if (!@interface.IsGenericType || @interface.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                continue;
            }

            var candidatePairType = @interface.GenericTypeArguments[0];
            if (TryGetKeyValuePairTypes(candidatePairType, out keyType, out valueType))
            {
                pairType = candidatePairType;
                return true;
            }
        }

        pairType = null!;
        keyType = null!;
        valueType = null!;
        return false;
    }

    private static bool TryGetKeyValuePairTypes(Type type, out Type keyType, out Type valueType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            keyType = type.GenericTypeArguments[0];
            valueType = type.GenericTypeArguments[1];
            return true;
        }

        keyType = null!;
        valueType = null!;
        return false;
    }

    private static bool IsOptionalType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>);
    }

    private sealed class ObjectDictionaryDefinition
    {
        public List<ObjectDictionaryFieldDefinition> Fields { get; } = [];

        public Dictionary<string, ObjectDictionaryFieldDefinition> FieldsMap { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public void Add(ObjectDictionaryFieldDefinition field)
        {
            Fields.Add(field);
            FieldsMap[field.Name] = field;
        }
    }

    private sealed class ObjectDictionaryFieldDefinition(
        string name,
        Type type,
        Func<object, object?> getValue,
        Action<object, object?>? setValue)
    {
        public string Name { get; } = name;

        public Type Type { get; } = type;

        public object? GetValue(object instance)
        {
            return getValue(instance);
        }

        public void SetValue(object instance, object? value)
        {
            if (setValue.NotExists())
            {
                return;
            }

            setValue(instance, ConvertValue(Type, value));
        }
    }
#endif

#if COMMON_PROJECT
    private sealed class AssignmentDefinition
    {
        private readonly List<AssignmentEntry> _entries = [];

        public void Add(AssignmentMember source, AssignmentMember target)
        {
            _entries.Add(new AssignmentEntry(source, target));
        }

        public void Populate(object target, object source)
        {
            foreach (var entry in _entries)
            {
                entry.Assign(target, source);
            }
        }
    }

    private sealed class AssignmentEntry(AssignmentMember source, AssignmentMember target)
    {
        public void Assign(object targetInstance, object sourceInstance)
        {
            var value = source.GetValue(sourceInstance);
            target.SetValue(targetInstance, value);
        }
    }

    private sealed class AssignmentMember
    {
        private readonly FieldInfo? _field;
        private readonly PropertyInfo? _property;

        public AssignmentMember(PropertyInfo property)
        {
            _property = property;
            MemberType = property.PropertyType;
        }

        public AssignmentMember(FieldInfo field)
        {
            _field = field;
            MemberType = field.FieldType;
        }

        public Type MemberType { get; }

        public object? GetValue(object instance)
        {
            return _property.Exists()
                ? _property.GetValue(instance)
                : _field!.GetValue(instance);
        }

        public void SetValue(object instance, object? value)
        {
            var converted = ConvertValue(MemberType, value);

            if (_property.Exists())
            {
                _property.SetValue(instance, converted);
                return;
            }

            _field!.SetValue(instance, converted);
        }
    }
#endif
}