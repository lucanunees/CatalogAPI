# ✨ NOVO ENDPOINT DE BUSCA ELASTICSEARCH

## 🎯 O que foi criado?

Um novo endpoint para buscar produtos no catálogo com três funcionalidades principais:

### 1️⃣ **Match Query** (Busca por Similaridade)
```
Busca: "iphone"
Resulta: iPhone 13, iPhone 14, iPhone 14 Pro...
```

### 2️⃣ **Boost** (Relevância)
```
Title (2.0x) > Id (0.5x)

Resultado:
- "Xiaomi Redmi Note 12" (encontrado no título) → Score 25.4
- "phone-xiaomi-123" (encontrado no id) → Score 2.3
```

### 3️⃣ **Fuzzy** (Tolera Erros)
```
Busca: "sammsung" (com erro)
Encontra: "samsung" ✅

Busca: "intelegnce" (com erro)
Encontra: "inteligence" ✅
```

---

## 📍 Endpoint

```
GET /api/catalog/search/products?q=termo&page=1&pageSize=10
```

---

## 🧪 Teste Rápido

### No VS Code (REST Client)
```http
GET http://localhost:5187/api/catalog/search/products?q=xiaomi
Accept: application/json
```

### Via cURL
```bash
curl "http://localhost:5187/api/catalog/search/products?q=xiaomi"
```

### Via Postman
- Método: `GET`
- URL: `http://localhost:5187/api/catalog/search/products`
- Params: `q=xiaomi`

---

## 📋 Response Exemplo

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
	  "score": 25.432898
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

## 🛠️ Arquivos Criados/Modificados

| Arquivo | O que mudou |
|---------|------------|
| `Endpoints/SearchEndpoints.cs` | ✨ **NOVO** - Endpoint de busca |
| `Program.cs` | ✏️ Registrado novo endpoint |
| `Metrics/AppMetrics.cs` | ✏️ Métricas de busca |
| `CatalogAPI.http` | ✏️ Exemplos de teste |
| `API/Endpoints/SEARCH_ENDPOINT_DOCS.md` | ✨ **NOVO** - Documentação técnica |

---

## ⚡ Como Funciona

### Query Structure (Elasticsearch DSL)
```
BOOL QUERY (Match pelo menos 1 condição)
├─ Title contém "xiaomi" com BOOST 2.0 + FUZZY
├─ Title contém "xiaomi" com FUZZY
└─ Id contém "xiaomi" com BOOST 0.5 + FUZZY
```

### Score de Relevância
```
Score = Σ (boost × relevância_do_match_no_campo)

Exemplo para "xiaomi":
- Encontrado em title → score = 2.0 × 12.7 = 25.4 ✅ (primeiro)
- Encontrado em id    → score = 0.5 × 4.6 = 2.3  (terceiro)
```

---

## ✅ Validações

O endpoint valida:
- ✅ `q` não pode estar vazio
- ✅ `q` máximo 200 caracteres
- ✅ `page` ≥ 1
- ✅ `pageSize` ≤ 100

---

## 🔐 Segurança

- Usa credenciais Elasticsearch Cloud (CloudId + ApiKey)
- Proteção contra DoS (pageSize máximo)
- Validação de entrada

---

## 📊 Métricas

O endpoint registra automaticamente:
- Total de buscas bem-sucedidas
- Total de erros
- Tempo de execução

Visualize em: `http://localhost:9090/metrics`

---

## 🚀 Próximos Passos

1. **Adicionar Filtros**: Por categoria, preço, etc.
2. **Agregações**: Contar produtos por categoria
3. **Autocomplete**: Sugestões enquanto digita
4. **Sorting**: Ordenar por preço, data, relevância
5. **Highlights**: Destacar trechos do texto encontrado

---

## 📝 Exemplo de Uso em C#

```csharp
using var client = new HttpClient();

var searchTerm = "xiaomi";
var response = await client.GetAsync(
	$"http://localhost:5187/api/catalog/search/products?q={searchTerm}"
);

var json = await response.Content.ReadAsStringAsync();
var result = JsonSerializer.Deserialize<SearchResponseDto>(json);

Console.WriteLine($"Total de resultados: {result.TotalResults}");
foreach (var product in result.Results)
{
	Console.WriteLine($"- {product.Title} (R${product.Price}, Score: {product.Score:F2})");
}
```

---

**✨ Pronto para usar!** 🎉
