import { FormEvent, useEffect, useState } from 'react';
import { PiggyBank, Plus, X } from 'lucide-react';
import * as api from './api';
import './forms-modern.css';

const money = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

type CreateRequest = { id: number; action: string } | null;

type BudgetPageProps = {
  year: number;
  month: number;
  createRequest?: CreateRequest;
};

export default function BudgetPage({ year, month, createRequest }: BudgetPageProps) {
  const [budget, setBudget] = useState<api.Budget | null>(null);
  const [categories, setCategories] = useState<api.Category[]>([]);
  const [categoryId, setCategoryId] = useState('');
  const [amount, setAmount] = useState(0);
  const [open, setOpen] = useState(false);

  const load = async () => {
    const [loadedBudget, loadedCategories] = await Promise.all([
      api.getBudget(year, month).catch(() => null),
      api.getCategories(),
    ]);
    setBudget(loadedBudget);
    setCategories(loadedCategories.filter((item) => item.type === 2 && item.isActive));
  };

  useEffect(() => { void load(); }, [year, month]);
  useEffect(() => {
    if (createRequest?.action === 'budget') setOpen(true);
  }, [createRequest?.id]);

  async function save(event: FormEvent) {
    event.preventDefault();
    await api.setBudget({ year, month, categoryId, plannedAmount: amount });
    setOpen(false);
    setCategoryId('');
    setAmount(0);
    await load();
  }

  return (
    <section className="modern-page">
      <div className="budget-hero">
        <div><span>Planejado no mês</span><strong>{money(budget?.plannedAmount ?? 0)}</strong><small>Definido no planejamento</small></div>
        <div><span>Realizado</span><strong>{money(budget?.actualAmount ?? 0)}</strong><small>O que já virou gasto</small></div>
        <div><span>Disponível no orçamento</span><strong className={(budget?.remainingAmount ?? 0) < 0 ? 'negative-text' : 'positive-text'}>{money(budget?.remainingAmount ?? 0)}</strong><small>Planejado menos realizado</small></div>
      </div>

      <article className="panel modern-list-card">
        <div className="modern-list-head">
          <div>
            <span className="section-kicker">ACOMPANHAMENTO MENSAL</span>
            <h2>Planejado × realizado por categoria</h2>
            <p>O Planejamento Anual define o plano. Aqui você acompanha o mês e ajusta um limite quando precisar.</p>
          </div>
          <button className="modern-add" onClick={() => setOpen(true)}><Plus size={18} />Ajustar orçamento</button>
        </div>

        <div className="budget-list">
          {budget?.categories.length ? budget.categories.map((item) => {
            const percentage = item.plannedAmount > 0
              ? Math.min(100, (item.actualAmount / item.plannedAmount) * 100)
              : 0;

            return (
              <div className="budget-item" key={item.categoryId}>
                <div>
                  <span>{item.categoryName}</span>
                  <strong>{money(item.actualAmount)} <small>de {money(item.plannedAmount)}</small></strong>
                </div>
                <div className="budget-progress" aria-label={`${percentage.toFixed(0)}% utilizado`}><i style={{ width: `${percentage}%` }} /></div>
                <small>{item.isOverBudget ? `Acima do planejado em ${money(Math.abs(item.remainingAmount))}` : `Disponível: ${money(item.remainingAmount)}`}</small>
              </div>
            );
          }) : (
            <div className="modern-empty">
              <strong>Nenhum orçamento para este mês</strong>
              <span>Defina o plano no Planejamento Anual ou ajuste uma categoria aqui.</span>
            </div>
          )}
        </div>
      </article>

      {open && (
        <div className="modern-modal-backdrop">
          <section className="modern-modal compact-modal">
            <div className="modern-modal-head">
              <div><span className="section-kicker">AJUSTAR MÊS</span><h2>Orçamento da categoria</h2></div>
              <button onClick={() => setOpen(false)}><X /></button>
            </div>
            <form onSubmit={save} className="modern-form">
              <div className="modal-symbol"><PiggyBank /></div>
              <label>
                Categoria
                <select value={categoryId} onChange={(event) => setCategoryId(event.target.value)} required>
                  <option value="">Escolha uma categoria</option>
                  {categories.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
                </select>
              </label>
              <label className="entry-amount">Valor planejado<div><span>R$</span><input type="number" step="0.01" min="0" value={amount || ''} onChange={(event) => setAmount(+event.target.value)} required /></div></label>
              <div className="modern-actions">
                <button type="button" className="modern-cancel" onClick={() => setOpen(false)}>Cancelar</button>
                <button className="primary-button">Salvar orçamento</button>
              </div>
            </form>
          </section>
        </div>
      )}
    </section>
  );
}
