using System.Text.Json;

namespace Magbat.Sim;

/// <summary>
/// Оценивает размер сообщений в байтах по их JSON-представлению
/// </summary>
public static class MessageSize
{
    public static int Of(ClientMessage message)
    {
        // SentAt не входит: это метка эксперимента, а не часть протокола
        return Measure(new { message.GameId, Command = Tagged(message.Command) });
    }

    public static int Of(ServerMessage message)
    {
        return Measure(new
        {
            message.GameId,
            message.Player,
            message.Error,
            Changes = message.Changes.Select(Tagged).ToArray()
        });
    }

    // Тип записывается как object, иначе сериализатор взял бы поля базового класса
    // и поля наследников потерялись бы. Имя типа нужно настоящему протоколу
    private static object Tagged(object value) => new { Type = value.GetType().Name, Data = value };

    private static int Measure(object value) => JsonSerializer.SerializeToUtf8Bytes(value).Length;
}