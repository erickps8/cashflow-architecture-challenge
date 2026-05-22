## Fluxo operacional detalhado

```text
┌──────────────┐
│   Cliente    │
└──────┬───────┘
       │ HTTP
       ▼
┌──────────────────────────────┐
│     CashFlow.Launch.Api      │
│                              │
│ - Valida lançamento          │
│ - Salva Entry                │
│ - Salva OutboxMessage        │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│     PostgreSQL Launch        │
│                              │
│ - Entries                    │
│ - OutboxMessages             │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│      CashFlow.Worker         │
│                              │
│ - Busca mensagens pendentes  │
│ - Publica no RabbitMQ        │
│ - Consome eventos            │
│ - Atualiza consolidação      │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│          RabbitMQ            │
│                              │
│ - Exchange                   │
│ - Queue                      │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│ PostgreSQL Consolidation     │
│                              │
│ - DailyConsolidation         │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│ CashFlow.Consolidation.Api   │
│                              │
│ - Consulta consolidado       │
└──────────────────────────────┘
```