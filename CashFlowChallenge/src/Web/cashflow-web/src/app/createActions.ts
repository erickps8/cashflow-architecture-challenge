import {
  ArrowDownCircle,
  ArrowUpCircle,
  CreditCard,
  ReceiptText,
  Repeat2,
  ShoppingBag,
  type LucideIcon,
} from 'lucide-react';
import type { TabId } from './navigation';

export type CreateAction =
  | 'expense'
  | 'income'
  | 'recurring'
  | 'card'
  | 'purchase'
  | 'budget';

export type StandardCreateAction = Exclude<CreateAction, 'card'>;

export type CreateRequest = {
  id: number;
  action: CreateAction;
} | null;

export type CreateActionItem = {
  id: CreateAction;
  label: string;
  target: TabId;
  icon: LucideIcon;
};

export const createActions: CreateActionItem[] = [
  { id: 'expense', label: 'Despesa', target: 'entries', icon: ArrowDownCircle },
  { id: 'income', label: 'Receita', target: 'entries', icon: ArrowUpCircle },
  { id: 'recurring', label: 'Recorrência', target: 'recurring', icon: Repeat2 },
  { id: 'card', label: 'Novo cartão', target: 'cards', icon: CreditCard },
  { id: 'purchase', label: 'Compra no cartão', target: 'cards', icon: ShoppingBag },
  { id: 'budget', label: 'Limite de orçamento', target: 'budget', icon: ReceiptText },
];

export function getCreateTarget(action: CreateAction): TabId {
  return createActions.find((item) => item.id === action)?.target ?? 'balance';
}

export function toStandardCreateRequest(
  request: CreateRequest,
): { id: number; action: StandardCreateAction } | null {
  if (!request || request.action === 'card') {
    return null;
  }

  return {
    id: request.id,
    action: request.action,
  };
}
