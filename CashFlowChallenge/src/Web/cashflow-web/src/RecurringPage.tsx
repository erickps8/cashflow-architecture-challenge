import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Plus, Repeat2, Trash2, X } from 'lucide-react';
import * as api from './api';
import './forms-modern.css';

const money = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

type CreateRequest = { id: number; action: string } | null;

type RecurringForm = {
  amount: number;
  type: number;
  description: string;
  accountId: string;
  categoryId: string;
  frequency: number;
  startAt: string;
  endAt: string;
  isActive: boolean;
};

const blankForm = (): RecurringForm => ({
  amount: 0,
  type: 2,
  description: '',
  accountId: '',
  categoryId: '',
  frequency: 0,
  startAt: new Date().toISOString().slice(0, 10),
  endAt: '',
  isActive: true,
});

export default function RecurringPage({ createRequest }: { createRequest?: CreateRequest }) {
  const [list, setList] = useState<api.Recurring[]>([]);
  const [accounts, setAccounts] = useState<api.Account[]>([]);
  const [categories, setCategories] = useState<api.Category[]>([]);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<api.Recurring | null>(null);
  const [form, setForm] = useState<RecurringForm>(blankForm());

  const totals = useMemo(() => {
    const active = list.filter((item) => item.isActive);
    const income = active.filter((item) => item.type === 1).reduce((sum, item) => sum + item.amount, 0);
    const expense = active.filter((item) => item.type === 2).reduce((sum, item) => sum + item.amount, 0);
    return { income, expense, net: income - expense };
  }, [list]);

  const load = async () => {
    const [recurring, loadedAccounts, loadedCategories] = await Promise.all([
      api.getRecurring(),
      api.getAccounts(),
      api.getCategories(),
    ]);
    setList(recurring);
    setAccounts(loadedAccounts);
    setCategories(loadedCategories);
  };

  useEffect(() => { void load(); }, []);
  useEffect(() => {
    if (createRequest?.action === 'recurring') openNew();
  }, [createRequest?.id]);

  function openNew() {
    setEditing(null);
    setForm(blankForm());
    setOpen(true);
  }

  function openItem(item: api.Recurring) {
    setEditing(item);
    setForm({
      amount: item.amount,
      type: item.type,
      description: item.description,
      accountId: item.accountId || '',
      categoryId: item.categoryId || '',
      frequency: item.frequency,
      startAt: item.startAt.slice(0, 10),
      endAt: item.endAt?.slice(0, 10) || '',
      isActive: item.isActive,
    });
    setOpen(true);
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    const body = {
      ...form,
      accountId: form.accountId || null,
      categoryId: form.categoryId || null,
      startAt: new Date(`${form.startAt}T12:00:00`).toISOString(),
      endAt: form.endAt ? new Date(`${form.endAt}T12:00:00`).toISOString() : null,
    };

    if (editing) await api.updateRecurring(editing.id, body);
    else await api.createRecurring(body);

    setOpen(false);
    await load();
  }

  async function remove() {
    if (!editing || !confirm(`Excluir a recorrência “${editing.description}”?`)) return;
    await api.deleteRecurring(editing.id);
    setOpen(false);
    await load();
  }

  return (
    <section className="modern-page">
      <div className="recurring-summary">
        <div><span>Receitas recorrentes</span><strong className="positive-text">{money(totals.income)}</strong></div>
        <div><span>Despesas recorrentes</span><strong className="negative-text">{money(totals.expense)}</strong></div>
        <div><span>Saldo recorrente</span><strong className={totals.net < 0 ? 'negative-text' : 'positive-text'}>{money(totals.net)}</strong></div>
      </div>

      <article className="panel modern-list-card">
        <div className="modern-list-head">
          <div>
            <span className="section-kicker">RECORRÊNCIAS</span>
            <h2>Compromissos automáticos</h2>
            <p>Toque em uma recorrência para ver ou editar</p>
          </div>
          <button className="desktop-add-button" onClick={openNew}><Plus size={18} />Adicionar</button>
        </div>

        <div className="modern-list">
          {list.map((item) => {
            const income = item.type === 1;
            return (
              <button type="button" className={`modern-row modern-row-button recurring-row ${income ? 'recurring-income' : 'recurring-expense'}`} key={item.id} onClick={() => openItem(item)}>
                <div className="modern-row-icon"><Repeat2 /></div>
                <div>
                  <strong>{item.description}</strong>
                  <span><b className="recurring-kind">{income ? 'Entrada' : 'Saída'}</b>{item.isActive ? ` · Próxima: ${new Date(item.nextOccurrenceAt).toLocaleDateString('pt-BR')}` : ' · Inativa'}</span>
                </div>
                <strong className="recurring-amount">{income ? '+' : '−'} {money(item.amount)}</strong>
              </button>
            );
          })}
        </div>
      </article>

      {open && (
        <div className="modern-modal-backdrop">
          <section className="modern-modal">
            <div className="modern-modal-head">
              <div>
                <span className="section-kicker">{editing ? 'DETALHES DA RECORRÊNCIA' : 'NOVA RECORRÊNCIA'}</span>
                <h2>{editing ? 'Editar compromisso' : 'Adicionar compromisso'}</h2>
              </div>
              <button onClick={() => setOpen(false)}><X /></button>
            </div>

            <form onSubmit={save} className="modern-form">
              <div className="entry-type-toggle">
                <button type="button" className={form.type === 2 ? 'active expense' : ''} onClick={() => setForm({ ...form, type: 2 })}>Despesa</button>
                <button type="button" className={form.type === 1 ? 'active income' : ''} onClick={() => setForm({ ...form, type: 1 })}>Receita</button>
              </div>

              <label className="entry-amount">Valor<div><span>R$</span><input type="number" step="0.01" value={form.amount || ''} onChange={(event) => setForm({ ...form, amount: +event.target.value })} /></div></label>
              <label>Descrição<input value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></label>

              <div className="modern-grid">
                <label>Conta<select value={form.accountId} onChange={(event) => setForm({ ...form, accountId: event.target.value })}><option value="">Sem conta</option>{accounts.filter((item) => item.isActive).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
                <label>Categoria<select value={form.categoryId} onChange={(event) => setForm({ ...form, categoryId: event.target.value })}><option value="">Sem categoria</option>{categories.filter((item) => item.type === form.type && item.isActive).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
                <label>Começa em<input type="date" value={form.startAt} onChange={(event) => setForm({ ...form, startAt: event.target.value })} /></label>
                <label>Termina em<input type="date" value={form.endAt} onChange={(event) => setForm({ ...form, endAt: event.target.value })} /></label>
                {editing && <label>Status<select value={form.isActive ? '1' : '0'} onChange={(event) => setForm({ ...form, isActive: event.target.value === '1' })}><option value="1">Ativa</option><option value="0">Inativa</option></select></label>}
              </div>

              <div className="modern-actions modern-actions-split">
                {editing && <button type="button" className="danger-button" onClick={remove}><Trash2 size={16} />Excluir item</button>}
                <div className="modern-actions-right">
                  <button type="button" className="modern-cancel" onClick={() => setOpen(false)}>Cancelar</button>
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
