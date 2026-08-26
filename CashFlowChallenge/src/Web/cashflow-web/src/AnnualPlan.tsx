import {useEffect,useMemo,useState} from 'react';
import {AlertTriangle,CalendarDays,ChevronLeft,ChevronRight,PiggyBank,TrendingDown,TrendingUp,WalletCards} from 'lucide-react';
import * as api from './api';
import './annual.css';

const months=['Jan','Fev','Mar','Abr','Mai','Jun','Jul','Ago','Set','Out','Nov','Dez'];
const money=(v:number)=>v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
const n=(v:string)=>Number(v)||0;

export default function AnnualPlan(){
  const currentYear=new Date().getFullYear();
  const[year,setYear]=useState(currentYear+1);
  const[opening,setOpening]=useState(()=>n(localStorage.getItem('cashflow_annual_opening')||'10000'));
  const[reserve,setReserve]=useState(()=>n(localStorage.getItem('cashflow_annual_reserve')||'10000'));
  const[selectedMonth,setSelectedMonth]=useState(0);
  const[projection,setProjection]=useState<api.Projection|null>(null);
  const[budgets,setBudgets]=useState<(api.Budget|null)[]>([]);
  const[loading,setLoading]=useState(false);

  async function load(){
    setLoading(true);
    localStorage.setItem('cashflow_annual_opening',String(opening));
    localStorage.setItem('cashflow_annual_reserve',String(reserve));
    try{
      const[p,b]=await Promise.all([
        api.getProjection(year,1,12,opening),
        Promise.all(Array.from({length:12},(_,i)=>api.getBudget(year,i+1).catch(()=>null)))
      ]);
      setProjection(p);setBudgets(b);
    }finally{setLoading(false)}
  }

  useEffect(()=>{load()},[]);

  const summary=useMemo(()=>{
    const m=projection?.months??[];
    const income=m.reduce((s,x)=>s+x.totalIncomeAmount,0);
    const expense=m.reduce((s,x)=>s+x.totalExpenseAmount,0);
    const result=m.reduce((s,x)=>s+x.netAmount,0);
    const lowest=m.length?m.reduce((a,b)=>a.closingBalance<=b.closingBalance?a:b):null;
    const negative=m.filter(x=>x.closingBalance<0);
    return{income,expense,result,lowest,negative,final:projection?.finalBalance??opening};
  },[projection,opening]);

  const month=projection?.months[selectedMonth];
  const budget=budgets[selectedMonth];
  const planned=budget?.plannedAmount??0;
  const availableAboveReserve=month?month.closingBalance-reserve:0;

  return <section className="annual-plan">
    <div className="annual-hero">
      <div className="annual-heading">
        <span className="section-kicker">PLANEJAMENTO DO ANO</span>
        <h2>Como seu dinheiro deve se comportar em {year}</h2>
        <p>Veja rapidamente quais meses apertam, quanto sobra e onde você precisa se preparar antes.</p>
      </div>
      <div className="annual-settings">
        <label>Ano<input type="number" value={year} onChange={e=>setYear(+e.target.value)}/></label>
        <label>Saldo no início do ano<input type="number" step="0.01" value={opening} onChange={e=>setOpening(+e.target.value)}/></label>
        <label>Reserva que não quero usar<input type="number" step="0.01" value={reserve} onChange={e=>setReserve(+e.target.value)}/></label>
        <button className="primary-button" onClick={load} disabled={loading}>{loading?'Calculando...':'Recalcular ano'}</button>
      </div>
    </div>

    <div className="annual-summary annual-summary-v2">
      <div><span>Entradas previstas</span><strong>{money(summary.income)}</strong><small>No ano inteiro</small></div>
      <div><span>Saídas previstas</span><strong>{money(summary.expense)}</strong><small>Inclui recorrências e cartões</small></div>
      <div><span>Sobra do ano</span><strong className={summary.result<0?'negative-text':'positive-text'}>{money(summary.result)}</strong><small>Receitas menos despesas</small></div>
      <div><span>Saldo no fim do ano</span><strong className={summary.final<0?'negative-text':'positive-text'}>{money(summary.final)}</strong><small>Considerando o saldo inicial</small></div>
    </div>

    <div className={`annual-health ${summary.negative.length?'annual-health-bad':'annual-health-good'}`}>
      <div className="health-icon">{summary.negative.length?<AlertTriangle/>:<TrendingUp/>}</div>
      <div>
        <strong>{summary.negative.length?`${summary.negative.length} mês(es) exigem atenção`:'Nenhum mês termina negativo'}</strong>
        <span>{summary.negative.length?`Primeiro alerta: ${months[(summary.negative[0]?.month??1)-1]} com ${money(summary.negative[0]?.closingBalance??0)}.`:`Sua projeção permanece acima de zero ao longo de ${year}.`}</span>
      </div>
      {summary.lowest&&<div className="health-low"><span>Pior saldo do ano</span><strong>{months[summary.lowest.month-1]} · {money(summary.lowest.closingBalance)}</strong></div>}
    </div>

    <article className="panel annual-timeline-panel">
      <div className="panel-title"><div><span className="section-kicker">MÊS A MÊS</span><h2>Escolha um mês para entender</h2><p>Não precisa ler uma planilha inteira. Toque no mês e veja o que acontece.</p></div></div>
      <div className="annual-month-strip">{projection?.months.map((x,i)=><button key={`${x.year}-${x.month}`} className={`${selectedMonth===i?'active':''} ${x.closingBalance<0?'negative':''}`} onClick={()=>setSelectedMonth(i)}><span>{months[i]}</span><strong>{money(x.closingBalance)}</strong><small>{x.netAmount<0?'Déficit':'Sobra'} {money(Math.abs(x.netAmount))}</small></button>)}</div>
    </article>

    {month&&<section className="annual-detail-grid">
      <article className="panel annual-month-card">
        <div className="month-card-top">
          <button className="month-arrow" aria-label="Mês anterior" onClick={()=>setSelectedMonth(Math.max(0,selectedMonth-1))} disabled={selectedMonth===0}><ChevronLeft/></button>
          <div><span className="section-kicker">DETALHE DO MÊS</span><h2>{months[selectedMonth]} {year}</h2><p>O que entra, o que sai e como o mês termina.</p></div>
          <button className="month-arrow" aria-label="Próximo mês" onClick={()=>setSelectedMonth(Math.min(11,selectedMonth+1))} disabled={selectedMonth===11}><ChevronRight/></button>
        </div>
        <div className="month-flow">
          <div><span>Começa com</span><strong>{money(month.openingBalance)}</strong></div>
          <div className="income"><span>+ Entradas</span><strong>{money(month.totalIncomeAmount)}</strong></div>
          <div className="expense"><span>− Saídas</span><strong>{money(month.totalExpenseAmount)}</strong></div>
          <div className="month-result"><span>Termina com</span><strong className={month.closingBalance<0?'negative-text':'positive-text'}>{money(month.closingBalance)}</strong></div>
        </div>
      </article>

      <article className="panel annual-explain-card">
        <div className="panel-title"><div><span className="section-kicker">DE ONDE VEM</span><h2>Composição do mês</h2></div></div>
        <div className="annual-breakdown">
          <div><span>Receitas normais</span><strong>{money(month.incomeAmount)}</strong></div>
          <div><span>Receitas recorrentes</span><strong>{money(month.recurringIncomeAmount)}</strong></div>
          <div><span>Despesas diretas</span><strong>{money(month.directExpenseAmount)}</strong></div>
          <div><span>Despesas recorrentes</span><strong>{money(month.recurringExpenseAmount)}</strong></div>
          <div><span>Cartões e parcelas</span><strong>{money(month.creditCardAmount)}</strong></div>
        </div>
      </article>

      <article className="panel annual-control-card">
        <div className="panel-title"><div><span className="section-kicker">SEGURANÇA</span><h2>Reserva e orçamento</h2></div><PiggyBank/></div>
        <div className="control-highlight"><span>Acima da reserva ao fim do mês</span><strong className={availableAboveReserve<0?'negative-text':'positive-text'}>{money(availableAboveReserve)}</strong><small>{availableAboveReserve<0?'Sua reserva mínima seria usada neste mês.':'Valor que permanece livre acima da reserva.'}</small></div>
        <div className="annual-breakdown compact"><div><span>Reserva protegida</span><strong>{money(reserve)}</strong></div><div><span>Orçamento planejado</span><strong>{money(planned)}</strong></div></div>
      </article>
    </section>}

    <div className="annual-help">
      <CalendarDays size={18}/><p><strong>Como usar:</strong> cadastre receitas, despesas, recorrências e compras parceladas normalmente. Esta tela só organiza esses dados para mostrar antecipadamente como cada mês do ano tende a terminar.</p>
    </div>
  </section>
}
