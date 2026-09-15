import { Capacitor, CapacitorHttp } from '@capacitor/core';

const tokenKey = 'cashflow_token';
const refreshUserKey = 'cashflow_refresh_user';
const refreshSessionKey = 'cashflow_refresh_session';
const refreshTokenKey = 'cashflow_refresh_token';
const refreshExpiresKey = 'cashflow_refresh_expires';
const apiBase = Capacitor.isNativePlatform() ? 'https://plania.cloud' : '';
const refreshBeforeMs = 10 * 60 * 1000;
let operation: Promise<void> | null = null;

export async function bootstrapRefreshSession() {
  await synchronizeSession();
  window.setInterval(() => void synchronizeSession(), 60_000);
}

async function synchronizeSession() {
  if (operation) return operation;
  operation = synchronizeSessionCore().finally(() => { operation = null; });
  return operation;
}

async function synchronizeSessionCore() {
  const accessToken = localStorage.getItem(tokenKey);
  const refresh = readRefreshSession();

  if (!accessToken) {
    clearRefreshSession();
    return;
  }

  const expiresAt = accessTokenExpiration(accessToken);
  if (!refresh) {
    if (expiresAt !== null && expiresAt <= Date.now()) {
      clearCashFlowSession();
      return;
    }

    await startRefreshSession(accessToken);
    return;
  }

  if (refresh.expiresAt <= Date.now()) {
    clearCashFlowSession();
    return;
  }

  if (expiresAt === null || expiresAt - Date.now() > refreshBeforeMs) return;
  await refreshAccessToken(refresh);
}

async function startRefreshSession(accessToken: string) {
  try {
    const response = await send('/auth/session/start', 'POST', undefined, accessToken);
    if (response.status < 200 || response.status >= 300) return;
    storeRefreshSession(response.data);
  } catch {
    // A falha ao criar a sessão de renovação não interrompe a sessão atual.
  }
}

async function refreshAccessToken(refresh: RefreshSession) {
  try {
    const response = await send('/auth/session/refresh', 'POST', {
      userId: refresh.userId,
      sessionId: refresh.sessionId,
      refreshToken: refresh.refreshToken,
    });

    if (response.status !== 200 || !response.data?.token) {
      clearCashFlowSession();
      return;
    }

    localStorage.setItem(tokenKey, response.data.token);
    storeRefreshSession(response.data);
  } catch {
    // Mantém a sessão atual em falhas transitórias de rede; a próxima verificação tenta novamente.
  }
}

async function send(path: string, method: string, data?: unknown, accessToken?: string) {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`;

  if (Capacitor.isNativePlatform()) {
    return CapacitorHttp.request({ url: `${apiBase}${path}`, method, headers, data });
  }

  const response = await fetch(`${apiBase}${path}`, {
    method,
    headers,
    body: data === undefined ? undefined : JSON.stringify(data),
  });
  const text = response.status === 204 ? '' : await response.text();
  let parsed: unknown = undefined;
  if (text) {
    try { parsed = JSON.parse(text); } catch { parsed = text; }
  }
  return { status: response.status, data: parsed as Record<string, string> | undefined };
}

type RefreshSession = {
  userId: string;
  sessionId: string;
  refreshToken: string;
  expiresAt: number;
};

function readRefreshSession(): RefreshSession | null {
  const userId = localStorage.getItem(refreshUserKey);
  const sessionId = localStorage.getItem(refreshSessionKey);
  const refreshToken = localStorage.getItem(refreshTokenKey);
  const expiresAt = Date.parse(localStorage.getItem(refreshExpiresKey) ?? '');
  return userId && sessionId && refreshToken && Number.isFinite(expiresAt)
    ? { userId, sessionId, refreshToken, expiresAt }
    : null;
}

function storeRefreshSession(data: Record<string, string> | undefined) {
  if (!data?.userId || !data.sessionId || !data.refreshToken || !data.expiresAt) return;
  localStorage.setItem(refreshUserKey, data.userId);
  localStorage.setItem(refreshSessionKey, data.sessionId);
  localStorage.setItem(refreshTokenKey, data.refreshToken);
  localStorage.setItem(refreshExpiresKey, data.expiresAt);
}

function accessTokenExpiration(token: string) {
  try {
    const payload = token.split('.')[1];
    if (!payload) return null;
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
    const decoded = JSON.parse(atob(padded)) as { exp?: number };
    return decoded.exp ? decoded.exp * 1000 : null;
  } catch {
    return null;
  }
}

function clearRefreshSession() {
  localStorage.removeItem(refreshUserKey);
  localStorage.removeItem(refreshSessionKey);
  localStorage.removeItem(refreshTokenKey);
  localStorage.removeItem(refreshExpiresKey);
}

function clearCashFlowSession() {
  localStorage.removeItem(tokenKey);
  localStorage.removeItem('cashflow_session_state');
  localStorage.removeItem('cashflow_session_name');
  localStorage.removeItem('cashflow_session_email');
  clearRefreshSession();
}
