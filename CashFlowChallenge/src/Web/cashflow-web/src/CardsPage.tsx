import { FormEvent, useEffect, useState } from 'react';
import { CreditCard, Plus, Settings, ShoppingBag, Trash2, X } from 'lucide-react';
import * as api from './api';
import './forms-modern.css';

const money = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

type CreateRequest = { id: number; action: string } | null;
type ModalType = 'card' | 'buy' | null;

const emptyCard = () => ({ name: '', limit: 0, closingDay: 20, dueDay: 28 });
const emptyPurchase = () => ({
  categoryId: '',
  description: '',
  totalAmount: 0,
  installmentsCount: 1,
  purchaseDate: new Date().toISOString().slice(0, 10),
});

type CardsPageProps = {
  year: number;
  month: number;
  createRequest?: CreateRequest;
};

export default function CardsPage({ year, month, createRequest }: CardsPageProps) {
  const [cards, setCards] = useState<api.Card[]>([]);
  const [categories, setCategories] = useState<api.Category[]>([]);
  const [selected, setSelected] = useState('');
  const [invoice, setInvoice] = useState<api.Invoice | null>(null);
  const [modal, setModal] = useState<ModalType>(null);
  const [editingCard, setEditingCard] = useState<api.Card | null>(null);
  const [editingPurchase, setEditingPurchase] = useState<string | null>(null);
  const [cardForm, setCardForm] = useState(emptyCard());
  const [purchaseForm, setPurchaseForm] = useState(emptyPurchase());

  const load = async () => {
    const [loadedCards, loadedCategories] = await Promise.all([api.getCards(), api.getCategories()]);
    const activeCards = loadedCards.filter((item) => item.isActive);
    setCards(activeCards);
    setCategories(loadedCategories.filter((item) => item.type === 2 && item.isActive));
    if (!activeCards.some((item) => item.id === selected)) setSelected(activeCards[0]?.id ?? '');
  };

  const refreshInvoice = async () => {
    if (selected) setInvoice(await api.getInvoice(selected, year, month));
  };

  useEffect(() => { void load(); }, []);
  useEffect(() => {
    if (selected) api.getInvoice(selected, year, month).then(setInvoice).catch(() => setInvoice(null));
    else setInvoice(null);
  }, [selected, year, month]);
  useEffect(() => {
    if (createRequest?.action === 'purchase') openNewPurchase();
    if (createRequest?.action === 'card') openNewCard();
  }, [createRequest?.id]);

  function openNewCard() {
    setEditingCard(null);
    setCardForm(emptyCard());
    setModal('card');
  }

  function editCard() {
    const card = cards.find((item) => item.id === selected);
    if (!card) return;
    setEditingCard(card);
    setCardForm({ name: card.name, limit: card.limit, closingDay: card.closingDay, dueDay: card.dueDay });
    setModal('card');
  }

  function openNewPurchase() {
    setEditingPurchase(null);
    setPurchaseForm(emptyPurchase());
    setModal('buy');
  }

  function openPurchase(item: api.Invoice['items'][number]) {
    setEditingPurchase(item.purchaseId);
    setPurchaseForm({
      categoryId: item.categoryId ?? '',
      description: item.description,
      totalAmount: item.purchaseTotalAmount,
      installmentsCount: item.installmentsCount,
      purchaseDate: item.purchaseDate.slice(0, 10),
    });
    setModal('buy');
  }

  async function saveCard(event: FormEvent) {
    event.preventDefault();
    if (editingCard) await api.updateCard(editingCard.id, cardForm);
    else await api.createCard(cardForm);
    setModal(null);
    setEditingCard(null);
    await load();
  }

  async function savePurchase(event: FormEvent) {
    event.preventDefault();
    const body = {
      ...purchaseForm,
      categoryId: purchaseForm.categoryId || null,
      creditCardId: selected,
      purchaseDate: new Date(`${purchaseForm.purchaseDate}T12:00:00`).toISOString(),
    };
    if (editingPurchase) await api.updatePurchase(editingPurchase, body);
    else await api.createPurchase(body);
    setModal(null);
    setEditingPurchase(null);
    await refreshInvoice();
  }

  async function removeCard() {
    if (!editingCard || !confirm(`Excluir o cartão “${editingCard.name}”? O histórico será preservado.`)) return;
    await api.deleteCard(editingCard.id);
    setModal(null);
    setEditingCard(null);
    await load();
  }

  async function removePurchase() {
    if (!editingPurchase || !confirm(`Excluir a compra “${purchaseForm.description}” e todas as parcelas ainda não pagas?`)) return;
    await api.deletePurchase(editingPurchase);
    setModal(null);
    setEditingPurchase(null);
    await refreshInvoice();
  }

  const selectedCard = cards.find((item) => item.id === selected);

  return (
    <section className="modern-page">
      <div className="cards-strip">
        {cards.map((card) => (
          <button key={card.id} className={`finance-card ${selected === card.id ? 'selected' : ''}`} onClick={() => setSelected(card.id)}>
            <CreditCard />
            <span>{card.name}</span>
            <strong>Limite {money(card.limit)}</strong>
            <small>Fecha dia {card.closingDay} · vence dia {card.dueDay}</small>
          </button>
        ))}
        <button className="add-finance-card" onClick={openNewCard}><Plus /><span>Novo cartão</span></button>
      </div>

      <article className="panel modern-list-card">
        <div className="modern-list-head">
          <div>
            <span className="section-kicker">FATURA ATUAL</span>
            <h2>{selectedCard?.name ?? 'Cartão'}</h2>
            <p>Total {money(invoice?.totalAmount ?? 0)} · em aberto {money(invoice?.openAmount ?? 0)}</p>
          </div>
          <div className="head-actions">
            {selected && <button className="secondary-button" onClick={editCard}><Settings size={17} />Gerenciar cartão</button>}
            <button className="modern-add" disabled={!selected} onClick={openNewPurchase}><Plus size={18} />Adicionar compra</button>
          </div>
        </div>

        <div className="modern-list">
          {invoice?.items.map((item) => (
            <button type="button" className="modern-row modern-row-button" key={item.installmentId} onClick={() => openPurchase(item)}>
              <div className="modern-row-icon"><ShoppingBag /></div>
              <div><strong>{item.description}</strong><span>Parcela {item.installmentNumber}/{item.installmentsCount}{item.isPaid ? ' · paga' : ''}</span></div>
              <strong>{money(item.amount)}</strong>
            </button>
          ))}
        </div>
      </article>

      {modal && (
        <div className="modern-modal-backdrop">
          <section className="modern-modal">
            <div className="modern-modal-head">
              <div>
                <span className="section-kicker">{modal === 'card' ? (editingCard ? 'GERENCIAR CARTÃO' : 'NOVO CARTÃO') : (editingPurchase ? 'DETALHES DA COMPRA' : 'NOVA COMPRA')}</span>
                <h2>{modal === 'card' ? (editingCard ? 'Editar cartão' : 'Adicionar cartão') : (editingPurchase ? 'Editar compra' : 'Adicionar compra')}</h2>
              </div>
              <button onClick={() => setModal(null)}><X /></button>
            </div>

            {modal === 'card' ? (
              <form className="modern-form" onSubmit={saveCard}>
                <label>Nome do cartão<input value={cardForm.name} onChange={(event) => setCardForm({ ...cardForm, name: event.target.value })} required /></label>
                <label className="entry-amount">Limite<div><span>R$</span><input type="number" step="0.01" value={cardForm.limit || ''} onChange={(event) => setCardForm({ ...cardForm, limit: +event.target.value })} /></div></label>
                <div className="modern-grid">
                  <label>Fechamento<input type="number" min="1" max="28" value={cardForm.closingDay} onChange={(event) => setCardForm({ ...cardForm, closingDay: +event.target.value })} /></label>
                  <label>Vencimento<input type="number" min="1" max="28" value={cardForm.dueDay} onChange={(event) => setCardForm({ ...cardForm, dueDay: +event.target.value })} /></label>
                </div>
                <div className="modern-actions modern-actions-split">
                  {editingCard && <button type="button" className="danger-button" onClick={removeCard}><Trash2 size={16} />Excluir cartão</button>}
                  <div className="modern-actions-right"><button type="button" className="modern-cancel" onClick={() => setModal(null)}>Cancelar</button><button className="primary-button">Salvar</button></div>
                </div>
              </form>
            ) : (
              <form className="modern-form" onSubmit={savePurchase}>
                <label>Descrição<input value={purchaseForm.description} onChange={(event) => setPurchaseForm({ ...purchaseForm, description: event.target.value })} required /></label>
                <label className="entry-amount">Valor total<div><span>R$</span><input type="number" step="0.01" value={purchaseForm.totalAmount || ''} onChange={(event) => setPurchaseForm({ ...purchaseForm, totalAmount: +event.target.value })} required /></div></label>
                <div className="modern-grid">
                  <label>Categoria<select value={purchaseForm.categoryId} onChange={(event) => setPurchaseForm({ ...purchaseForm, categoryId: event.target.value })}><option value="">Sem categoria</option>{categories.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
                  <label>Parcelas<input type="number" min="1" value={purchaseForm.installmentsCount} onChange={(event) => setPurchaseForm({ ...purchaseForm, installmentsCount: +event.target.value })} /></label>
                  <label>Data<input type="date" value={purchaseForm.purchaseDate} onChange={(event) => setPurchaseForm({ ...purchaseForm, purchaseDate: event.target.value })} /></label>
                </div>
                <div className="modern-actions modern-actions-split">
                  {editingPurchase && <button type="button" className="danger-button" onClick={removePurchase}><Trash2 size={16} />Excluir compra</button>}
                  <div className="modern-actions-right"><button type="button" className="modern-cancel" onClick={() => setModal(null)}>Cancelar</button><button className="primary-button">Salvar</button></div>
                </div>
              </form>
            )}
          </section>
        </div>
      )}
    </section>
  );
}
