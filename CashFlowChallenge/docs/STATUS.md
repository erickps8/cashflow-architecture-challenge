# CashFlow — Status do Desenvolvimento

Última atualização: 26/08/2026

## Objetivo

Gestor financeiro pessoal com foco em planejamento, orçamento, cartões, recorrências e projeção de saldo futuro. O objetivo é evoluir para substituir a planilha financeira e grande parte do uso do Mobills.

## Branch atual

`feature/web-design-system-v2`

Fluxo de trabalho: implementar → build → homologar localmente → corrigir → homologar → PR.

## Estado atual

### Backend
- Autenticação JWT.
- Contas e categorias.
- Lançamentos de receitas e despesas.
- Recorrências.
- Cartões, compras parceladas e faturas.
- Orçamento mensal.
- Balanço mensal.
- Projeção financeira entre meses.
- Recorrências futuras entram automaticamente na projeção.
- Parcelas de cartão entram automaticamente na projeção.
- Identificação de mês/saldo negativo homologada.

### Frontend V2
Stack: React + TypeScript + Vite + Recharts + Lucide React + Nginx/Docker.

Telas atuais:
- Login.
- Visão geral / Dashboard.
- Lançamentos.
- Recorrências.
- Cartões / Faturas.
- Orçamento.
- Planejamento Anual.

Direção visual: produto financeiro moderno, compacto, responsivo e adequado para desktop e celular.

## Planejamento Anual

Última funcionalidade implementada e ainda em homologação.

Referência funcional: planilha `PLANEJAMENTO ORÇAMENTÁRIO 2027`, organizada em Jan–Dez + total anual.

A tela atual consolida dados da projeção existente:
- receitas diretas;
- receitas recorrentes;
- despesas diretas;
- despesas recorrentes;
- cartões/parcelamentos;
- total de receitas e despesas;
- sobra/déficit mensal;
- saldo acumulado;
- orçamento planejado;
- reserva mínima;
- excedente acima da reserva.

Próxima evolução esperada: detalhar linhas específicas como salários, 13º, férias/extra, horas extras, escola, material escolar, fundo de aniversários, lazer e demais categorias, sem transformar a funcionalidade em uma planilha estática.

## Próxima tarefa

1. Build da `feature/web-design-system-v2`.
2. Abrir a tela Planejamento Anual.
3. Testar inicialmente ano 2027, saldo inicial R$ 10.000 e reserva mínima R$ 10.000.
4. Comparar valores e UX com a imagem da planilha de referência.
5. Corrigir detalhamento, cálculos e experiência de uso.
6. Homologar a V2 antes de PR/merge.

## Homologação com dados

Existe a branch `homolog/personal-finance-seed`, usada exclusivamente para popular o PostgreSQL local com uma base aproximada para testes.

O seed é idempotente e já foi executado com sucesso. Ele não deve ser levado automaticamente para `develop`, pois contém dados de homologação e alguns valores são estimativas.

## Ambiente local

Diretório usual:
`D:\Projetos\cashflow-architecture-challenge\CashFlowChallenge`

Comandos principais:

```bat
git pull
docker compose up -d --build
docker compose ps
docker logs <container> --tail 100
```

Evitar `docker compose down -v` sem necessidade, pois pode remover os dados locais.

## Observações técnicas

O ambiente utiliza Docker Desktop + WSL2. Problemas anteriores de WSL/Docker foram estabilizados após atualização. Não assumir que novos erros são de infraestrutura sem primeiro analisar os logs.

PostgreSQL pode provocar reinício das APIs durante inicialização/migrations (`Connection refused` / `57P03 database system is starting up`).

## Regra para próximos agentes

Antes de implementar, ler este arquivo e os arquivos relevantes da branch atual. Não assumir que este documento substitui a leitura do código. Atualizar este STATUS ao concluir etapas importantes para preservar continuidade entre conversas/agentes.
