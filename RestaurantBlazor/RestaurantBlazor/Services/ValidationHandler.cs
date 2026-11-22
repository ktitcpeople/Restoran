using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ochag.Validation;

namespace RestaurantBlazor.Services;

public sealed class ValidationHandler : DelegatingHandler
{
    public ValidationHandler() : base(new HttpClientHandler()) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Content != null && request.Method != HttpMethod.Get)
        {
            var json = await request.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(json))
            {
                using var document = JsonDocument.Parse(json);
                var errors = InputRules.Validate(request.RequestUri!.AbsolutePath, document.RootElement);
                if (errors.Count > 0)
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = JsonContent.Create(new { status = false, message = string.Join(" ", errors.Values.Distinct()), errors })
                    };
            }
        }
        return await base.SendAsync(request, ct);
    }
}
