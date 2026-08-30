import {
  ArrowDownCircle,
  CalendarRange,
  CreditCard,
  LayoutDashboard,
  PiggyBank,
  Repeat2,
  Users,
  WalletCards,
  type LucideIcon,
} from 'lucide-react';

export type TabId =
  | 'balance'
  | 'dash'
  | 'entries'
  | 'recurring'
  | 'cards'
  | 'budget'
  | 'annual'
  | 'members';

export type NavigationItem = {
  id: TabId;
  label: string;
  mobileLabel: string;
  description: string;
  icon: LucideIcon;
  hasPeriod: boolean;
};

export const navigationItems: NavigationItem[] = [
  {
    id: 'balance',
    label: 'Balanço mensal',
    mobileLabel: 'Balanço',
    description: 'Quanto entrou, quanto saiu e quanto sobrou.',
    icon: WalletCards,
    hasPeriod: false,
  },
  {
    id: 'dash',
    label: 'Visão geral',
    mobileLabel: 'Visão',
    description: 'Sua situação financeira e próximos meses.',
    icon: LayoutDashboard,
    hasPeriod: true,
  },
  {
    id: 'entries',
    label: 'Lançamentos',
    mobileLabel: 'Lançamentos',
    description: 'Seu histórico financeiro, sem formulário atrapalhando.',
    icon: ArrowDownCircle,
    hasPeriod: true,
  },
  {
    id: 'recurring',
    label: 'Recorrências',
    mobileLabel: 'Recorr.',
    description: 'Compromissos que se repetem automaticamente.',
    icon: Repeat2,
    hasPeriod: false,
  },
  {
    id: 'cards',
    label: 'Cartões',
    mobileLabel: 'Cartões',
    description: 'Cartões, compras e faturas em um só lugar.',
    icon: CreditCard,
    hasPeriod: true,
  },
  {
    id: 'budget',
    label: 'Orçamento',
    mobileLabel: 'Orçamento',
    description: 'Planeje limites e acompanhe o que ainda pode gastar.',
    icon: PiggyBank,
    hasPeriod: true,
  },
  {
    id: 'annual',
    label: 'Planejamento anual',
    mobileLabel: 'Anual',
    description: 'Antecipe meses de aperto e planeje o ano.',
    icon: CalendarRange,
    hasPeriod: false,
  },
  {
    id: 'members',
    label: 'Grupo',
    mobileLabel: 'Grupo',
    description: 'Gerencie quem compartilha as finanças deste grupo.',
    icon: Users,
    hasPeriod: false,
  },
];

export function getNavigationItem(tab: TabId) {
  return navigationItems.find((item) => item.id === tab) ?? navigationItems[0];
}
