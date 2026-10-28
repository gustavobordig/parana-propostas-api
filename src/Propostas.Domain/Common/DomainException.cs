namespace Propostas.Domain.Common;

public sealed class DomainException(string mensagem) : Exception(mensagem);
