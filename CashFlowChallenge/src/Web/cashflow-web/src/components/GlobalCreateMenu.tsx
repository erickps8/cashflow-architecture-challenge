import { FolderPlus, Plus, X } from 'lucide-react';
import { createActions, type CreateAction } from '../app/createActions';

type GlobalCreateMenuProps = {
  open: boolean;
  onToggle: () => void;
  onClose: () => void;
  onCreate: (action: CreateAction) => void;
  onCreateCategory: () => void;
};

export default function GlobalCreateMenu({
  open,
  onToggle,
  onClose,
  onCreate,
  onCreateCategory,
}: GlobalCreateMenuProps) {
  return (
    <div className={`global-create ${open ? 'open' : ''}`}>
      {open && (
        <>
          <button
            className="global-create-scrim"
            type="button"
            aria-label="Fechar ações"
            onClick={onClose}
          />

          <div className="global-create-menu">
            <span>Adicionar</span>
            {createActions.map(({ id, label, icon: Icon }) => (
              <button key={id} type="button" onClick={() => onCreate(id)}>
                <Icon size={19} />
                <span>{label}</span>
              </button>
            ))}
            <button type="button" onClick={onCreateCategory}>
              <FolderPlus size={19} />
              <span>Categoria</span>
            </button>
          </div>
        </>
      )}

      <button
        className="global-create-fab"
        type="button"
        aria-label={open ? 'Fechar menu de cadastro' : 'Adicionar'}
        onClick={onToggle}
      >
        {open ? <X /> : <Plus />}
      </button>
    </div>
  );
}
