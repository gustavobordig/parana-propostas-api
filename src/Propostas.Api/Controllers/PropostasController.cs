using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Propostas.Application.Propostas;
using Propostas.Application.Propostas.Obter;

namespace Propostas.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/propostas")]
[Produces("application/json")]
public sealed class PropostasController(ObterPropostaHandler obterProposta) : ControllerBase
{
    [HttpGet("{clienteId:guid}")]
    [EndpointSummary("Consulta a proposta de crédito de um cliente.")]
    [EndpointDescription("A proposta é gerada de forma assíncrona após o cadastro. Um 404 logo após o cadastro significa que o evento ainda está sendo processado.")]
    [ProducesResponseType<PropostaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorClienteId(Guid clienteId, CancellationToken cancellationToken)
    {
        var proposta = await obterProposta.HandleAsync(clienteId, cancellationToken);

        return proposta is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Proposta não encontrada.")
            : Ok(proposta);
    }
}
