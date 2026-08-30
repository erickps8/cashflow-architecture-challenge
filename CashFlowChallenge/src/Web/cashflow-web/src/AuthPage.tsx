import { FormEvent, useEffect, useRef, useState } from 'react';
import { Check, LoaderCircle, WalletCards } from 'lucide-react';
import * as api from './api';

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (options: { client_id: string; callback: (response: { credential: string }) => void }) => void;
          renderButton: (element: HTMLElement, options: object) => void;
        };
      };
    };
  }
}

type AuthMode = 'login' | 'register' | 'group' | 'pending' | 'forgot' | 'reset';

export default function AuthPage({ done }: { done: () => void }) {
  const resetParams = new URLSearchParams(window.location.search);
  const resetEmail = resetParams.get('resetEmail') ?? '';
  const resetToken = resetParams.get('resetToken') ?? '';

  const [mode, setMode] = useState<AuthMode>(resetEmail && resetToken ? 'reset' : 'login');
  const [user, setUser] = useState('');
  const [email, setEmail] = useState(resetEmail);
  const [pass, setPass] = useState('');
  const [confirmPass, setConfirmPass] = useState('');
  const [group, setGroup] = useState('');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(false);
  const googleRef = useRef<HTMLDivElement>(null);
  const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined;

  const passwordRules = [
    ['8 caracteres', pass.length >= 8],
    ['uma letra', /[A-Za-z]/.test(pass)],
    ['um número', /\d/.test(pass)],
    ['um caractere especial', /[^A-Za-z0-9]/.test(pass)],
  ] as const;

  const validPassword = passwordRules.every(([, valid]) => valid);

  async function finish(result: api.AuthResult) {
    if (result.pendingApproval) {
      api.session.clear();
      setMode('pending');
      return;
    }
    if (result.requiresGroup) {
      setMode('group');
      return;
    }
    if (result.token) done();
  }

  useEffect(() => {
    if (mode !== 'login' || !googleClientId) return;

    const initialize = () => {
      if (!window.google || !googleRef.current) return;
      window.google.accounts.id.initialize({
        client_id: googleClientId,
        callback: async (response) => {
          setLoading(true);
          setError('');
          try {
            await finish(await api.googleLogin(response.credential));
          } catch (exception) {
            setError(exception instanceof Error ? exception.message : 'Falha no login Google');
          } finally {
            setLoading(false);
          }
        },
      });
      googleRef.current.innerHTML = '';
      window.google.accounts.id.renderButton(googleRef.current, {
        theme: 'outline', size: 'large', width: 320, text: 'continue_with', shape: 'rectangular',
      });
    };

    const existing = document.querySelector('script[data-google-identity]') as HTMLScriptElement | null;
    if (existing) {
      initialize();
      return;
    }

    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.dataset.googleIdentity = 'true';
    script.onload = initialize;
    script.onerror = () => setError('Não foi possível carregar o login do Google.');
    document.head.appendChild(script);
  }, [mode, googleClientId]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (loading) return;
    if ((mode === 'register' || mode === 'reset') && !validPassword) {
      setError('Sua senha ainda não atende aos requisitos abaixo.');
      return;
    }
    if (mode === 'reset' && pass !== confirmPass) {
      setError('As senhas não conferem.');
      return;
    }

    setError('');
    setMessage('');
    setLoading(true);

    try {
      if (mode === 'login') {
        await finish(await api.login(user, pass));
      } else if (mode === 'register') {
        const check = await api.checkGroup(group);
        if (check.exists && !confirm(`O grupo “${check.name}” já existe. Deseja solicitar entrada nele?`)) return;
        await finish(await api.register(user, email, pass, group));
      } else if (mode === 'group') {
        const check = await api.checkGroup(group);
        if (check.exists && !confirm(`O grupo “${check.name}” já existe. Deseja solicitar entrada nele?`)) return;
        await finish(await api.chooseGroup(group));
      } else if (mode === 'forgot') {
        const result = await api.forgotPassword(email);
        setMessage(result.message);
      } else if (mode === 'reset') {
        await api.resetPassword(email, resetToken, pass);
        window.history.replaceState({}, '', window.location.pathname);
        setMessage('Senha alterada. Entre com sua nova senha.');
        setPass('');
        setConfirmPass('');
        setMode('login');
      }
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Não foi possível continuar.');
    } finally {
      setLoading(false);
    }
  }

  if (mode === 'pending') {
    return (
      <main className="login-page"><section className="login-card">
        <div className="brand-lockup"><div className="brand-mark"><WalletCards /></div><div><strong>CashFlow</strong><span>Finanças compartilhadas</span></div></div>
        <div className="login-copy"><span className="eyebrow">SOLICITAÇÃO ENVIADA</span><h1>Aguardando aprovação.</h1><p>O gestor do grupo precisa aceitar sua entrada. Depois disso, entre novamente.</p><button className="primary-button" onClick={() => setMode('login')}>Voltar ao login</button></div>
      </section></main>
    );
  }

  const title = mode === 'register' ? 'Comece seu espaço financeiro.' : mode === 'group' ? 'Escolha seu grupo.' : mode === 'forgot' ? 'Recupere sua senha.' : mode === 'reset' ? 'Crie uma nova senha.' : '';

  return (
    <main className="login-page"><section className="login-card">
      <div className="brand-lockup"><div className="brand-mark"><WalletCards /></div><div><strong>CashFlow</strong><span>Finanças compartilhadas</span></div></div>
      {mode !== 'login' && <div className="login-copy"><span className="eyebrow">{mode === 'forgot' || mode === 'reset' ? 'SEGURANÇA' : mode === 'register' ? 'CRIAR CONTA' : 'SEU GRUPO'}</span><h1>{title}</h1><p>{mode === 'forgot' ? 'Informe seu e-mail e enviaremos um link temporário.' : mode === 'reset' ? 'O link é temporário e só pode ser usado para redefinir sua senha.' : mode === 'group' ? 'Se ele já existir, o gestor precisará aprovar sua entrada.' : 'Acesse suas finanças com segurança.'}</p></div>}
      <form onSubmit={submit}>
        {mode === 'login' && <label>E-mail ou usuário<input value={user} onChange={(e) => setUser(e.target.value)} required disabled={loading} /></label>}
        {mode === 'register' && <><label>Nome<input value={user} onChange={(e) => setUser(e.target.value)} required disabled={loading} /></label><label>E-mail<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required disabled={loading} /></label></>}
        {mode === 'forgot' && <label>E-mail<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required disabled={loading} /></label>}
        {(mode === 'login' || mode === 'register' || mode === 'reset') && <label>Senha<input type="password" value={pass} onChange={(e) => setPass(e.target.value)} required disabled={loading} />{(mode === 'register' || mode === 'reset') && <div className="password-rules"><small>Sua senha precisa ter:</small>{passwordRules.map(([text, valid]) => <span key={text} className={valid ? 'valid' : ''}><Check size={14} />{text}</span>)}</div>}</label>}
        {mode === 'reset' && <label>Confirmar nova senha<input type="password" value={confirmPass} onChange={(e) => setConfirmPass(e.target.value)} required disabled={loading} /></label>}
        {mode === 'group' || mode === 'register' ? <label>Grupo / família / empresa<input value={group} onChange={(e) => setGroup(e.target.value)} required disabled={loading} placeholder="Ex.: Família Pinheiro" /></label> : null}
        {error && <div className="error">{error}</div>}
        {message && <div className="auth-note">{message}</div>}
        <button className="primary-button auth-submit" disabled={loading}>{loading && <LoaderCircle size={18} className="spin" />}{mode === 'login' ? 'Entrar' : mode === 'register' ? 'Criar conta' : mode === 'forgot' ? 'Enviar link' : mode === 'reset' ? 'Salvar nova senha' : 'Continuar'}</button>
      </form>
      {mode === 'login' && <button type="button" className="auth-link" disabled={loading} onClick={() => { setError(''); setMessage(''); setMode('forgot'); }}>Esqueci minha senha</button>}
      {mode === 'login' && <><div className="auth-divider"><span>ou</span></div>{googleClientId ? <div className="google-login" ref={googleRef} /> : <><button type="button" className="google-fallback" disabled><strong>G</strong> Continuar com Google</button><div className="auth-note">Login Google aguardando configuração do Client ID.</div></>}</>}
      {mode === 'login' && <button type="button" className="auth-link" disabled={loading} onClick={() => setMode('register')}>Criar meu cadastro</button>}
      {(mode === 'register' || mode === 'forgot') && <button type="button" className="auth-link" disabled={loading} onClick={() => { setError(''); setMessage(''); setMode('login'); }}>Voltar ao login</button>}
    </section></main>
  );
}
