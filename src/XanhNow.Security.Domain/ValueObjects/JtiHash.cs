namespace XanhNow.Security.Domain.ValueObjects;

public sealed record JtiHash : StringValueObject
{
    private JtiHash(string value) : base(value, nameof(JtiHash), 160)
    {
    }

    public static JtiHash From(string value) => new(value);
}
