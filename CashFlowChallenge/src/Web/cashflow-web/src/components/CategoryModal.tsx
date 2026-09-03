import { FormEvent, useState } from 'react';
import { X } from 'lucide-react';
import * as api from '../api';

type CategoryModalProps = {
  open: boolean;
  onClose: () => void;
};

export default function CategoryModal({ open, onClose }: CategoryModalProps) {
  const [name, setName] = useState('');
  const [type, setType] = useState(2);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  if (!open) {
    return null;
  }

  const close = () => {
    setName('');
    setType(2);
    setError('');
    onClose();
  };

  const save = async (event: FormEvent) => {
    event.preventDefault();
    const categoryName = name.trim();

    if (!categoryName) {
      return;
    }

    setSaving(true);
    setError('');

    try {
      await api.createCategory({ name: categoryName, type });
      close();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Falha ao criar categoria');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="modern-modal-backdrop">
      <section className="modern-modal compact-modal">
        <div className="modern-modal-head">
          <div>
            <span className="section-kicker">ORGANIZAR</span>
            <h2>Nova categoria</h2>
            <p>Crie uma categoria para usar em lançamentos, orçamento e planejamento anual.</p>
          </div>
          <button type="button" onClick={close} aria-label="Fechar">
            <X />
          </button>
        </div>

        <form className="modern-form" onSubmit={save}>
          {error && <div className="error">{error}</div>}

          <label>
            Nome
            <input
              autoFocus
              maxLength={80}
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="Ex.: Carro, Escola, Lazer"
              required
            />
          </label>

          <label>
            Tipo
            <select value={type} onChange={(event) => setType(Number(event.target.value))}>
              <option value={2}>Despesa</option>
              <option value={1}>Receita</option>
            </select>
          </label>

          <div className="modern-actions">
            <button type="button" className="modern-cancel" onClick={close}>
              Cancelar
            </button>
            <button className="primary-button" disabled={saving}>
              {saving ? 'Salvando...' : 'Criar categoria'}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
