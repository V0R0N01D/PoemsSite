using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Poems.Common.Embeddings;

public static class EmbeddingServiceExtensions
{
    public static IServiceCollection AddPoemEmbeddings(this IServiceCollection services, IConfiguration configuration)
    {
        var url = configuration["VectorSearch:Url"] ?? "http://embeddings:8000/";
        if (!Uri.TryCreate(url.TrimEnd('/') + "/", UriKind.Absolute, out var address) ||
            address.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("VectorSearch:Url must be an absolute HTTP(S) URL.");

        services.AddHttpClient<PoemEmbeddingClient>(client =>
        {
            client.BaseAddress = address;
            // A long poem can contain hundreds of fragments. Queries have a shorter deadline at the site.
            client.Timeout = TimeSpan.FromMinutes(10);
        });
        return services;
    }
}
