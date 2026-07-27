using System.Text.Json.Serialization;

namespace CatalogAPI.Domain.Elastic
{
    public class ElasticCatalog     
    {

        [JsonPropertyName("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("PriceCents")]
        public decimal PriceCents { get; set; }

        [JsonPropertyName("Currency")]
        public string Currency { get; set; } = string.Empty;
    }
}
