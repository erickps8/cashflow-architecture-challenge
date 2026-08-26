import {useEffect,useMemo,useState} from 'react';
import {ArrowDownCircle,ArrowUpCircle,ChevronLeft,ChevronRight,CreditCard,Equal,Minus,WalletCards} from 'lucide-react';
import * as api from './api';
import './monthly.css';

const money=(v:number)=>v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
const monthLabel=(year:number,month:number)=>new Date(year,month-1,1).toLocaleDateString('pt-BR',{month:'long',year:'numeric'});

function moveMonth(year:number,month:number,delta:number){const d=new Date(year,month-1+delta,1);return{year:d.getFullYear(),month:d.getMonth()+1}}

export default function MonthlyBalance({year,month,onPeriodChange}:{year:number;month:number;onPeriodChange:(year:number,month:number)=>void}){
  const[balance,setBalance]=useState<api.MonthlyBalance|null>(null);
  const[accounts,setAccounts]=useState<api.Account[]>([]);
  const[entries,setEntries]=useState<api.Entry[]>([]);
  const[loading,setLoading]=useState(true);

  useEffect(()=>{
    let active=true;setLoading(true);
    api.getAccounts().then(async acc=>{
      const activeAccounts=acc.filter(x=>x.isActive);
      const base=activeAccounts.reduce((s,x)=>s+x.initialBalance,0);
      const projection=await api.getProjection(year,1,month,base);
      const monthsEntries=await Promise.all(Array.from({length:month},(_,i)=>api.getEntries(year,i+1).catch(()=>[])));
      if(!active)return;
      setAccounts(activeAccounts);setBalance(projection.months.at(-1)??null);setEntries(monthsEntries.flat());
    }).finally(()=>active&&setLoading(false));
    return()=>{active=false};
  },[year,month]);

  const accountBalances=useMemo(()=>accounts.map(account=>{
    const movement=entries.filter(x=>x.accountId===account.id).reduce((sum,x)=>sum+(x.type===1?x.amount:-x.amount),0);
    return{...account,currentBalance:account.initialBalance+movement};
  }),[accounts,entries]);
  const totalAccounts=accountBalances.reduce((s,x)=>s+x.currentBalance,0);
  const prev=()=>{const p=moveMonth(year,month,-1);onPeriodChange(p.year,p.month)};
  const next=()=>{const p=moveMonth(year,month,1);onPeriodChange(p.year,p.month)};

  if(loading||!balance)return <div className="panel skeleton-panel"><div className="skeleton"/><div className="skeleton short"/></div>;

  return <section className="monthly-balance">
    <div className="period-nav" aria-label="Selecionar mês"><button type="button" onClick={prev} aria-label="Mês anterior"><ChevronLeft/></button><div><span>Balanço mensal</span><strong>{monthLabel(year,month)}</strong></div><button type="button" onClick={next} aria-label="Próximo mês"><ChevronRight/></button></div>

    <section className="account-overview account-overview-clear">
      <article className="account-total"><div><span>TOTAL NAS CONTAS</span><strong className={totalAccounts<0?'negative-text':''}>{money(totalAccounts)}</strong><small>Quanto você tem somando todas as contas cadastradas</small></div><WalletCards size={24}/></article>
      <div className="account-strip">{accountBalances.length?accountBalances.map(x=><article key={x.id}><span>{x.name}</span><strong className={x.currentBalance<0?'negative-text':''}>{money(x.currentBalance)}</strong><small>Saldo desta conta</small></article>):<article><span>Contas</span><strong>—</strong><small>Nenhuma conta ativa cadastrada</small></article>}</div>
    </section>

    <section className="monthly-equation" aria-label="Resultado financeiro do mês">
      <div className="equation-value income"><span>Entrou</span><strong>{money(balance.totalIncomeAmount)}</strong></div>
      <Minus className="equation-symbol"/>
      <div className="equation-value expense"><span>Saiu</span><strong>{money(balance.totalExpenseAmount)}</strong></div>
      <Equal className="equation-symbol"/>
      <div className={`equation-value result ${balance.netAmount<0?'negative':'positive'}`}><span>{balance.netAmount<0?'Faltou':'Sobrou'}</span><strong>{money(Math.abs(balance.netAmount))}</strong></div>
    </section>

    <section className="monthly-main-cards">
      <article><span><ArrowUpCircle size={17}/>Entradas</span><strong className="positive-text">{money(balance.totalIncomeAmount)}</strong><small>Tudo que entrou neste mês</small></article>
      <article><span><ArrowDownCircle size={17}/>Saídas</span><strong className="negative-text">{money(balance.totalExpenseAmount)}</strong><small>Tudo que saiu neste mês</small></article>
      <article><span><CreditCard size={17}/>Cartões</span><strong>{money(balance.creditCardAmount)}</strong><small>Parte das saídas em faturas/parcelas</small></article>
      <article className="monthly-result"><span>Resultado do mês</span><strong className={balance.netAmount<0?'negative-text':'positive-text'}>{money(balance.netAmount)}</strong><small>Entradas menos saídas</small></article>
    </section>

    <section className="monthly-detail-grid">
      <article className="panel monthly-flow-card"><div className="panel-title"><div><span className="section-kicker">FECHAMENTO</span><h2>Do início ao fim do mês</h2></div></div><div className="monthly-flow"><div><span>Você começou com</span><strong>{money(balance.openingBalance)}</strong></div><div><span>Resultado do mês</span><strong className={balance.netAmount<0?'negative-text':'positive-text'}>{money(balance.netAmount)}</strong></div><div className="closing"><span>Você termina com</span><strong className={balance.closingBalance<0?'negative-text':'positive-text'}>{money(balance.closingBalance)}</strong></div></div></article>
      <article className="panel"><div className="panel-title"><div><span className="section-kicker">DETALHAMENTO</span><h2>Para onde foi o dinheiro</h2></div></div><div className="breakdown"><div><span>Despesas diretas</span><strong>{money(balance.directExpenseAmount)}</strong></div><div><span>Recorrências</span><strong>{money(balance.recurringExpenseAmount)}</strong></div><div><span>Cartões e parcelas</span><strong>{money(balance.creditCardAmount)}</strong></div></div></article>
    </section>
  </section>
}
