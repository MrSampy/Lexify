namespace Lexify.Domain.Entities;

public sealed class Language
{
    public short Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string NativeName { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public short SortOrder { get; private set; }

    private Language() { }

    public Language(string code, string name, string nativeName, bool isActive = true, short sortOrder = 0)
    {
        Code = code;
        Name = name;
        NativeName = nativeName;
        IsActive = isActive;
        SortOrder = sortOrder;
    }

    public void Toggle() => IsActive = !IsActive;
}
