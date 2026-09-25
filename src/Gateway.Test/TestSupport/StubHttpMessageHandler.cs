namespace Gateway.Test.TestSupport;

/// <summary>
/// Reemplaza el HttpMessageHandler real de un HttpClient nombrado para
/// simular las respuestas de Identity.Api sin red. No se usa NSubstitute aqui
/// porque HttpMessageHandler.SendAsync es protected: NSubstitute no permite
/// configurar miembros protegidos sin exponerlos primero.
/// </summary>
public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
  public HttpRequestMessage? LastRequest { get; private set; }

  public string? LastRequestBody { get; private set; }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    LastRequest = request;
    LastRequestBody = request.Content is null
      ? null
      : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

    return responder(request);
  }
}
