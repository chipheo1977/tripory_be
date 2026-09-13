using System.Reflection;

namespace BuildingBlocks.Core.Domains.Abstractions.DDD;

public abstract class ValueObject : IEquatable<ValueObject>
{
    private List<PropertyInfo>? _properties;
    private List<FieldInfo>? _fields;

    public static bool operator ==(ValueObject? obj1, ValueObject? obj2)
    {
        if (obj1 is null && obj2 is null) return true;
        if (obj1 is null || obj2 is null) return false;
        return obj1.Equals(obj2);
    }

    public static bool operator !=(ValueObject? obj1, ValueObject? obj2) => !(obj1 == obj2);

    public bool Equals(ValueObject? other) => Equals(other as object);

    public override bool Equals(object? obj)
    {
        if (obj is null || GetType() != obj.GetType()) return false;
        return GetProperties().All(p => PropertiesAreEqual(obj, p))
            && GetFields().All(f => FieldsAreEqual(obj, f));
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (var prop in GetProperties())
            {
                var value = prop.GetValue(this, null);
                hash = hash * 23 + (value?.GetHashCode() ?? 0);
            }
            foreach (var field in GetFields())
            {
                var value = field.GetValue(this);
                hash = hash * 23 + (value?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    private bool PropertiesAreEqual(object obj, PropertyInfo p)
    {
        return object.Equals(p.GetValue(this, null), p.GetValue(obj, null));
    }
    private bool FieldsAreEqual(object obj, FieldInfo f)
    {
        return object.Equals(f.GetValue(this), f.GetValue(obj));
    }
    private List<PropertyInfo> GetProperties()
    {
        return _properties ??= GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToList();
    }
    private List<FieldInfo> GetFields()
    {
        return _fields ??= GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .ToList();
    }
}