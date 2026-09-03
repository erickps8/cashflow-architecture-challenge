import { LogOut } from 'lucide-react';

type PageHeaderProps = {
  title: string;
  description: string;
  onLogout: () => void;
};

export default function PageHeader({ title, description, onLogout }: PageHeaderProps) {
  return (
    <header className="page-header">
      <div>
        <span className="eyebrow">GESTOR FINANCEIRO</span>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>

      <button
        className="mobile-logout"
        type="button"
        aria-label="Sair da conta"
        onClick={onLogout}
      >
        <LogOut size={19} />
      </button>
    </header>
  );
}
