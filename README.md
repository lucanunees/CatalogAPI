# CatalogAPI

API de Catálogo do FIAPGame, responsável por gerenciar jogos (CRUD), expor endpoints públicos para consulta e endpoints administrativos protegidos por JWT. Integra-se com RabbitMQ via MassTransit e utiliza PostgreSQL com Entity Framework Core e padrão Outbox para consistência entre banco e mensagens.

- Plataforma: .NET 8 (ASP.NET Core, Minimal APIs)
- Linguagem: C# 12
- Banco de dados: PostgreSQL (EF Core)
- Mensageria: RabbitMQ (MassTransit, Outbox)
- Autenticação/Autorização: JWT Bearer
- Observabilidade: Health Checks
- Documentação: Swagger/OpenAPI

## Estrutura do projeto

- Domain
    - Entities
        - Game: entidade de jogo (Id, Title, PriceCents, Currency, CreatedAtUtc)
        - Order, LibraryItem: entidades de pedidos e biblioteca (mapeadas no DbContext)
- Infrastructure
    - Persistence
        - CatalogDbContext: mapeia Games, Orders, LibraryItems; configura índices/constraints; registra Inbox/Outbox do MassTransit
- API
    - Endpoints
        - GamesEndpoints: endpoints REST para jogos (GET/POST/PUT/DELETE)
    - Program.cs: composition root com DI, EF Core, JWT, Swagger, HealthChecks e MassTransit (RabbitMQ + Outbox)

## Endpoints principais

- GET /games
    - Lista jogos em ordem alfabética
- GET /games/{id}
    - Retorna jogo por Id
- POST /games (requer role Admin)
    - Cria jogo
- PUT /games/{id} (requer role Admin)
    - Atualiza jogo
- DELETE /games/{id} (requer role Admin)
    - Remove jogo
- GET /health
    - Health check (inclui verificação do DbContext)

Observação: Existem mapeamentos adicionais para pedidos e biblioteca: MapOrdersEndpoints() e MapLibraryEndpoints().

## Autenticação e Autorização

- JWT Bearer (Issuer, Audience, Key via configuração)
- Endpoints de escrita em /games exigem autorização com role Admin
- Swagger configurado com esquema Bearer

Exemplo de header:
Authorization: Bearer {seu_token_jwt}

## Mensageria (MassTransit + RabbitMQ)

- Outbox EF Core habilitado para garantir consistência (UsePostgres, UseBusOutbox)
- Consumer registrado: PaymentProcessedConsumer
- Exchange principal de catálogo: fcg.catalog (topic) para OrderPlacedEventV1
- Fila de consumo: catalog.payment-processed
    - Bind na exchange fcg.payments (topic) com routing key v1.payment-processed

## Requisitos

- .NET SDK 8.0
- PostgreSQL
- RabbitMQ
- Variáveis/Configurações de ambiente:
    - ConnectionStrings:Default
    - Jwt:Issuer, Jwt:Audience, Jwt:Key
    - RabbitMq:Host, RabbitMq:Username, RabbitMq:Password, RabbitMq:VirtualHost (opcional, padrão “/”)

## Configuração

Exemplo de appsettings.json (ajuste conforme seu ambiente):
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=catalogdb;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "https://seu-issuer",
    "Audience": "fiapgame-clients",
    "Key": "chave-secreta-em-dev-apenas"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```


## Migrações e Banco

- Crie e aplique migrações do EF Core antes de executar.
- O DbContext mapeia:
    - Games: índice por Title
    - Orders: índices por UserId e GameId
    - LibraryItems: índice único (UserId, GameId)
- MassTransit adiciona tabelas de Inbox/Outbox.

Comandos (exemplo):
- dotnet ef migrations add Initial --project CatalogAPI
- dotnet ef database update --project CatalogAPI

Certifique-se de ter o pacote de design e ferramentas EF instalados globalmente se necessário:
- dotnet tool install --global dotnet-ef

## Execução

- Via CLI:
    - dotnet build
    - dotnet run
- Aplicação expõe Swagger em:
    - /swagger
- Health check:
    - /health

## Uso rápido (cURL)

- Listar jogos:
    - curl http://localhost:5000/games
- Criar jogo (admin):
    - curl -X POST http://localhost:5000/games -H "Authorization: Bearer {TOKEN}" -H "Content-Type: application/json" -d "{\"title\":\"Jogo X\",\"priceCents\":999,\"currency\":\"BRL\"}"

## Boas práticas adotadas

- Minimal APIs com endpoints agrupados
- AsNoTracking em consultas de leitura
- JWT com validação de emissor/audiência/assinatura e ClockSkew reduzido
- Swagger com definição de segurança Bearer
- MassTransit Outbox para evitar perda/duplicação de mensagens
- HealthChecks incluindo verificação do DbContext


#### Por Marco Antonio