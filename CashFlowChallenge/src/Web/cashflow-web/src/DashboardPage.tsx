import { useEffect, useMemo, useState } from 'react';
import { AlertTriangle, ArrowDownCircle, ArrowUpCircle, ChevronRight, CircleDollarSign, CreditCard, ShieldCheck, TrendingDown, TrendingUp, Wallet } from 'lucide-react';
import { Area, AreaChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import * as api from './api';

const money = (value: number) => value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
const compact = (value: number) => new Intl.NumberFormat('pt-BR', { notation: 'compact', maximumFractionDigits: 1 }).format(value);
const monthName = (month: number) => new Date(2020, month - 1, 1).toLocaleDateString('pt-BR', { month: 'short' }).replace('.', '');
const categoryColors = ['#2563eb', '#f97316', '#7c3aed', '#16a34a', '#dc2626', '#0891b2', '#db2777', '#d97706'];

type MetricKind = 'income' | 'expense' | 'card' | 'balance';
type MetricProps = { title: string; value: number; kind: MetricKind; detail: string };
type CategoryRow = { name: string; value: number; previous: number; change: number | null };

function Metric({ title, value, kind, detail }: MetricProps) {
  const Icon = kind === 'income' ? ArrowUpCircle : kind === 'expense' ? ArrowDownCircle : kind === 'card' ? CreditCard : CircleDollarSign;
  return <article className={`metric metric-${kind}`}><div className="metric-head"><span>{title}</span><span className="metric-icon"><Icon size={18}/></span></div><strong>{money(value)}</strong><small>{detail}</small></article>;
}

function previousPeriod(year: number, month: number) {
  return month === 1 ? { year: year - 1, month: 12 } : { year, month: month - 1 };
}

export default function DashboardPage({ year, month }: { year: number; month: number }) {
  const [balance, setBalance] = useState<api.MonthlyBalance | null>(null);
  const [previousBalance, setPreviousBalance] = useState<api.MonthlyBalance | null>(null);
  const [projection, setProjection] = useState<api.Projection | null>(null);
  const [entries, setEntries] = useState<api.Entry[]>([]);
  const [previousEntries, setPreviousEntries] = useState<api.Entry[]>([]);
  const [categories, setCategories] = useState<api.Category[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    setLoading(true);
    const previous = previousPeriod(year, month);
    Promise.all([api.getAccounts(), api.getEntries(year, month), api.getEntries(previous.year, previous.month), api.getCategories()])
      .then(async ([accounts, monthEntries, priorEntries, loadedCategories]) => {
        const initialBalance = accounts.filter((account) => account.isActive).reduce((sum, account) => sum + account.initialBalance, 0);
        const untilCurrentMonth = await api.getProjection(year, 1, month, initialBalance);
        const current = untilCurrentMonth.months.at(-1);
        if (!current) return;

        let prior: api.MonthlyBalance | null = null;
        if (month > 1) {
          prior = untilCurrentMonth.months.at(-2) ?? null;
        } else {
          const previousYear = await api.getProjection(year - 1, 1, 12, initialBalance);
          prior = previousYear.months.at(-1) ?? null;
        }

        const future = await api.getProjection(year, month, 6, current.openingBalance);
        if (active) {
          setBalance(current);
          setPreviousBalance(prior);
          setProjection(future);
          setEntries(monthEntries);
          setPreviousEntries(priorEntries);
          setCategories(loadedCategories);
        }
      })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [year, month]);

  const projectionData = useMemo(() => projection?.months.map((item) => ({ name: `${monthName(item.month)}/${String(item.year).slice(2)}`, saldo: item.closingBalance })) ?? [], [projection]);
  const expenseData = useMemo(() => balance ? [{ name: 'Diretas', value: balance.directExpenseAmount }, { name: 'Recorrentes', value: balance.recurringExpenseAmount }, { name: 'Cartão', value: balance.creditCardAmount }].filter((item) => item.value > 0) : [], [balance]);

  const categoryBreakdown = useMemo<CategoryRow[]>(() => {
    const names = new Map(categories.map((category) => [category.id, category.name]));
    const aggregate = (source: api.Entry[]) => {
      const result = new Map<string, number>();
      source.filter((entry) => entry.type === 2).forEach((entry) => {
        const name = entry.categoryId ? names.get(entry.categoryId) ?? 'Sem categoria' : 'Sem categoria';
        result.set(name, (result.get(name) ?? 0) + entry.amount);
      });
      return result;
    };
    const current = aggregate(entries);
    const previous = aggregate(previousEntries);
    return [...current.entries()]
      .map(([name, value]) => {
        const prior = previous.get(name) ?? 0;
        return { name, value, previous: prior, change: prior > 0 ? ((value - prior) / prior) * 100 : null };
      })
      .filter((item) => item.value > 0)
      .sort((a, b) => b.value - a.value)
      .slice(0, 8);
  }, [entries, previousEntries, categories]);

  const expenseChange = previousBalance && previousBalance.totalExpenseAmount > 0
    ? ((balance?.totalExpenseAmount ?? 0) - previousBalance.totalExpenseAmount) / previousBalance.totalExpenseAmount * 100
    : null;
  const monthlyResult = balance?.netAmount ?? 0;
  const firstNegative = projection?.months.find((item) => item.isNegative);
  const topCategory = categoryBreakdown[0];

  const alerts = useMemo(() => {
    const items: Array<{ tone: 'good' | 'bad' | 'warn'; text: string }> = [];
    if (monthlyResult > 0) items.push({ tone: 'good', text: `Após as receitas e despesas conhecidas, o mês tem sobra de ${money(monthlyResult)}.` });
    else if (monthlyResult < 0) items.push({ tone: 'bad', text: `As despesas conhecidas superam as receitas do mês em ${money(Math.abs(monthlyResult))}.` });
    else items.push({ tone: 'warn', text: 'As receitas e despesas conhecidas do mês estão empatadas.' });
    if (balance && balance.closingBalance < 0) items.push({ tone: 'bad', text: `Considerando o saldo de abertura, o mês está projetado para fechar ${money(Math.abs(balance.closingBalance))} no negativo.` });
    if (firstNegative) items.push({ tone: 'bad', text: `Mantendo os compromissos atuais, ${monthName(firstNegative.month)}/${firstNegative.year} fecha negativo.` });
    if (expenseChange !== null && expenseChange >= 15) items.push({ tone: 'warn', text: `Suas despesas totais estão ${expenseChange.toFixed(0)}% maiores que no mês anterior.` });
    if (topCategory && topCategory.change !== null && topCategory.change >= 20) items.push({ tone: 'warn', text: `${topCategory.name} subiu ${topCategory.change.toFixed(0)}% entre os lançamentos diretos em relação ao mês anterior.` });
    return items.slice(0, 4);
  }, [monthlyResult, balance, firstNegative, expenseChange, topCategory]);

  if (loading || !balance) return <div className="panel skeleton-panel"><div className="skeleton" /></div>;

  return <>
    <section className="metrics">
      <Metric title="Receitas" value={balance.totalIncomeAmount} kind="income" detail="Entradas do mês" />
      <Metric title="Despesas" value={balance.totalExpenseAmount} kind="expense" detail="Inclui cartão" />
      <Metric title="Cartão" value={balance.creditCardAmount} kind="card" detail="Parte das despesas" />
      <Metric title="Saldo previsto" value={balance.closingBalance} kind="balance" detail="Saldo acumulado no fechamento" />
    </section>

    <section className="dashboard-grid">
      <article className="panel">
        <div className="panel-title"><div><span className="section-kicker">RESULTADO DO MÊS</span><h2>Receitas menos despesas</h2></div><span className={monthlyResult >= 0 ? 'status good' : 'status bad'}><Wallet size={15} />{monthlyResult >= 0 ? 'Superávit' : 'Déficit'}</span></div>
        <div className="breakdown">
          <div><span>Saldo de abertura</span><strong>{money(balance.openingBalance)}</strong></div>
          <div><span>Resultado do mês</span><strong className={monthlyResult < 0 ? 'negative-text' : 'positive-text'}>{money(monthlyResult)}</strong></div>
          <div className="total-row"><span>Saldo previsto no fechamento</span><strong className={balance.closingBalance < 0 ? 'negative-text' : 'positive-text'}>{money(balance.closingBalance)}</strong></div>
        </div>
      </article>

      <article className="panel">
        <div className="panel-title"><div><span className="section-kicker">ALERTAS</span><h2>O que merece atenção</h2></div><AlertTriangle size={19} /></div>
        <div className="table-list compact-list">{alerts.map((alert, index) => <div key={`${alert.text}-${index}`}><div><span>{alert.text}</span><small>{alert.tone === 'good' ? 'Situação saudável' : alert.tone === 'bad' ? 'Ação recomendada' : 'Acompanhe'}</small></div>{alert.tone === 'good' ? <ShieldCheck className="positive-text" size={17} /> : <AlertTriangle className={alert.tone === 'bad' ? 'negative-text' : ''} size={17} />}</div>)}</div>
      </article>

      <article className="panel chart-panel wide">
        <div className="panel-title"><div><span className="section-kicker">PRÓXIMOS MESES</span><h2>Como seu saldo tende a evoluir</h2></div>{projection?.hasNegativeMonth ? <span className="status bad"><TrendingDown size={15} />Risco de negativo</span> : <span className="status good"><TrendingUp size={15} />Sem negativo previsto</span>}</div>
        <div className="chart-wrap"><ResponsiveContainer width="100%" height="100%"><AreaChart data={projectionData}><CartesianGrid strokeDasharray="3 3" vertical={false} /><XAxis dataKey="name" /><YAxis tickFormatter={compact} /><Tooltip formatter={(value) => money(Number(value ?? 0))} /><Area type="monotone" dataKey="saldo" stroke="var(--accent)" strokeWidth={3} fill="var(--accent-soft)" /></AreaChart></ResponsiveContainer></div>
      </article>

      <article className="panel">
        <div className="panel-title"><div><span className="section-kicker">LANÇAMENTOS DIRETOS POR CATEGORIA</span><h2>Onde você lançou despesas</h2></div></div>
        <div className="donut-layout"><div className="donut"><ResponsiveContainer width="100%" height="100%"><PieChart><Pie data={categoryBreakdown} dataKey="value" nameKey="name" innerRadius="62%" outerRadius="88%" paddingAngle={2}>{categoryBreakdown.map((item, index) => <Cell key={item.name} fill={categoryColors[index % categoryColors.length]} />)}</Pie><Tooltip formatter={(value) => money(Number(value ?? 0))} /></PieChart></ResponsiveContainer></div><div className="legend-list">{categoryBreakdown.map((item, index) => <div key={item.name}><span className="legend-dot" style={{ background: categoryColors[index % categoryColors.length] }} /><span>{item.name}{item.change !== null ? ` · ${item.change >= 0 ? '+' : ''}${item.change.toFixed(0)}%` : ''}</span><strong>{money(item.value)}</strong></div>)}</div></div>
      </article>

      <article className="panel">
        <div className="panel-title"><div><span className="section-kicker">TOP LANÇAMENTOS DIRETOS</span><h2>Comparação com o mês anterior</h2></div></div>
        <div className="breakdown">{categoryBreakdown.slice(0, 5).map((item) => <div key={item.name}><span>{item.name}</span><strong className={item.change !== null && item.change > 0 ? 'negative-text' : item.change !== null && item.change < 0 ? 'positive-text' : ''}>{item.change === null ? 'novo gasto' : `${item.change >= 0 ? '+' : ''}${item.change.toFixed(0)}%`} · {money(item.value)}</strong></div>)}</div>
      </article>

      <article className="panel">
        <div className="panel-title"><div><span className="section-kicker">DESPESAS</span><h2>Composição das saídas</h2></div></div>
        <div className="donut-layout"><div className="donut"><ResponsiveContainer width="100%" height="100%"><PieChart><Pie data={expenseData} dataKey="value" nameKey="name" innerRadius="62%" outerRadius="88%" paddingAngle={2}>{expenseData.map((item, index) => <Cell key={item.name} fill={categoryColors[index % categoryColors.length]} />)}</Pie><Tooltip formatter={(value) => money(Number(value ?? 0))} /></PieChart></ResponsiveContainer></div><div className="legend-list">{expenseData.map((item, index) => <div key={item.name}><span className="legend-dot" style={{ background: categoryColors[index % categoryColors.length] }} /><span>{item.name}</span><strong>{money(item.value)}</strong></div>)}</div></div>
      </article>

      <article className="panel">
        <div className="panel-title"><div><span className="section-kicker">FECHAMENTO</span><h2>Resultado do mês</h2></div></div>
        <div className="breakdown"><div><span>Entradas</span><strong className="positive-text">{money(balance.totalIncomeAmount)}</strong></div><div><span>Saídas (cartão incluído)</span><strong className="negative-text">{money(balance.totalExpenseAmount)}</strong></div><div className="total-row"><span>Sobra / déficit do mês</span><strong className={balance.netAmount < 0 ? 'negative-text' : 'positive-text'}>{money(balance.netAmount)}</strong></div></div>
      </article>

      <article className="panel">
        <div className="panel-title"><div><span className="section-kicker">PREVISÃO</span><h2>Próximos fechamentos</h2></div></div>
        <div className="table-list compact-list">{projection?.months.map((item) => <div key={`${item.year}-${item.month}`}><div><span>{monthName(item.month)}/{String(item.year).slice(2)}</span><small>{item.isNegative ? 'Saldo acumulado negativo' : 'Saldo acumulado previsto'}</small></div><strong className={item.closingBalance < 0 ? 'negative-text' : ''}>{money(item.closingBalance)}</strong><ChevronRight size={16} /></div>)}</div>
      </article>
    </section>
  </>;
}
