using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;

namespace CatalogAPI.Domain.Elastic
{
    public class ElasticCatalog     
    {
        [JsonIgnore]
        public string? Id { get; set; }

        [JsonPropertyName("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("PriceCents")]
        public decimal PriceCents { get; set; }

        [JsonPropertyName("Currency")]
        public string Currency { get; set; } = string.Empty;
    }
}
