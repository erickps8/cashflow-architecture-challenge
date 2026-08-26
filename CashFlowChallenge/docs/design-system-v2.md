# CashFlow Design System v2

## Objetivo
Criar uma experiência financeira clara, previsível, acessível e responsiva, com visual consistente em desktop e mobile.

## Referências aplicadas
- Atlassian Design System: tokens como fonte única para cor, spacing, tipografia, radius e elevação.
- IBM Carbon: componentes previsíveis, foco em teclado, contraste e semântica.
- WCAG: estados não dependem apenas de cor; foco visível; contraste adequado.
- Dashboards financeiros modernos: hierarquia por KPIs, tendência temporal, composição de despesas e alertas acionáveis.

## Tokens
Os tokens vivem em `src/styles.css` dentro de `:root`.

Categorias:
- superfícies: `--bg`, `--surface`, `--surface-soft`
- texto: `--text`, `--muted`, `--subtle`
- bordas: `--border`, `--border-subtle`
- ação: `--accent`, `--accent-strong`, `--accent-soft`
- estados: `--success`, `--danger`, `--warning`
- radius: `--radius-sm`, `--radius-md`, `--radius-lg`
- sombras: `--shadow-sm`, `--shadow-md`
- espaçamento: `--space-*`

## Navegação
- Desktop: sidebar persistente.
- Mobile: bottom navigation com cinco destinos principais.
- O item selecionado sempre possui estado visual persistente.

## Dashboard
- KPIs: receitas, despesas, cartão e saldo projetado.
- Gráfico temporal: projeção de saldo para seis meses.
- Gráfico de composição: despesas diretas, recorrentes e cartão.
- Alertas: risco de saldo negativo e estado positivo/negativo.

## Componentes
- `metric`: KPI financeiro.
- `panel`: superfície de conteúdo.
- `status`: feedback semântico.
- `form-grid`: formulários responsivos com labels visíveis.
- `table-list`: listas financeiras.
- `progress`: progresso de orçamento.
- `primary-button`, `secondary-button`, `small-button`: hierarquia de ações.

## Responsividade
- 1180px: grids complexos colapsam.
- 900px: sidebar vira bottom navigation.
- 640px: formulários passam para uma coluna e cards reduzem densidade.
- 390px: KPIs viram uma coluna para evitar truncamento.

## Acessibilidade
- foco visível em botões e inputs;
- labels persistentes nos campos;
- navegação por botões semânticos;
- estados com texto/ícone além da cor;
- `prefers-reduced-motion` respeitado;
- targets mobile com aproximadamente 44px ou mais.
