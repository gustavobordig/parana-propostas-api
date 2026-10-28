namespace Propostas.Infrastructure.Messaging;

internal sealed record RabbitMqOptions(string Host, ushort Porta, string VirtualHost, string Usuario, string Senha)
{
    public static RabbitMqOptions DaUrl(string url)
    {
        var uri = new Uri(url);
        var credenciais = uri.UserInfo.Split(':', 2);
        var virtualHost = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

        return new RabbitMqOptions(
            uri.Host,
            (ushort)(uri.IsDefaultPort || uri.Port < 0 ? 5672 : uri.Port),
            string.IsNullOrEmpty(virtualHost) ? "/" : virtualHost,
            Uri.UnescapeDataString(credenciais.ElementAtOrDefault(0) ?? "guest"),
            Uri.UnescapeDataString(credenciais.ElementAtOrDefault(1) ?? "guest"));
    }
}
