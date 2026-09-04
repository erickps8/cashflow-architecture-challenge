import { LogOut, WalletCards } from 'lucide-react';
import { navigationItems, type TabId } from '../app/navigation';

type AppNavigationProps = {
  activeTab: TabId;
  userName?: string | null;
  userEmail?: string | null;
  onNavigate: (tab: TabId) => void;
  onLogout: () => void;
};

function firstName(value?: string | null) {
  return value?.trim().split(/\s+/)[0] || 'Usuário';
}

function initials(value?: string | null) {
  const parts = value?.trim().split(/\s+/).filter(Boolean) ?? [];
  if (parts.length === 0) return 'CF';
  return `${parts[0][0]}${parts.length > 1 ? parts.at(-1)![0] : ''}`.toUpperCase();
}

export default function AppNavigation({
  activeTab,
  userName,
  userEmail,
  onNavigate,
  onLogout,
}: AppNavigationProps) {
  return (
    <>
      <aside className="sidebar">
        <div className="logo">
          <span className="logo-mark">
            <WalletCards />
          </span>
          <div>
            <strong>CashFlow</strong>
            <small>Compartilhado</small>
          </div>
        </div>

        <nav>
          {navigationItems.map(({ id, label, icon: Icon }) => (
            <button
              key={id}
              type="button"
              className={activeTab === id ? 'active' : ''}
              onClick={() => onNavigate(id)}
            >
              <Icon size={19} />
              <span>{label}</span>
            </button>
          ))}
        </nav>

        <div className="sidebar-foot">
          <div className="profile-dot">{initials(userName)}</div>
          <div>
            <span>{firstName(userName)}</span>
            <small>{userEmail || 'Minha conta'}</small>
          </div>
          <button className="icon-button" type="button" aria-label="Sair" onClick={onLogout}>
            <LogOut />
          </button>
        </div>
      </aside>

      <nav className="mobile-nav">
        {navigationItems.map(({ id, mobileLabel, icon: Icon }) => (
          <button
            key={id}
            type="button"
            className={activeTab === id ? 'active' : ''}
            onClick={() => onNavigate(id)}
          >
            <Icon size={20} />
            <span>{mobileLabel}</span>
          </button>
        ))}
      </nav>
    </>
  );
}
