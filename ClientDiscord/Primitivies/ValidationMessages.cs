namespace ClientDiscord.Primitivies;

public static class ValidationMessages
{
    public static Func<string, string> NotNull { get; set; } =
        (propertyName) => $"Объект {propertyName} не может быть null";

    public static Func<string, string> NotEmpty { get; set; } =
        (propertyName) => $"Объект {propertyName} не может быть Empty";

    public static Func<string, string> InvalidProperty { get; set; } =
        (propertyName) => $"Объект {propertyName} имеет недопустимое значение";
}