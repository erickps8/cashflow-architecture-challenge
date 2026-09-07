import { useEffect, useState } from 'react';
import { WalletCards } from 'lucide-react';
import * as api from './api';
import AnnualPlan from './AnnualPlan';
import AuthPage from './AuthPage';
import CardsPage from './CardsPage';
import DashboardPage from './DashboardPage';
import EntriesPage from './EntriesPage';
import GroupMembersPage from './GroupMembersPage';
import MonthlyBalance from './MonthlyBalance';
import OnboardingWizard from './OnboardingWizard';
import {
  getCreateTarget,
  toStandardCreateRequest,
  type CreateAction,
  type CreateRequest,
} from './app/createActions';
import { getNavigationItem, type TabId } from './app/navigation';
import AppNavigation from './components/AppNavigation';
import CategoryModal from './components/CategoryModal';
import GlobalCreateMenu from './components/GlobalCreateMenu';
import PageHeader from './components/PageHeader';
import PeriodNavigator from './components/PeriodNavigator';

type LogoutProps = {
  logout: () => void;
};

type JwtPayload = {
  exp?: number;
};

function hasActiveSession() {
  const token = api.session.token;

  if (!token || api.session.state !== 'active') {
    return false;
  }

  try {
    const payload = token.split('.')[1];
    if (!payload) {
      return false;
    }

    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = JSON.parse(atob(normalized)) as JwtPayload;

    if (!decoded.exp) {
      return true;
    }

    const isExpired = decoded.exp * 1000 <= Date.now();
    if (isExpired) {
      api.session.clear();
      return false;
    }

    return true;
  } catch {
    api.session.clear();
    return false;
  }
}

function AppShell({ logout }: LogoutProps) {
  const now = new Date();
  const [tab, setTab] = useState<TabId>('dash');
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [createOpen, setCreateOpen] = useState(false);
  const [createRequest, setCreateRequest] = useState<CreateRequest>(null);
  const [categoryOpen, setCategoryOpen] = useState(false);

  const currentPage = getNavigationItem(tab);
  const standardCreateRequest = toStandardCreateRequest(createRequest);
  const firstName = api.session.name?.trim().split(/\s+/)[0];
  const pageDescription = tab === 'dash' && firstName
    ? `Olá, ${firstName}. ${currentPage.description}`
    : currentPage.description;

  const changePeriod = (nextYear: number, nextMonth: number) => {
    setYear(nextYear);
    setMonth(nextMonth);
  };

  const signOut = () => {
    api.session.clear();
    logout();
  };

  const create = (action: CreateAction) => {
    setTab(getCreateTarget(action));
    setCreateOpen(false);
    setCreateRequest({ id: Date.now(), action });
  };

  const openCategory = () => {
    setCreateOpen(false);
    setCategoryOpen(true);
  };

  return (
    <div className="app-shell">
      <AppNavigation
        activeTab={tab}
        userName={api.session.name}
        userEmail={api.session.email}
        onNavigate={setTab}
        onLogout={signOut}
      />

      <main className="content">
        <PageHeader
          title={currentPage.label}
          description={pageDescription}
          onLogout={signOut}
        />

        {currentPage.hasPeriod && (
          <PeriodNavigator year={year} month={month} compact onChange={changePeriod} />
        )}

        {tab === 'dash' && <DashboardPage year={year} month={month} />}
        {tab === 'balance' && (
          <MonthlyBalance year={year} month={month} onPeriodChange={changePeriod} />
        )}
        {tab === 'entries' && (
          <EntriesPage
            year={year}
            month={month}
            createRequest={standardCreateRequest}
          />
        )}
        {tab === 'cards' && (
          <CardsPage year={year} month={month} createRequest={createRequest} />
        )}
        {tab === 'annual' && <AnnualPlan />}
        {tab === 'members' && <GroupMembersPage />}
      </main>

      <GlobalCreateMenu
        open={createOpen}
        onToggle={() => setCreateOpen((value) => !value)}
        onClose={() => setCreateOpen(false)}
        onCreate={create}
        onCreateCategory={openCategory}
      />

      <CategoryModal open={categoryOpen} onClose={() => setCategoryOpen(false)} />
    </div>
  );
}

function LoadingApp() {
  return (
    <main className="login-page">
      <section className="login-card">
        <div className="brand-lockup">
          <div className="brand-mark">
            <WalletCards />
          </div>
          <div>
            <strong>CashFlow</strong>
            <span>Preparando seu espaço...</span>
          </div>
        </div>
      </section>
    </main>
  );
}

function LoggedApp({ logout }: LogoutProps) {
  const [checking, setChecking] = useState(true);
  const [onboarding, setOnboarding] = useState(false);

  useEffect(() => {
    let active = true;
    const now = new Date();

    Promise.all([
      api.getAccounts(),
      api.getRecurring(),
      api.getCards(),
      api.getEntries(now.getFullYear(), now.getMonth() + 1),
    ])
      .then(([accounts, recurring, cards, entries]) => {
        if (!active) {
          return;
        }

        const hasFinancialData =
          accounts.some((account) => account.isActive && Math.abs(account.initialBalance) > 0) ||
          recurring.some((item) => item.isActive) ||
          cards.some((card) => card.isActive) ||
          entries.length > 0;

        setOnboarding(!hasFinancialData);
      })
      .catch(() => {
        if (!active) {
          return;
        }

        if (!api.session.token) {
          logout();
          return;
        }

        setOnboarding(false);
      })
      .finally(() => {
        if (active) {
          setChecking(false);
        }
      });

    return () => {
      active = false;
    };
  }, [logout]);

  if (checking) {
    return <LoadingApp />;
  }

  if (onboarding) {
    const finishOnboarding = () => setOnboarding(false);
    return <OnboardingWizard onFinish={finishOnboarding} onSkip={finishOnboarding} />;
  }

  return <AppShell logout={logout} />;
}

export default function AppModern() {
  const [logged, setLogged] = useState(hasActiveSession);

  if (!logged) {
    return <AuthPage done={() => setLogged(true)} />;
  }

  return <LoggedApp logout={() => setLogged(false)} />;
}
