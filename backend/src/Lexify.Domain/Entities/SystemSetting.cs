namespace Lexify.Domain.Entities;

public sealed class SystemSetting
{
    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public string ValueType { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private SystemSetting() { }

    public SystemSetting(string key, string value, string valueType, string? description = null)
    {
        Key = key;
        Value = value;
        ValueType = valueType;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string value, Guid updatedBy)
    {
        Value = value;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
