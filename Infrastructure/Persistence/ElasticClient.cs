using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace CatalogAPI.Infrastructure.Persistence
{

    public interface IElasticClient<T>
    {
        Task<IReadOnlyCollection<T>> GetCatalogAsync(IndexName indexName);
        Task<bool> Create(T log, IndexName index);
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
        public async Task<bool> Create(T log, IndexName index)
        {
           var response = await this._client.IndexAsync(log, index.ToString());

            if (response.IsValidResponse)
                return true;
            return false;
        }

        public async Task<IReadOnlyCollection<T>> GetCatalogAsync(IndexName indexName)
        {
            var response = await this._client.SearchAsync<T>(indexName);
            return response.Documents;
        }
    }
}
