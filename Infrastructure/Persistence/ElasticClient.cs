using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace CatalogAPI.Infrastructure.Persistence
{

    public interface IElasticClient<T>
    {
        Task<IReadOnlyCollection<T>> GetCatalogAsync(string indexName);
        Task<bool> Create(T catalog, string index);
    }
    public class ElasticClient<T> : IElasticClient<T>
    {
        private readonly ElasticsearchClient _client;

        public ElasticClient(IElasticSettings settings)
        {
            var clientSettings = new ElasticsearchClientSettings(
                 new Uri(settings.CloudId))
                 .Authentication(new ApiKey(settings.ApiKey));

            this._client = new ElasticsearchClient(clientSettings);
        }
        public async Task<bool> Create(T catalog, string index)
        {
            var response = await _client.IndexAsync(catalog, x => x.Index(index));


            if (response.IsValidResponse)
                return true;

            // Log do erro
            Console.WriteLine($"Erro ao criar documento no Elasticsearch: {response.DebugInformation}");
            return false;
        }

        public async Task<IReadOnlyCollection<T>> GetCatalogAsync(string indexName)
        {
            var response = await this._client.SearchAsync<T>(indexName);
            return response.Documents;
        }
    }
}
