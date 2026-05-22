# CashFlowChallenge

Solução desenvolvida em .NET 8 para controle de fluxo de caixa, composta por serviço de lançamentos financeiros e serviço de consolidação diária.

## Objetivo

Permitir o registro de lançamentos financeiros de débito e crédito, mantendo a consolidação diária de saldo de forma assíncrona, resiliente e desacoplada.

O principal requisito arquitetural atendido é garantir que o serviço de lançamentos continue disponível mesmo que o serviço de consolidação esteja indisponível.

## Arquitetura

A solução utiliza uma arquitetura orientada a eventos, com separação entre os contextos de lançamento e consolidação.

Fluxo principal:

```text
  -> salva lançamento
  -> grava mensagem na Outbox
  -> Worker publica evento no RabbitMQ
  -> Worker consome evento
  -> Worker atualiza consolidado
```

## Estrutura do Projeto

```text
CashFlowChallenge
├── docker-compose.yml
├── README.md
├── docs
│   └── architecture.md
│
└── src
    ├── Launch
    │   ├── 1-Application
    │   │   └── CashFlow.Launch.Api
    │   ├── 2-Domain
    │   │   └── CashFlow.Launch.Domain
    │   └── 3-Infra
    │       └── 3.1-Data
    │           └── CashFlow.Launch.Infrastructure
    │
    ├── Consolidation
    │   ├── 1-Application
    │   │   └── CashFlow.Consolidation.Api
    │   ├── 2-Domain
    │   │   └── CashFlow.Consolidation.Domain
    │   └── 3-Infra
    │       └── CashFlow.Consolidation.Infra
    │
    └── Worker
        └── CashFlow.Worker
```

---

# Componentes

## CashFlow.Launch.Api

Responsável pelo cadastro de lançamentos financeiros.

## CashFlow.Launch.Domain

Contém as regras e entidades do domínio de lançamentos.

## CashFlow.Launch.Infrastructure

Responsável pela persistência dos lançamentos e mensagens de outbox.

## CashFlow.Worker

Responsável por:

- publicar mensagens pendentes da outbox;
- consumir eventos do RabbitMQ;
- processar a consolidação diária.

## CashFlow.Consolidation.Api

Responsável pela consulta do saldo diário consolidado.

## CashFlow.Consolidation.Domain

Contém as regras e entidades do domínio de consolidação.

## CashFlow.Consolidation.Infra

Responsável pela persistência dos dados consolidados.

---

# Tecnologias

- .NET 8
- ASP.NET Core
- PostgreSQL
- RabbitMQ
- Entity Framework Core
- Docker Compose
- Worker Service

---

# Padrões utilizados

- DDD
- SOLID
- Repository Pattern
- Service Layer
- Outbox Pattern
- Event Driven Architecture
- Mensageria assíncrona

---

# Decisões arquiteturais

## Separação entre lançamentos e consolidação

Os serviços foram separados para evitar acoplamento direto entre o registro de lançamentos e o cálculo do saldo diário.

Com isso, o serviço de lançamento não depende da disponibilidade do consolidado.

## Uso de mensageria

O RabbitMQ foi utilizado para comunicação assíncrona entre os contextos.

Essa abordagem permite:

- desacoplamento entre serviços;
- absorção de picos de carga;
- processamento posterior em caso de falha;
- maior resiliência operacional.

## Uso do Outbox Pattern

O padrão Outbox foi adotado para reduzir o risco de perda de mensagens.

Ao registrar um lançamento, a aplicação também grava uma mensagem pendente na tabela de outbox. Um Worker fica responsável por publicar essas mensagens no RabbitMQ.

Essa decisão evita o problema de salvar o lançamento no banco, mas falhar antes de publicar o evento.

---

# Requisitos não funcionais atendidos

## Disponibilidade

O serviço de lançamentos continua disponível mesmo que o serviço de consolidação esteja fora do ar.

## Resiliência

As mensagens ficam armazenadas na outbox até serem publicadas com sucesso.

## Escalabilidade

A arquitetura permite evolução para múltiplas instâncias dos serviços e consumers.

## Integridade

O lançamento financeiro é persistido antes da publicação do evento, reduzindo risco de inconsistência.

## Desacoplamento

Os serviços não se comunicam diretamente por HTTP para processar a consolidação.

---

# Como executar

## Pré-requisitos

- Docker
- Docker Compose
- .NET 8 SDK

## Subir a aplicação

Na raiz do projeto, execute:

```bash
docker compose up --build
```

---

# Endpoints

## Launch API

Swagger:

```text
http://localhost:5001/swagger
```

## Consolidation API

Swagger:

```text
http://localhost:5002/swagger
```

## RabbitMQ Management

```text
http://localhost:15672
```

Usuário:

```text
guest
```

Senha:

```text
guest
```

---

# Banco de dados

A solução utiliza PostgreSQL com bancos separados por contexto:

- cashflow_launch
- cashflow_consolidation

---

# RabbitMQ

Exchange:

```text
cashflow.exchange
```

Queue:

```text
cashflow.entry-created.queue
```

Routing key:

```text
EntryCreatedEvent
```

---

# Testes realizados

Foram validados os seguintes cenários:

- criação de lançamento de crédito;
- criação de lançamento de débito;
- consolidação acumulada por data;
- processamento assíncrono via RabbitMQ;
- publicação de mensagens via Worker;
- manutenção do serviço de lançamento mesmo com consolidação desacoplada.

---

# Trade-offs

A solução prioriza clareza arquitetural, resiliência e simplicidade.

Algumas decisões como Kubernetes, autenticação JWT, observabilidade avançada e cache distribuído foram consideradas como evoluções futuras, mas não foram implementadas para evitar overengineering no escopo do desafio.

---

# Evoluções futuras

- Implementar Dead Letter Queue;
- Implementar idempotência no consumer;
- Melhorar estratégia de reprocessamento;
- Adicionar autenticação e autorização;
- Adicionar OpenTelemetry;
- Adicionar métricas operacionais;
- Adicionar health checks;
- Adicionar retry exponencial;
- Adicionar cache distribuído para consultas de consolidação;
- Adicionar testes automatizados mais abrangentes.

---

# Considerações finais

A solução demonstra uma arquitetura distribuída, resiliente e orientada a eventos, com foco em disponibilidade do serviço de lançamentos e processamento assíncrono da consolidação diária.
