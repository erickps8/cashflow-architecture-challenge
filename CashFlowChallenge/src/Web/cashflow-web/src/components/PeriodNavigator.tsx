import { ChevronLeft, ChevronRight } from 'lucide-react';

type PeriodNavigatorProps = {
  year: number;
  month: number;
  label?: string;
  onChange: (year: number, month: number) => void;
};

function moveMonth(year: number, month: number, delta: number) {
  const date = new Date(year, month - 1 + delta, 1);

  return {
    year: date.getFullYear(),
    month: date.getMonth() + 1,
  };
}

function formatPeriod(year: number, month: number) {
  return new Date(year, month - 1, 1).toLocaleDateString('pt-BR', {
    month: 'long',
    year: 'numeric',
  });
}

export default function PeriodNavigator({
  year,
  month,
  label = 'Período',
  onChange,
}: PeriodNavigatorProps) {
  const navigate = (delta: number) => {
    const nextPeriod = moveMonth(year, month, delta);
    onChange(nextPeriod.year, nextPeriod.month);
  };

  return (
    <div className="period-nav">
      <button type="button" onClick={() => navigate(-1)} aria-label="Mês anterior">
        <ChevronLeft />
      </button>

      <div>
        <span>{label}</span>
        <strong>{formatPeriod(year, month)}</strong>
      </div>

      <button type="button" onClick={() => navigate(1)} aria-label="Próximo mês">
        <ChevronRight />
      </button>
    </div>
  );
}
