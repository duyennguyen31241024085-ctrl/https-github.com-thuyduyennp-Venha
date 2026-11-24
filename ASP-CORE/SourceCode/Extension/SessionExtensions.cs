using System.Text.Json;

namespace MSSV_HoVaTen.Extension;

public static class SessionExtensions
{
    public static T Get<T>(this ISession session, string key) where T : class =>
        session.GetString(key) is string value ? JsonSerializer.Deserialize<T>(value) : null;

    public static void Set<T>(this ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value));
}
