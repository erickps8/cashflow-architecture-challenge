import { expect, test } from '@playwright/test';

test('fluxo principal do CashFlow funciona ponta a ponta', async ({ page }) => {
  const failures: string[] = [];
  page.on('console', (message) => {
    if (message.type() === 'error') failures.push(`console: ${message.text()}`);
  });
  page.on('pageerror', (error) => {
    failures.push(`pageerror: ${error.message}`);
  });
  page.on('response', (response) => {
    if (response.status() >= 500) failures.push(`${response.status()} ${response.request().method()} ${response.url()}`);
  });

  const assertNoRuntimeFailures = () => expect(failures, failures.join('\n')).toEqual([]);
  const stamp = Date.now();
  const email = `e2e-${stamp}@cashflow.local`;
  const password = 'CashFlow@123';
  const group = `E2E ${stamp}`;

  await page.goto('/');
  await expect(page.getByText('CashFlow').first()).toBeVisible();
  assertNoRuntimeFailures();

  await page.getByRole('button', { name: 'Criar meu cadastro' }).click();
  await page.getByLabel('Nome').fill('Teste Automatizado');
  await page.getByLabel('E-mail').fill(email);
  await page.getByLabel('Senha').fill(password);
  await page.getByLabel('Grupo / família / empresa').fill(group);
  await page.getByRole('button', { name: 'Criar conta' }).click();

  await expect(page.getByText('Vamos montar sua vida financeira?')).toBeVisible({ timeout: 30_000 });
  await page.getByRole('button', { name: 'Prefiro cadastrar sozinho' }).click();
  await expect(page.getByRole('heading', { name: 'Visão geral' })).toBeVisible({ timeout: 30_000 });
  assertNoRuntimeFailures();

  const pages = [
    ['Balanço mensal', 'Balanço mensal'],
    ['Lançamentos', 'Lançamentos'],
    ['Cartões', 'Cartões'],
    ['Planejamento anual', 'Planejamento anual'],
    ['Grupo', 'Grupo'],
    ['Visão geral', 'Visão geral'],
  ] as const;

  for (const [button, heading] of pages) {
    await page.getByRole('button', { name: button, exact: true }).first().click();
    await expect(page.getByRole('heading', { name: heading, exact: true })).toBeVisible({ timeout: 30_000 });
    assertNoRuntimeFailures();
  }

  await page.getByRole('button', { name: 'Lançamentos', exact: true }).first().click();
  await expect(page.getByRole('button', { name: 'Recorrências', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Recorrências', exact: true }).click();
  await expect(page.getByText('Receitas recorrentes')).toBeVisible({ timeout: 30_000 });
  assertNoRuntimeFailures();

  await page.getByRole('button', { name: /sair/i }).first().click();
  await expect(page.getByRole('button', { name: 'Entrar' })).toBeVisible();
  await page.getByLabel('E-mail ou usuário').fill(email);
  await page.getByLabel('Senha').fill(password);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByRole('heading', { name: 'Visão geral' })).toBeVisible({ timeout: 30_000 });

  assertNoRuntimeFailures();
});
