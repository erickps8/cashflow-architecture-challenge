import { useEffect, useMemo, useState } from 'react';
import {
  ArrowDownCircle,
  ArrowUpCircle,
  ChevronRight,
  CircleDollarSign,
  CreditCard,
  TrendingDown,
  TrendingUp,
} from 'lucide-react';
import {
  Area,
  AreaChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import * as api from './api';

const money = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

const compact = (value: number) =>
  new Intl.NumberFormat('pt-BR', { notation: 'compact', maximumFractionDigits: 1 }).format(value);

const monthName = (month: number) =>
  new Date(2020, month - 1, 1).toLocaleDateString('pt-BR', { month: 'short' }).replace('.', '');

const expenseColors = ['#2563eb', '#7c3aed', '#f59e0b'];

type MetricKind = 'income' | 'expense' | 'card' | 'balance';

type MetricProps = {
  title: string;
  value: number;
  kind: MetricKind;
  detail: string;
};

function Metric({ title, value, kind, detail }: MetricProps) {
  const Icon = kind === 'income'
    ? ArrowUpCircle
    : kind === 'expense'
      ? ArrowDownCircle
      : kind === 'card'
        ? CreditCard
        : CircleDollarSign;

  return (
    <article className={`metric metric-${kind}`}>
      <div className="metric-head">
        <span>{title}</span>
        <span className="metric-icon"><Icon size={18} /></span>
      </div>
      <strong>{money(value)}</strong>
      <small>{detail}</small>
    </article>
  );
}

export default function DashboardPage({ year, month }: { year: number; month: number }) {
  const [balance, setBalance] = useState<api.MonthlyBalance | null>(null);
  const [projection, setProjection] = useState<api.Projection | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    setLoading(true);

    api.getAccounts()
      .then(async (accounts) => {
        const initialBalance = accounts
          .filter((account) => account.isActive)
          .reduce((sum, account) => sum + account.initialBalance, 0);
        const untilCurrentMonth = await api.getProjection(year, 1, month, initialBalance);
        const current = untilCurrentMonth.months.at(-1);
        if (!current) return;

        const future = await api.getProjection(year, month, 6, current.openingBalance);
        if (active) {
          setBalance(current);
          setProjection(future);
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => { active = false; };
  }, [year, month]);

  const projectionData = useMemo(
    () => projection?.months.map((item) => ({
      name: `${monthName(item.month)}/${String(item.year).slice(2)}`,
      saldo: item.closingBalance,
    })) ?? [],
    [projection],
  );

  const expenseData = useMemo(
    () => balance
      ? [
          { name: 'Diretas', value: balance.directExpenseAmount },
          { name: 'Recorrentes', value: balance.recurringExpenseAmount },
          { name: 'Cartão', value: balance.creditCardAmount },
        ].filter((item) => item.value > 0)
      : [],
    [balance],
  );

  if (loading || !balance) {
    return <div className="panel skeleton-panel"><div className="skeleton" /></div>;
  }

  return (
    <>
      <section className="metrics">
        <Metric title="Receitas" value={balance.totalIncomeAmount} kind="income" detail="Entradas do mês" />
        <Metric title="Despesas" value={balance.totalExpenseAmount} kind="expense" detail="Saídas do mês" />
        <Metric title="Cartão" value={balance.creditCardAmount} kind="card" detail="Faturas e parcelas" />
        <Metric title="Saldo previsto" value={balance.closingBalance} kind="balance" detail="Fechamento do mês" />
      </section>

      <section className="dashboard-grid">
        <article className="panel chart-panel wide">
          <div className="panel-title">
            <div><span className="section-kicker">PRÓXIMOS MESES</span><h2>Como seu saldo tende a evoluir</h2></div>
            {projection?.hasNegativeMonth
              ? <span className="status bad"><TrendingDown size={15} />Risco de negativo</span>
              : <span className="status good"><TrendingUp size={15} />Sem negativo previsto</span>}
          </div>
          <div className="chart-wrap">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={projectionData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="name" />
                <YAxis tickFormatter={compact} />
                <Tooltip formatter={(value) => money(Number(value ?? 0))} />
                <Area type="monotone" dataKey="saldo" stroke="var(--accent)" strokeWidth={3} fill="var(--accent-soft)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </article>

        <article className="panel">
          <div className="panel-title"><div><span className="section-kicker">DESPESAS</span><h2>Onde o dinheiro saiu</h2></div></div>
          <div className="donut-layout">
            <div className="donut">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie data={expenseData} dataKey="value" nameKey="name" innerRadius="62%" outerRadius="88%" paddingAngle={2}>
                    {expenseData.map((item, index) => <Cell key={item.name} fill={expenseColors[index % expenseColors.length]} />)}
                  </Pie>
                  <Tooltip formatter={(value) => money(Number(value ?? 0))} />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <div className="legend-list">
              {expenseData.map((item, index) => (
                <div key={item.name}>
                  <span className="legend-dot" style={{ background: expenseColors[index % expenseColors.length] }} />
                  <span>{item.name}</span>
                  <strong>{money(item.value)}</strong>
                </div>
              ))}
            </div>
          </div>
        </article>

        <article className="panel">
          <div className="panel-title"><div><span className="section-kicker">FECHAMENTO</span><h2>Resultado do mês</h2></div></div>
          <div className="breakdown">
            <div><span>Entradas</span><strong className="positive-text">{money(balance.totalIncomeAmount)}</strong></div>
            <div><span>Saídas</span><strong className="negative-text">{money(balance.totalExpenseAmount)}</strong></div>
            <div className="total-row"><span>Sobra / déficit</span><strong className={balance.netAmount < 0 ? 'negative-text' : 'positive-text'}>{money(balance.netAmount)}</strong></div>
          </div>
        </article>

        <article className="panel">
          <div className="panel-title"><div><span className="section-kicker">PREVISÃO</span><h2>Próximos fechamentos</h2></div></div>
          <div className="table-list compact-list">
            {projection?.months.map((item) => (
              <div key={`${item.year}-${item.month}`}>
                <div><span>{monthName(item.month)}/{String(item.year).slice(2)}</span><small>{item.isNegative ? 'Saldo negativo' : 'Saldo previsto'}</small></div>
                <strong className={item.closingBalance < 0 ? 'negative-text' : ''}>{money(item.closingBalance)}</strong>
                <ChevronRight size={16} />
              </div>
            ))}
          </div>
        </article>
      </section>
    </>
  );
}
