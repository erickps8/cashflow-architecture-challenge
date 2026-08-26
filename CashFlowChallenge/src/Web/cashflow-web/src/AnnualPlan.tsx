import {useEffect,useMemo,useState} from 'react';
import * as api from './api';

const months=['Jan','Fev','Mar','Abr','Mai','Jun','Jul','Ago','Set','Out','Nov','Dez'];
const money=(v:number)=>v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
const n=(v:string)=>Number(v)||0;

type Row={label:string;values:number[];kind?:'section'|'total'|'result'|'balance'|'control';total?:number};

export default function AnnualPlan(){
  const currentYear=new Date().getFullYear();
  const[year,setYear]=useState(currentYear+1);
  const[opening,setOpening]=useState(()=>n(localStorage.getItem('cashflow_annual_opening')||'10000'));
  const[reserve,setReserve]=useState(()=>n(localStorage.getItem('cashflow_annual_reserve')||'10000'));
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

  const rows=useMemo<Row[]>(()=>{
    if(!projection)return[];
    const m=projection.months;
    const directIncome=m.map(x=>x.incomeAmount);
    const recurringIncome=m.map(x=>x.recurringIncomeAmount);
    const totalIncome=m.map(x=>x.totalIncomeAmount);
    const directExpense=m.map(x=>x.directExpenseAmount);
    const recurringExpense=m.map(x=>x.recurringExpenseAmount);
    const card=m.map(x=>x.creditCardAmount);
    const totalExpense=m.map(x=>x.totalExpenseAmount);
    const result=m.map(x=>x.netAmount);
    const balance=m.map(x=>x.closingBalance);
    const planned=budgets.map(x=>x?.plannedAmount??0);
    const excess=balance.map(x=>Math.max(0,x-reserve));
    const sum=(a:number[])=>a.reduce((s,x)=>s+x,0);
    return[
      {label:'RECEITAS',values:Array(12).fill(0),kind:'section'},
      {label:'Receitas diretas / extras',values:directIncome,total:sum(directIncome)},
      {label:'Receitas recorrentes',values:recurringIncome,total:sum(recurringIncome)},
      {label:'Total receitas',values:totalIncome,total:sum(totalIncome),kind:'total'},
      {label:'DESPESAS',values:Array(12).fill(0),kind:'section'},
      {label:'Despesas diretas',values:directExpense,total:sum(directExpense)},
      {label:'Despesas recorrentes',values:recurringExpense,total:sum(recurringExpense)},
      {label:'Cartões / parcelas',values:card,total:sum(card)},
      {label:'Total despesas',values:totalExpense,total:sum(totalExpense),kind:'total'},
      {label:'ORÇAMENTO PLANEJADO',values:Array(12).fill(0),kind:'section'},
      {label:'Teto planejado por categorias',values:planned,total:sum(planned),kind:'control'},
      {label:'RESULTADO',values:Array(12).fill(0),kind:'section'},
      {label:'Sobra / (déficit) mensal',values:result,total:sum(result),kind:'result'},
      {label:'Saldo acumulado',values:balance,total:balance.at(-1)??opening,kind:'balance'},
      {label:'METAS / CONTROLE',values:Array(12).fill(0),kind:'section'},
      {label:'Reserva mínima protegida',values:Array(12).fill(reserve),total:reserve,kind:'control'},
      {label:'Excedente acima da reserva',values:excess,total:excess.at(-1)??0,kind:'control'}
    ];
  },[projection,budgets,reserve,opening]);

  return <section className="annual-plan">
    <div className="annual-toolbar">
      <div><span className="section-kicker">VISÃO ANUAL</span><h2>Planejamento orçamentário {year}</h2><p>Baseado em lançamentos, recorrências, cartões e orçamento mensal já cadastrados.</p></div>
      <div className="annual-controls">
        <label>Ano<input type="number" value={year} onChange={e=>setYear(+e.target.value)}/></label>
        <label>Saldo inicial<input type="number" step="0.01" value={opening} onChange={e=>setOpening(+e.target.value)}/></label>
        <label>Reserva mínima<input type="number" step="0.01" value={reserve} onChange={e=>setReserve(+e.target.value)}/></label>
        <button className="primary-button" onClick={load} disabled={loading}>{loading?'Calculando...':'Atualizar'}</button>
      </div>
    </div>
    <div className="annual-summary">
      <div><span>Receitas no ano</span><strong>{money(projection?.months.reduce((s,x)=>s+x.totalIncomeAmount,0)??0)}</strong></div>
      <div><span>Despesas no ano</span><strong>{money(projection?.months.reduce((s,x)=>s+x.totalExpenseAmount,0)??0)}</strong></div>
      <div><span>Resultado anual</span><strong>{money(projection?.months.reduce((s,x)=>s+x.netAmount,0)??0)}</strong></div>
      <div><span>Saldo final</span><strong className={(projection?.finalBalance??0)<0?'negative-text':'positive-text'}>{money(projection?.finalBalance??opening)}</strong></div>
    </div>
    <div className="annual-table-wrap">
      <table className="annual-table">
        <thead><tr><th>Categoria</th>{months.map(x=><th key={x}>{x}</th>)}<th>Ano {year}</th></tr></thead>
        <tbody>{rows.map((r,i)=>r.kind==='section'?<tr key={`${r.label}-${i}`} className="annual-section"><td colSpan={14}>{r.label}</td></tr>:<tr key={`${r.label}-${i}`} className={`annual-row ${r.kind??''}`}><td>{r.label}</td>{r.values.map((v,j)=><td key={j} className={v<0?'negative-cell':r.kind==='balance'&&v>=reserve?'positive-cell':''}>{v===0?'—':money(v)}</td>)}<td className={(r.total??0)<0?'negative-cell':''}>{r.total===0?'—':money(r.total??0)}</td></tr>)}</tbody>
      </table>
    </div>
    <p className="annual-note">Receitas extraordinárias, como 13º, férias e horas extras, entram aqui quando forem cadastradas como lançamentos futuros ou recorrências no sistema.</p>
  </section>
}
