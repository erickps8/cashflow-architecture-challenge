#!/bin/sh
set -eu

echo "Aguardando banco cashflow_launch e migrations..."
until psql -h postgres -U cashflow -d cashflow_launch -tAc "SELECT to_regclass('\"Accounts\"') IS NOT NULL AND to_regclass('\"MonthlyBudgets\"') IS NOT NULL;" 2>/dev/null | grep -q t; do
  sleep 2
done

echo "Tabelas prontas. Carregando base de homologacao..."
psql -v ON_ERROR_STOP=1 -h postgres -U cashflow -d cashflow_launch -f /seed/seed.sql

echo "Seed concluido."
