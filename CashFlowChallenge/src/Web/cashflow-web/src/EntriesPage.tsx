import { FormEvent, useEffect, useMemo, useState } from 'react';
import {
  ArrowDownCircle,
  ArrowUpCircle,
  BriefcaseBusiness,
  Bus,
  Car,
  CircleDollarSign,
  GraduationCap,
  HeartPulse,
  Home,
  Landmark,
  Plane,
  Plus,
  Repeat2,
  Search,
  ShoppingCart,
  Sparkles,
  Trash2,
  Utensils,
  WalletCards,
  X,
  type LucideIcon,
} from 'lucide-react';
import * as api from './api';
import RecurringPage from './RecurringPage';
import './entries.css';

const money = (value: number) => value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
const isoDate = (year: number, month: number) => `${year}-${String(month).padStart(2, '0')}-01`;
const normalize = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();

type CategoryVisual = { icon: LucideIcon; tone: string };
const categoryVisual = (name?: string): CategoryVisual => {
  const value = normalize(name ?? '');
  if (/mercado|supermercado|alimentacao|compras/.test(value)) return { icon: ShoppingCart, tone: '#16a34a' };
  if (/restaurante|lanche|delivery/.test(value)) return { icon: Utensils, tone: '#ea580c' };
  if (/escola|educacao|curso|material escolar/.test(value)) return { icon: GraduationCap, tone: '#7c3aed' };
  if (/saude|farmacia|medic|dent/.test(value)) return { icon: HeartPulse, tone: '#e11d48' };
  if (/combustivel|carro|veiculo|manutencao/.test(value)) return { icon: Car, tone: '#2563eb' };
  if (/transporte|uber|taxi|onibus/.test(value)) return { icon: Bus, tone: '#0891b2' };
  if (/moradia|aluguel|condominio|energia|agua|internet/.test(value)) return { icon: Home, tone: '#9333ea' };
  if (/viagem|ferias/.test(value)) return { icon: Plane, tone: '#0284c7' };
  if (/salario|trabalho|hora extra|13/.test(value)) return { icon: BriefcaseBusiness, tone: '#059669' };
  if (/investimento|rendimento|dividendo/.test(value)) return { icon: Landmark, tone: '#0f766e' };
  if (/cartao|fatura/.test(value)) return { icon: WalletCards, tone: '#4f46e5' };
  return { icon: CircleDollarSign, tone: '#64748b' };
};

const categoryHints: Array<{ terms: RegExp; categories: RegExp }> = [
  { terms: /mercado|supermercado|atacadao|carrefour|assai|extra|pao de acucar/, categories: /mercado|supermercado|alimentacao/ },
  { terms: /posto|gasolina|etanol|combustivel|shell|petrobras|ipiranga/, categories: /combustivel/ },
  { terms: /escola|colegio|mensalidade escolar|material escolar/, categories: /escola|educacao|material escolar/ },
  { terms: /farmacia|drogaria|medico|hospital|consulta|dentista/, categories: /saude|farmacia/ },
  { terms: /uber|99|taxi|onibus|metro/, categories: /transporte/ },
  { terms: /ifood|restaurante|lanchonete|pizza|hamburguer/, categories: /restaurante|alimentacao|lazer/ },
  { terms: /aluguel|condominio|energia|luz|caesb|agua|internet/, categories: /moradia|aluguel|condominio|energia|agua|internet/ },
  { terms: /salario|pagamento|vencimento/, categories: /salario/ },
  { terms: /ferias/, categories: /ferias/ },
  { terms: /13|decimo terceiro/, categories: /13|decimo terceiro/ },
  { terms: /viagem|hotel|passagem|airbnb/, categories: /viagem/ },
];

type CreateRequest = { id: number; action: 'expense' | 'income' | 'recurring' | 'purchase' } | null;
type EntryForm = { amount: number; type: number; description: string; accountId: string; categoryId: string; date: string };
type EntriesPageProps = { year: number; month: number; createRequest?: CreateRequest };
type EntriesView = 'entries' | 'recurring';

export default function EntriesPage({ year, month, createRequest }: EntriesPageProps) {
  const [view, setView] = useState<EntriesView>('entries');
  const [list, setList] = useState<api.Entry[]>([]);
  const [accounts, setAccounts] = useState<api.Account[]>([]);
  const [categories, setCategories] = useState<api.Category[]>([]);
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<api.Entry | null>(null);
  const blankForm = (): EntryForm => ({ amount: 0, type: 2, description: '', accountId: '', categoryId: '', date: isoDate(year, month) });
  const [form, setForm] = useState<EntryForm>(blankForm());

  const load = async () => {
    const [entries, loadedAccounts, loadedCategories] = await Promise.all([api.getEntries(year, month), api.getAccounts(), api.getCategories()]);
    setList(entries); setAccounts(loadedAccounts); setCategories(loadedCategories);
  };
  useEffect(() => { void load(); }, [year, month]);
  useEffect(() => {
    if (createRequest?.action === 'recurring') { setView('recurring'); return; }
    if (createRequest?.action === 'expense' || createRequest?.action === 'income') { setView('entries'); openNew(createRequest.action === 'income' ? 1 : 2); }
  }, [createRequest?.id]);

  const totals = useMemo(() => ({
    income: list.filter((item) => item.type === 1).reduce((sum, item) => sum + item.amount, 0),
    expense: list.filter((item) => item.type === 2).reduce((sum, item) => sum + item.amount, 0),
  }), [list]);
  const filtered = useMemo(() => { const term = search.toLowerCase(); return list.filter((item) => item.description.toLowerCase().includes(term)); }, [list, search]);
  const categoryUsage = useMemo(() => list.reduce<Record<string, number>>((usage, item) => { if (item.categoryId) usage[item.categoryId] = (usage[item.categoryId] ?? 0) + 1; return usage; }, {}), [list]);
  const sortedCategories = useMemo(() => categories.filter((item) => item.type === form.type && item.isActive).sort((a, b) => (categoryUsage[b.id] ?? 0) - (categoryUsage[a.id] ?? 0) || a.name.localeCompare(b.name, 'pt-BR')), [categories, form.type, categoryUsage]);
  const suggestedCategory = useMemo(() => {
    if (!form.description.trim()) return undefined;
    const description = normalize(form.description);
    const hint = categoryHints.find((item) => item.terms.test(description));
    if (!hint) return undefined;
    return sortedCategories.find((item) => hint.categories.test(normalize(item.name)));
  }, [form.description, sortedCategories]);

  function openNew(type = 2) { setEditing(null); setForm({ ...blankForm(), type }); setOpen(true); }
  function openItem(entry: api.Entry) { setEditing(entry); setForm({ amount: entry.amount, type: entry.type, description: entry.description, accountId: entry.accountId || '', categoryId: entry.categoryId || '', date: entry.occurredAt.slice(0, 10) }); setOpen(true); }
  async function save(event: FormEvent) {
    event.preventDefault();
    const body = { amount: form.amount, type: form.type, description: form.description, accountId: form.accountId || null, categoryId: form.categoryId || null, occurredAt: new Date(`${form.date}T12:00:00`).toISOString(), isRecurring: false };
    if (editing) await api.updateEntry(editing.id, body); else await api.createEntry(body);
    setOpen(false); await load();
  }
  async function remove() { if (!editing || !confirm(`Excluir o lançamento “${editing.description}”?`)) return; await api.deleteEntry(editing.id); setOpen(false); await load(); }
  const accountName = (id?: string) => accounts.find((item) => item.id === id)?.name;
  const categoryName = (id?: string) => categories.find((item) => item.id === id)?.name;

  return <section className="entries-page">
    <div className="entries-view-tabs" role="tablist" aria-label="Tipo de lançamento">
      <button type="button" role="tab" aria-selected={view === 'entries'} className={view === 'entries' ? 'active' : ''} onClick={() => setView('entries')}><ArrowDownCircle size={17}/>Lançamentos</button>
      <button type="button" role="tab" aria-selected={view === 'recurring'} className={view === 'recurring' ? 'active' : ''} onClick={() => setView('recurring')}><Repeat2 size={17}/>Recorrências</button>
    </div>
    {view === 'recurring' ? <RecurringPage createRequest={createRequest}/> : <>
      <div className="entries-summary"><div><span><ArrowUpCircle/>Entrou</span><strong className="positive-text">{money(totals.income)}</strong></div><div><span><ArrowDownCircle/>Saiu</span><strong className="negative-text">{money(totals.expense)}</strong></div><div className="entries-result"><span>Resultado</span><strong>{money(totals.income - totals.expense)}</strong></div></div>
      <article className="panel entries-history">
        <div className="entries-toolbar"><div><span className="section-kicker">HISTÓRICO</span><h2>Movimentações do mês</h2><p>Toque em um lançamento para ver ou editar</p></div><button className="desktop-add-button" onClick={() => openNew()}><Plus size={18}/>Adicionar</button></div>
        <div className="entries-search"><Search size={17}/><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar lançamento"/></div>
        <div className="entries-list">{filtered.map((entry) => {
          const name = categoryName(entry.categoryId); const visual = categoryVisual(name); const Icon = visual.icon;
          return <button type="button" className="entry-row" key={entry.id} onClick={() => openItem(entry)}>
            <div className="entry-icon" style={{ backgroundColor: `${visual.tone}18`, color: visual.tone }}><Icon/></div>
            <div className="entry-copy"><strong>{entry.description}</strong><span>{new Date(entry.occurredAt).toLocaleDateString('pt-BR')}{name ? ` · ${name}` : ''}{accountName(entry.accountId) ? ` · ${accountName(entry.accountId)}` : ''}</span></div>
            <strong className={entry.type === 2 ? 'negative-text' : 'positive-text'}>{entry.type === 2 ? '- ' : '+ '}{money(entry.amount)}</strong>
          </button>;
        })}</div>
      </article>
    </>}
    {open && <div className="entry-modal-backdrop"><section className="entry-modal">
      <div className="entry-modal-head"><div><span className="section-kicker">{editing ? 'DETALHES DO LANÇAMENTO' : 'NOVO LANÇAMENTO'}</span><h2>{editing ? 'Editar movimentação' : 'Adicionar movimentação'}</h2></div><button className="entry-close" onClick={() => setOpen(false)}><X/></button></div>
      <form onSubmit={save}>
        <div className="entry-type-toggle"><button type="button" className={form.type === 2 ? 'active expense' : ''} onClick={() => setForm({ ...form, type: 2, categoryId: '' })}>Despesa</button><button type="button" className={form.type === 1 ? 'active income' : ''} onClick={() => setForm({ ...form, type: 1, categoryId: '' })}>Receita</button></div>
        <label className="entry-amount">Valor<div><span>R$</span><input type="number" step="0.01" value={form.amount || ''} onChange={(event) => setForm({ ...form, amount: +event.target.value })} required/></div></label>
        <label>Descrição<input value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} required/></label>
        {suggestedCategory && suggestedCategory.id !== form.categoryId && <button type="button" className="category-suggestion" onClick={() => setForm({ ...form, categoryId: suggestedCategory.id })}><Sparkles size={15}/>Sugestão: <strong>{suggestedCategory.name}</strong></button>}
        <div className="entry-form-grid">
          <label>Data<input type="date" value={form.date} onChange={(event) => setForm({ ...form, date: event.target.value })}/></label>
          <label>Conta<select value={form.accountId} onChange={(event) => setForm({ ...form, accountId: event.target.value })}><option value="">Sem conta</option>{accounts.filter((item) => item.isActive).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
          <label>Categoria<select value={form.categoryId} onChange={(event) => setForm({ ...form, categoryId: event.target.value })}><option value="">Sem categoria</option>{sortedCategories.map((item) => <option key={item.id} value={item.id}>{item.name}{categoryUsage[item.id] ? ' · frequente' : ''}</option>)}</select></label>
        </div>
        <div className="entry-modal-actions">{editing && <button type="button" className="danger-button" onClick={remove}><Trash2 size={16}/>Excluir item</button>}<div className="entry-modal-actions-right"><button type="button" className="entry-cancel" onClick={() => setOpen(false)}>Cancelar</button><button className="primary-button">Salvar</button></div></div>
      </form>
    </section></div>}
  </section>;
}
