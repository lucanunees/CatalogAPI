# 🔍 Endpoint de Busca Elasticsearch - Documentação

## 📋 Resumo

Novo endpoint criado para realizar consultas no Elasticsearch com:
- **Match Query**: Busca por similaridade de texto
- **Boost**: Aumenta a relevância de campos específicos (Title tem 2x mais peso)
- **Fuzzy Matching**: Tolera pequenos erros de digitação (até 2 caracteres)
- **Índice**: `catalog`

---

## 🛣️ Endpoint

```
GET /api/catalog/search/products?q=query&page=1&pageSize=10
```

### Query Parameters

| Parâmetro | Tipo | Obrigatório | Descrição | Padrão |
|-----------|------|-------------|-----------|--------|
| `q` | string | ✅ Sim | Termo de busca (máx 200 caracteres) | - |
| `page` | int | ❌ Não | Número da página (começa em 1) | 1 |
| `pageSize` | int | ❌ Não | Itens por página (máx 100) | 10 |

---

## 📊 Response (Status 200)

```json
{
  "query": "xiaomi",
  "totalResults": 1523,
  "page": 1,
  "pageSize": 10,
  "hasNextPage": true,
  "results": [
	{
	  "id": "phone-123",
	  "title": "Xiaomi Redmi Note 12",
	  "price": 899.99,
	  "currency": "BRL",
	  "score": 25.432898  // Score de relevância do ES
	},
	{
	  "id": "phone-456",
	  "title": "Xiaomi Poco X5",
	  "price": 1099.99,
	  "currency": "BRL",
	  "score": 23.112345
	}
  ],
  "executionTimeMs": 142
}
```

---

## 🧪 Exemplos de Uso

### 1️⃣ Busca Simples
```bash
curl "http://localhost:5000/api/catalog/search/products?q=iphone"
```

### 2️⃣ Com Paginação
```bash
curl "http://localhost:5000/api/catalog/search/products?q=samsung&page=2&pageSize=20"
```

### 3️⃣ Com Erro de Digitação (Fuzzy)
```bash
# A busca por "samsng" vai encontrar "samsung" mesmo com erro
curl "http://localhost:5000/api/catalog/search/products?q=samsng"
```

### 4️⃣ Via C# HttpClient
```csharp
using var client = new HttpClient();

var query = "xiaomi";
var page = 1;
var pageSize = 10;

var response = await client.GetAsync(
	$"http://localhost:5000/api/catalog/search/products?q={query}&page={page}&pageSize={pageSize}"
);

var content = await response.Content.ReadAsStringAsync();
var searchResult = JsonSerializer.Deserialize<SearchResponseDto>(content);

foreach (var product in searchResult.Results)
{
	Console.WriteLine($"{product.Title} - R$ {product.Price} (Score: {product.Score})");
}
```

---

## ⚙️ Como Funciona a Query

### Estrutura da Query Elasticsearch

```
Bool Query
├── Should (pelo menos 1 deve match)
│   ├── Match Title com BOOST 2.0 + FUZZY
│   ├── Match Title com FUZZY
│   └── Match Id com BOOST 0.5 + FUZZY
└── MinimumShouldMatch: 1
```

### Explicação de Cada Cláusula

| Cláusula | Boost | Fuzzy | Propósito |
|----------|-------|-------|-----------|
| Match no Title (boost 2.0) | 2.0x | AUTO | Campo  mais importante, resulta são mais relevantes |
| Match no Title (fallback) | 1.0x | AUTO | Fallback para resultados que não tiveram match com boost |
| Match no Id | 0.5x | AUTO | Busca em ID tem metade da importância do título |

### Fuzziness: "AUTO"

- **Para strings com 1-2 caracteres**: Permite 0 edições
- **Para strings com 3-5 caracteres**: Permite 1 edição
- **Para strings com 6+ caracteres**: Permite 2 edições

**Exemplos:**
```
Busca: "samsng"     → Encontra: "samsung"  (1 erro tolera é < 6 chars)
Busca: "intelegnce" → Encontra: "inteligence" (2 erros tolera > 6 chars)
Busca: "ao"         → Encontra: "ação" (0 erros tolera)
```

---

## 🔐 Segurança

O endpoint valida:
- ✅ Query não pode estar vazia
- ✅ Query limitada a 200 caracteres
- ✅ Page deve ser >= 1
- ✅ PageSize limitado a 100 (previne abuso)
- ✅ Autenticação via Elasticsearch Cloud (CloudId + ApiKey)

---

## 📈 Métricas

O endpoint registra:
- **search_completed_total**: Contador de buscas bem-sucedidas
- **search_errors_total**: Contador de erros (elasticsearch_error, general_error)
- **business_request_duration_seconds**: Tempo de execução (histogram)

Visualize em `/metrics` (Prometheus)

---

## ❌ Códigos de Erro

| Status | Cenário |
|--------|---------|
| **200** | Sucesso |
| **400** | Query vazia, muito grande, ou parâmetros inválidos |
| **500** | Erro na conexão com Elasticsearch |

---

## 🚀 Próximos Passos (Opcional)

1. **Agregações**: Adicione faceting por categoria, preço, etc.
   ```csharp
   .Aggregations(a => a
	  .Terms("by_category", t => t.Field("category"))
   )
   ```

2. **Filters**: Filtre por preço mín/máx
   ```csharp
   .Query(q => q.Bool(b => b
	  .Must(m => m.Price > 100 && m.Price < 1000)
   ))
   ```

3. **Sort**: Ordene por preço, relevância, data
   ```csharp
   .Sort(s => s.Field(f => f.Price))
   ```

4. **Autocomplete**: Use prefix queries para sugestões
   ```csharp
   .Query(q => q.Match_All())  // Para typeahead
   ```

---

## 📝 Arquivos Modificados

| Arquivo | Mudança |
|---------|---------|
| `Endpoints/SearchEndpoints.cs` | ✨ Novo endpoint de busca |
| `Program.cs` | Registrado `app.MapSearchEndpoints()` |
| `Metrics/AppMetrics.cs` | ✏️ Adicionadas métricas de busca |

---

## 🔗 Referências

- [Elasticsearch Query DSL](https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl.html)
- [Match Query](https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-match-query.html)
- [Fuzziness](https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-fuzzy-query.html)
- [Bool Query](https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-bool-query.html)
- [Elastic.Clients.Elasticsearch C# API](https://github.com/elastic/elasticsearch-net)
