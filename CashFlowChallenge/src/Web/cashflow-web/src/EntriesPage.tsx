import {FormEvent,useEffect,useMemo,useState} from 'react';
import {ArrowDownCircle,ArrowUpCircle,CalendarDays,Plus,Search,X} from 'lucide-react';
import * as api from './api';
import './entries.css';

const money=(v:number)=>v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});
const isoDate=(year:number,month:number)=>`${year}-${String(month).padStart(2,'0')}-01`;

export default function EntriesPage({year,month}:{year:number;month:number}){
  const[list,setList]=useState<api.Entry[]>([]);
  const[accounts,setAccounts]=useState<api.Account[]>([]);
  const[cats,setCats]=useState<api.Category[]>([]);
  const[open,setOpen]=useState(false);
  const[search,setSearch]=useState('');
  const[form,setForm]=useState({amount:0,type:2,description:'',accountId:'',categoryId:'',date:isoDate(year,month)});

  const load=()=>Promise.all([api.getEntries(year,month),api.getAccounts(),api.getCategories()]).then(([a,b,c])=>{setList(a);setAccounts(b);setCats(c)});
  useEffect(()=>{load()},[year,month]);
  useEffect(()=>setForm(x=>({...x,date:isoDate(year,month)})),[year,month]);

  const totals=useMemo(()=>({income:list.filter(x=>x.type===1).reduce((s,x)=>s+x.amount,0),expense:list.filter(x=>x.type===2).reduce((s,x)=>s+x.amount,0)}),[list]);
  const filtered=useMemo(()=>list.filter(x=>x.description.toLowerCase().includes(search.toLowerCase())),[list,search]);

  async function save(e:FormEvent){
    e.preventDefault();
    await api.createEntry({amount:form.amount,type:form.type,description:form.description,accountId:form.accountId||null,categoryId:form.categoryId||null,occurredAt:new Date(`${form.date}T12:00:00`).toISOString(),isRecurring:false});
    setForm({amount:0,type:2,description:'',accountId:'',categoryId:'',date:isoDate(year,month)});
    setOpen(false);await load();
  }

  const accountName=(id?:string)=>accounts.find(x=>x.id===id)?.name;
  const categoryName=(id?:string)=>cats.find(x=>x.id===id)?.name;

  return <section className="entries-page">
    <div className="entries-summary">
      <div><span><ArrowUpCircle/>Entrou</span><strong className="positive-text">{money(totals.income)}</strong></div>
      <div><span><ArrowDownCircle/>Saiu</span><strong className="negative-text">{money(totals.expense)}</strong></div>
      <div className="entries-result"><span>Resultado</span><strong className={totals.income-totals.expense<0?'negative-text':'positive-text'}>{money(totals.income-totals.expense)}</strong></div>
    </div>

    <article className="panel entries-history">
      <div className="entries-toolbar">
        <div><span className="section-kicker">HISTÓRICO</span><h2>Movimentações do mês</h2><p>{list.length} lançamento(s)</p></div>
        <button className="add-entry-button" onClick={()=>setOpen(true)}><Plus size={18}/><span>Adicionar</span></button>
      </div>
      <div className="entries-search"><Search size={17}/><input value={search} onChange={e=>setSearch(e.target.value)} placeholder="Buscar lançamento"/></div>
      <div className="entries-list">{filtered.length?filtered.map(x=><div className="entry-row" key={x.id}><div className={`entry-icon ${x.type===1?'income':'expense'}`}>{x.type===1?<ArrowUpCircle/>:<ArrowDownCircle/>}</div><div className="entry-copy"><strong>{x.description}</strong><span>{new Date(x.occurredAt).toLocaleDateString('pt-BR')}{categoryName(x.categoryId)?` · ${categoryName(x.categoryId)}`:''}{accountName(x.accountId)?` · ${accountName(x.accountId)}`:''}</span></div><strong className={x.type===2?'negative-text':'positive-text'}>{x.type===2?'- ':'+ '}{money(x.amount)}</strong></div>):<div className="entries-empty"><strong>Nenhum lançamento neste mês</strong><span>Use o botão “Adicionar” para registrar a primeira movimentação.</span></div>}</div>
    </article>

    {open&&<div className="entry-modal-backdrop" onMouseDown={e=>{if(e.target===e.currentTarget)setOpen(false)}}>
      <section className="entry-modal" role="dialog" aria-modal="true" aria-label="Novo lançamento">
        <div className="entry-modal-head"><div><span className="section-kicker">NOVO LANÇAMENTO</span><h2>Adicionar movimentação</h2><p>Preencha só o necessário.</p></div><button className="entry-close" onClick={()=>setOpen(false)} aria-label="Fechar"><X/></button></div>
        <form onSubmit={save}>
          <div className="entry-type-toggle"><button type="button" className={form.type===2?'active expense':''} onClick={()=>setForm({...form,type:2,categoryId:''})}><ArrowDownCircle/>Despesa</button><button type="button" className={form.type===1?'active income':''} onClick={()=>setForm({...form,type:1,categoryId:''})}><ArrowUpCircle/>Receita</button></div>
          <label className="entry-amount">Valor <div><span>R$</span><input autoFocus inputMode="decimal" type="number" step="0.01" min="0.01" value={form.amount||''} onChange={e=>setForm({...form,amount:+e.target.value})} placeholder="0,00" required/></div></label>
          <label>Descrição<input value={form.description} onChange={e=>setForm({...form,description:e.target.value})} placeholder="Ex.: Supermercado, salário, escola..." required/></label>
          <div className="entry-form-grid">
            <label>Data<div className="entry-date"><CalendarDays size={17}/><input type="date" value={form.date} onChange={e=>setForm({...form,date:e.target.value})} required/></div></label>
            <label>Conta<select value={form.accountId} onChange={e=>setForm({...form,accountId:e.target.value})}><option value="">Sem conta</option>{accounts.filter(x=>x.isActive).map(x=><option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
            <label>Categoria<select value={form.categoryId} onChange={e=>setForm({...form,categoryId:e.target.value})}><option value="">Sem categoria</option>{cats.filter(x=>x.type===form.type&&x.isActive).map(x=><option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
          </div>
          <div className="entry-modal-actions"><button type="button" className="entry-cancel" onClick={()=>setOpen(false)}>Cancelar</button><button className="primary-button">Salvar lançamento</button></div>
        </form>
      </section>
    </div>}
  </section>
}
