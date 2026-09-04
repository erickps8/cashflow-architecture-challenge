import {
  ArrowDownCircle,
  CalendarRange,
  CreditCard,
  LayoutDashboard,
  Users,
  WalletCards,
  type LucideIcon,
} from 'lucide-react';

export type TabId =
  | 'dash'
  | 'balance'
  | 'entries'
  | 'cards'
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
    id: 'dash',
    label: 'Visão geral',
    mobileLabel: 'Visão',
    description: 'Sua situação financeira e próximos meses.',
    icon: LayoutDashboard,
    hasPeriod: true,
  },
  {
    id: 'balance',
    label: 'Balanço mensal',
    mobileLabel: 'Balanço',
    description: 'Quanto entrou, quanto saiu e quanto sobrou.',
    icon: WalletCards,
    hasPeriod: false,
  },
  {
    id: 'entries',
    label: 'Lançamentos',
    mobileLabel: 'Lançamentos',
    description: 'Lançamentos e recorrências em um só lugar.',
    icon: ArrowDownCircle,
    hasPeriod: true,
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
