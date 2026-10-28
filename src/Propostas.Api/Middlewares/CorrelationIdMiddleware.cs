using Propostas.Infrastructure.Observability;
using Serilog.Context;

namespace Propostas.Api.Middlewares;

internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, CorrelationContext correlacao)
    {
        if (Guid.TryParse(http.Request.Headers[CorrelationContext.Header], out var recebido))
            correlacao.Id = recebido;

        http.Response.Headers[CorrelationContext.Header] = correlacao.Id.ToString();

        using (LogContext.PushProperty("CorrelationId", correlacao.Id))
        {
            await next(http);
        }
    }
}
