import { FormEvent, useEffect, useMemo, useState } from 'react';
import {
  ArrowDownCircle,
  ArrowUpCircle,
  Plus,
  Search,
  Trash2,
  X,
} from 'lucide-react';
import * as api from './api';
import './entries.css';

const money = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

const isoDate = (year: number, month: number) =>
  `${year}-${String(month).padStart(2, '0')}-01`;

type CreateRequest = {
  id: number;
  action: 'expense' | 'income' | 'recurring' | 'purchase' | 'budget';
} | null;

type EntryForm = {
  amount: number;
  type: number;
  description: string;
  accountId: string;
  categoryId: string;
  date: string;
};

type EntriesPageProps = {
  year: number;
  month: number;
  createRequest?: CreateRequest;
};

export default function EntriesPage({ year, month, createRequest }: EntriesPageProps) {
  const [list, setList] = useState<api.Entry[]>([]);
  const [accounts, setAccounts] = useState<api.Account[]>([]);
  const [categories, setCategories] = useState<api.Category[]>([]);
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<api.Entry | null>(null);

  const blankForm = (): EntryForm => ({
    amount: 0,
    type: 2,
    description: '',
    accountId: '',
    categoryId: '',
    date: isoDate(year, month),
  });

  const [form, setForm] = useState<EntryForm>(blankForm());

  const load = async () => {
    const [entries, loadedAccounts, loadedCategories] = await Promise.all([
      api.getEntries(year, month),
      api.getAccounts(),
      api.getCategories(),
    ]);
    setList(entries);
    setAccounts(loadedAccounts);
    setCategories(loadedCategories);
  };

  useEffect(() => {
    void load();
  }, [year, month]);

  useEffect(() => {
    if (createRequest?.action === 'expense' || createRequest?.action === 'income') {
      openNew(createRequest.action === 'income' ? 1 : 2);
    }
  }, [createRequest?.id]);

  const totals = useMemo(
    () => ({
      income: list.filter((item) => item.type === 1).reduce((sum, item) => sum + item.amount, 0),
      expense: list.filter((item) => item.type === 2).reduce((sum, item) => sum + item.amount, 0),
    }),
    [list],
  );

  const filtered = useMemo(() => {
    const term = search.toLowerCase();
    return list.filter((item) => item.description.toLowerCase().includes(term));
  }, [list, search]);

  function openNew(type = 2) {
    setEditing(null);
    setForm({ ...blankForm(), type });
    setOpen(true);
  }

  function openItem(entry: api.Entry) {
    setEditing(entry);
    setForm({
      amount: entry.amount,
      type: entry.type,
      description: entry.description,
      accountId: entry.accountId || '',
      categoryId: entry.categoryId || '',
      date: entry.occurredAt.slice(0, 10),
    });
    setOpen(true);
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    const body = {
      amount: form.amount,
      type: form.type,
      description: form.description,
      accountId: form.accountId || null,
      categoryId: form.categoryId || null,
      occurredAt: new Date(`${form.date}T12:00:00`).toISOString(),
      isRecurring: false,
    };

    if (editing) await api.updateEntry(editing.id, body);
    else await api.createEntry(body);

    setOpen(false);
    await load();
  }

  async function remove() {
    if (!editing || !confirm(`Excluir o lançamento “${editing.description}”?`)) return;
    await api.deleteEntry(editing.id);
    setOpen(false);
    await load();
  }

  const accountName = (id?: string) => accounts.find((item) => item.id === id)?.name;
  const categoryName = (id?: string) => categories.find((item) => item.id === id)?.name;

  return (
    <section className="entries-page">
      <div className="entries-summary">
        <div>
          <span><ArrowUpCircle />Entrou</span>
          <strong className="positive-text">{money(totals.income)}</strong>
        </div>
        <div>
          <span><ArrowDownCircle />Saiu</span>
          <strong className="negative-text">{money(totals.expense)}</strong>
        </div>
        <div className="entries-result">
          <span>Resultado</span>
          <strong>{money(totals.income - totals.expense)}</strong>
        </div>
      </div>

      <article className="panel entries-history">
        <div className="entries-toolbar">
          <div>
            <span className="section-kicker">HISTÓRICO</span>
            <h2>Movimentações do mês</h2>
            <p>Toque em um lançamento para ver ou editar</p>
          </div>
          <button className="desktop-add-button" onClick={() => openNew()}>
            <Plus size={18} />Adicionar
          </button>
        </div>

        <div className="entries-search">
          <Search size={17} />
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar lançamento" />
        </div>

        <div className="entries-list">
          {filtered.map((entry) => (
            <button type="button" className="entry-row" key={entry.id} onClick={() => openItem(entry)}>
              <div className={`entry-icon ${entry.type === 1 ? 'income' : 'expense'}`}>
                {entry.type === 1 ? <ArrowUpCircle /> : <ArrowDownCircle />}
              </div>
              <div className="entry-copy">
                <strong>{entry.description}</strong>
                <span>
                  {new Date(entry.occurredAt).toLocaleDateString('pt-BR')}
                  {categoryName(entry.categoryId) ? ` · ${categoryName(entry.categoryId)}` : ''}
                  {accountName(entry.accountId) ? ` · ${accountName(entry.accountId)}` : ''}
                </span>
              </div>
              <strong className={entry.type === 2 ? 'negative-text' : 'positive-text'}>
                {entry.type === 2 ? '- ' : '+ '}{money(entry.amount)}
              </strong>
            </button>
          ))}
        </div>
      </article>

      {open && (
        <div className="entry-modal-backdrop">
          <section className="entry-modal">
            <div className="entry-modal-head">
              <div>
                <span className="section-kicker">{editing ? 'DETALHES DO LANÇAMENTO' : 'NOVO LANÇAMENTO'}</span>
                <h2>{editing ? 'Editar movimentação' : 'Adicionar movimentação'}</h2>
              </div>
              <button className="entry-close" onClick={() => setOpen(false)}><X /></button>
            </div>

            <form onSubmit={save}>
              <div className="entry-type-toggle">
                <button type="button" className={form.type === 2 ? 'active expense' : ''} onClick={() => setForm({ ...form, type: 2, categoryId: '' })}>Despesa</button>
                <button type="button" className={form.type === 1 ? 'active income' : ''} onClick={() => setForm({ ...form, type: 1, categoryId: '' })}>Receita</button>
              </div>

              <label className="entry-amount">
                Valor
                <div>
                  <span>R$</span>
                  <input type="number" step="0.01" value={form.amount || ''} onChange={(event) => setForm({ ...form, amount: +event.target.value })} required />
                </div>
              </label>

              <label>
                Descrição
                <input value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} required />
              </label>

              <div className="entry-form-grid">
                <label>Data<input type="date" value={form.date} onChange={(event) => setForm({ ...form, date: event.target.value })} /></label>
                <label>
                  Conta
                  <select value={form.accountId} onChange={(event) => setForm({ ...form, accountId: event.target.value })}>
                    <option value="">Sem conta</option>
                    {accounts.filter((item) => item.isActive).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
                  </select>
                </label>
                <label>
                  Categoria
                  <select value={form.categoryId} onChange={(event) => setForm({ ...form, categoryId: event.target.value })}>
                    <option value="">Sem categoria</option>
                    {categories.filter((item) => item.type === form.type && item.isActive).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
                  </select>
                </label>
              </div>

              <div className="entry-modal-actions">
                {editing && <button type="button" className="danger-button" onClick={remove}><Trash2 size={16} />Excluir item</button>}
                <div className="entry-modal-actions-right">
                  <button type="button" className="entry-cancel" onClick={() => setOpen(false)}>Cancelar</button>
                  <button className="primary-button">Salvar</button>
                </div>
              </div>
            </form>
          </section>
        </div>
      )}
    </section>
  );
}
