# CashFlowChallenge

Solução desenvolvida em .NET 8 para controle de fluxo de caixa, composta por microsserviços de lançamentos financeiros, consolidação diária, autenticação e processamento assíncrono de mensagens.

## Objetivo

Permitir o registro de lançamentos financeiros de débito e crédito, mantendo a consolidação diária de saldo de forma assíncrona, resiliente e desacoplada.

---

## Arquitetura

A solução utiliza arquitetura orientada a eventos, separando os contextos de lançamento, consolidação, autenticação e processamento assíncrono.

Fluxo principal:

```text
Launch API
  -> salva lançamento financeiro
  -> grava mensagem na Outbox

Worker
  -> lê mensagens pendentes da Outbox
  -> publica evento no RabbitMQ
  -> marca mensagem como processada
  -> controla retry em caso de falha
  -> consome evento EntryCreatedEvent
  -> chama regra de consolidação diária

Consolidation Domain/Infra
  -> aplica regra de consolidação
  -> persiste DailyConsolidation

Consolidation API
  -> consulta consolidados
  -> reprocessa consolidados
```

Essa abordagem evita comunicação síncrona direta entre o lançamento e a consolidação, reduzindo acoplamento e aumentando a resiliência da solução.

---

## Estrutura do Projeto

```text
CashFlowChallenge
├── docker-compose.yml
├── README.md
├── docs
│   └── architecture.md
│
├── src
│   ├── Launch
│   │   ├── 1-Application
│   │   │   └── CashFlow.Launch.Api
│   │   ├── 2-Domain
│   │   │   └── CashFlow.Launch.Domain
│   │   └── 3-Infra
│   │       └── 3.1-Data
│   │           └── CashFlow.Launch.Infrastructure
│   │
│   ├── Consolidation
│   │   ├── 1-Application
│   │   │   └── CashFlow.Consolidation.Api
│   │   ├── 2-Domain
│   │   │   └── CashFlow.Consolidation.Domain
│   │   └── 3-Infra
│   │       └── CashFlow.Consolidation.Infra
│   │
│   ├── Worker
│   │   └── CashFlow.Worker
│   │
│   └── Auth
│       └── CashFlow.Auth.Api
│
└── tests
    └── CashFlow.Tests
```


---

## Componentes

### CashFlow.Launch.Api

Responsável por expor os endpoints de lançamentos financeiros.

### CashFlow.Launch.Domain

Contém as entidades, regras de domínio, serviços, interfaces e eventos relacionados aos lançamentos.

### CashFlow.Launch.Infrastructure

Responsável pela persistência dos lançamentos e das mensagens de Outbox.

### CashFlow.Worker

Responsável por:

- consultar mensagens pendentes na Outbox;
- publicar eventos no RabbitMQ;
- marcar mensagens como processadas;
- controlar tentativas de retry em caso de falha.

### CashFlow.Consolidation.Api

Responsável por expor os endpoints de consulta e reprocessamento da consolidação diária.

### CashFlow.Consolidation.Domain

Contém as entidades e regras de domínio da consolidação diária.

### CashFlow.Consolidation.Infra

Responsável pela persistência dos dados consolidados.

### CashFlow.Auth.Api

Responsável por autenticação, geração de token JWT e controle de roles por endpoint.

---

## Tecnologias

- .NET 8
- ASP.NET Core
- ASP.NET Identity
- JWT Bearer Authentication
- PostgreSQL
- RabbitMQ
- Entity Framework Core
- Docker Compose
- Worker Service
- xUnit
- Moq
- FluentAssertions

---

## Padrões utilizados

- DDD
- SOLID
- Repository Pattern
- Service Layer
- Outbox Pattern
- Event Driven Architecture
- Mensageria assíncrona
- JWT Authentication
- Autorização granular por roles

---

## Decisões arquiteturais

### Separação entre lançamentos e consolidação

Os serviços foram separados para evitar acoplamento direto entre o registro de lançamentos e o cálculo do saldo diário.

Com isso, o serviço de lançamentos não depende da disponibilidade do serviço de consolidação.

### Uso de mensageria

O RabbitMQ foi utilizado para comunicação assíncrona entre os contextos.

Essa abordagem permite:

- desacoplamento entre serviços;
- absorção de picos de carga;
- processamento posterior em caso de falha;
- maior resiliência operacional.

### Uso do Outbox Pattern

O padrão Outbox foi adotado para reduzir o risco de perda de mensagens.

Ao registrar um lançamento, a aplicação também grava uma mensagem pendente na tabela de Outbox. Um Worker fica responsável por publicar essas mensagens no RabbitMQ.

Essa decisão evita o problema de salvar o lançamento no banco, mas falhar antes da publicação do evento.

### Autenticação e autorização

A autenticação foi isolada em um serviço próprio, utilizando ASP.NET Identity e JWT.

As APIs de negócio validam o token JWT e utilizam roles granulares por endpoint, como:

- `Entry.Create`
- `Entry.GetAll`
- `DailyConsolidation.GetAll`
- `DailyConsolidation.Reprocess`

Essa abordagem evita autorização genérica demais e permite controle mais preciso sobre as permissões.

### Reprocessamento

O reprocessamento manual da Outbox não foi adotado, pois o Worker já executa retry automático das mensagens pendentes.

O endpoint de reprocessamento existente atua apenas sobre a consolidação diária, recalculando o saldo com base em:

```text
Balance = TotalCredits - TotalDebits
```

---

## Requisitos não funcionais atendidos

### Disponibilidade

O serviço de lançamentos continua disponível mesmo que a consolidação esteja indisponível.

### Resiliência

As mensagens ficam armazenadas na Outbox até serem publicadas com sucesso.

### Escalabilidade

A arquitetura permite evolução para múltiplas instâncias dos serviços e consumers.

### Integridade

O lançamento financeiro é persistido antes da publicação do evento, reduzindo o risco de inconsistência.

### Segurança

A solução possui autenticação JWT e autorização granular por roles.

### Desacoplamento

Os serviços não se comunicam diretamente por HTTP para processar a consolidação.

---

## Como executar

### Pré-requisitos

- Docker
- Docker Compose
- .NET 8 SDK

### Subir a aplicação

Na raiz do projeto, execute:

```bash
docker compose up --build
```

---

## Endpoints

### Launch API

```text
http://localhost:5001/swagger
```

### Consolidation API

```text
http://localhost:5002/swagger
```

### Auth API

```text
http://localhost:5003/swagger
```

# Autenticação e autorização

A autenticação da solução utiliza JWT Bearer com ASP.NET Identity.

As permissões são controladas por roles específicas por endpoint.

## Perfis disponíveis

- Entry.Create
- Entry.GetAll
- DailyConsolidation.GetAll
- DailyConsolidation.Reprocess

## Criar usuário

Acessar:

```text
http://localhost:5003/swagger
```

Executar:

```text
POST /api/Auth/register
```

Exemplo:

```json
{
  "username": "admin",
  "email": "admin@cashflow.com",
  "password": "123456",
  "roles": [
    "entries-create",
    "entries",
    "daily-consolidations",
    "daily-consolidations-reprocess",
    "outbox-messages"
  ]
}
```

## Realizar login

Executar:

```text
POST /api/Auth/login
```

Exemplo:

```json
{
  "username": "admin",
  "password": "123456"
}
```

O endpoint retornará um token JWT.

## Utilizar token no Swagger

1. copiar o token retornado no login;
2. acessar o Swagger da API desejada;
3. clicar em `Authorize`;
4. colar apenas o token JWT.


### RabbitMQ Management

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

## Banco de dados

A solução utiliza PostgreSQL com bancos separados por contexto:

- `cashflow_launch`
- `cashflow_consolidation`
- `cashflow_auth`

---

## RabbitMQ

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

## Testes automatizados

O projeto possui testes automatizados cobrindo os principais fluxos arquiteturais e de negócio.

Cenários cobertos:

- criação de lançamentos financeiros válidos;
- validação de lançamentos inválidos;
- geração de mensagens na Outbox;
- consulta de lançamentos;
- consolidação diária de créditos;
- consolidação diária de débitos;
- criação de consolidação quando ainda não existe;
- reprocessamento do saldo diário;
- tratamento de erro com notificações;
- publicação de mensagens pelo Worker;
- controle de retry quando ocorre falha na publicação;
- autenticação com usuário inválido;
- registro de usuário com roles;
- login válido com geração de token JWT.

Para executar os testes:

```bash
dotnet test
```

Atualmente existem 16 testes automatizados cobrindo os fluxos principais da solução.

---

## Trade-offs

A solução prioriza clareza arquitetural, resiliência e simplicidade.

Algumas decisões, como Kubernetes, cache distribuído e observabilidade avançada, foram consideradas como evoluções futuras, mas não foram implementadas para evitar fugir do escopo do desafio.

---

## Considerações finais

A solução demonstra uma arquitetura distribuída, resiliente e orientada a eventos, com foco em disponibilidade do serviço de lançamentos, processamento assíncrono da consolidação diária, segurança via JWT e controle granular de autorização.
