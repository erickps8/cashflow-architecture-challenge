import { LogOut, WalletCards } from 'lucide-react';
import { navigationItems, type TabId } from '../app/navigation';

type AppNavigationProps = {
  activeTab: TabId;
  onNavigate: (tab: TabId) => void;
  onLogout: () => void;
};

export default function AppNavigation({
  activeTab,
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
          <div className="profile-dot">CF</div>
          <div>
            <span>Minha conta</span>
            <small>Grupo financeiro</small>
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
