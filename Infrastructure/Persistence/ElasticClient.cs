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
            var settings_client = new ElasticsearchClientSettings(new Uri(settings.CloudId))
                .Authentication(new BasicAuthentication(settings.CloudId, settings.ApiKey));

            this._client = new ElasticsearchClient(settings_client);
        }
        public async Task<bool> Create(T catalog, string index)
        {
            var response = await this._client.IndexAsync(catalog, index);

            if (response.IsValidResponse)
                return true;
            return false;
        }

        public async Task<IReadOnlyCollection<T>> GetCatalogAsync(string indexName)
        {
            var response = await this._client.SearchAsync<T>(indexName);
            return response.Documents;
        }
    }
}
