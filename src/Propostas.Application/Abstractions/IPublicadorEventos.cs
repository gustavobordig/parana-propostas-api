namespace Propostas.Application.Abstractions;

public interface IPublicadorEventos
{
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken) where TEvento : class;
}
