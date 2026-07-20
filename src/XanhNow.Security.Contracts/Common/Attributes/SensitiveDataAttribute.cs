namespace XanhNow.Security.Contracts.Common.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class SensitiveDataAttribute(string category) : Attribute
{
    public string Category { get; } = string.IsNullOrWhiteSpace(category) ? "sensitive" : category;
}
