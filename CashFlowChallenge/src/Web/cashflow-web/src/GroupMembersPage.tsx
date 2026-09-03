import { FormEvent, useEffect, useState } from 'react';
import { Check, Pencil, Trash2, Users, X } from 'lucide-react';
import * as api from './api';

type GroupMember = Awaited<ReturnType<typeof api.getGroupMembers>>[number];

function memberStatus(member: GroupMember) {
  if (member.role === 'Owner') return 'Gestor';
  if (member.status === 'Pending') return 'Aguardando aprovação';
  if (member.status === 'Active') return 'Ativo';
  return 'Rejeitado';
}

export default function GroupMembersPage() {
  const [items, setItems] = useState<GroupMember[]>([]);
  const [group, setGroup] = useState<api.GroupInfo | null>(null);
  const [groupName, setGroupName] = useState('');
  const [editingGroup, setEditingGroup] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const load = async () => {
    try {
      setError('');
      const [members, info] = await Promise.all([api.getGroupMembers(), api.getGroup()]);
      setItems(members);
      setGroup(info);
      setGroupName(info.name);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Falha ao carregar grupo');
    }
  };

  useEffect(() => { void load(); }, []);

  const run = async (work: () => Promise<void>) => {
    setBusy(true);
    setError('');
    try {
      await work();
      await load();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Não foi possível concluir a ação');
    } finally {
      setBusy(false);
    }
  };

  const decide = (id: string, approve: boolean) => run(() => api.decideGroupMember(id, approve));

  const remove = (member: GroupMember) => {
    if (!confirm(`Remover ${member.name || member.email} do grupo?`)) return;
    void run(() => api.removeGroupMember(member.id));
  };

  const rename = (event: FormEvent) => {
    event.preventDefault();
    const name = groupName.trim();
    if (!name || name === group?.name) {
      setEditingGroup(false);
      return;
    }
    void run(async () => {
      await api.renameGroup(name);
      setEditingGroup(false);
    });
  };

  const isOwner = group?.role === 'Owner';

  return (
    <section className="card modern-list-card">
      <div className="modern-list-head">
        <div>
          <span className="eyebrow">GRUPO</span>
          {!editingGroup ? (
            <div className="group-title-row">
              <h2>{group?.name || 'Membros e solicitações'}</h2>
              {isOwner && (
                <button type="button" className="icon-button" title="Editar nome do grupo" onClick={() => setEditingGroup(true)}>
                  <Pencil size={16} />
                </button>
              )}
            </div>
          ) : (
            <form className="group-name-form" onSubmit={rename}>
              <input value={groupName} onChange={(event) => setGroupName(event.target.value)} disabled={busy} autoFocus />
              <button className="primary-button" disabled={busy}>Salvar</button>
              <button type="button" className="secondary-button" disabled={busy} onClick={() => { setGroupName(group?.name || ''); setEditingGroup(false); }}>Cancelar</button>
            </form>
          )}
          <p>{isOwner ? 'Você controla quem pode acessar as finanças deste grupo.' : 'Veja quem participa deste grupo.'}</p>
        </div>
        <Users />
      </div>

      {error && <div className="error">{error}</div>}

      <div className="modern-list">
        {items.map((member) => (
          <div className="modern-row" key={member.id}>
            <div className="modern-row-icon"><Users /></div>
            <div>
              <strong>{member.name || member.email}</strong>
              <span>{member.email} · {memberStatus(member)}</span>
            </div>

            {isOwner && member.status === 'Pending' && (
              <div className="member-actions">
                <button type="button" className="danger-button" disabled={busy} onClick={() => void decide(member.id, false)}>
                  <X size={15} /> Rejeitar
                </button>
                <button type="button" className="primary-button" disabled={busy} onClick={() => void decide(member.id, true)}>
                  <Check size={15} /> Aprovar
                </button>
              </div>
            )}

            {isOwner && member.status === 'Active' && member.role !== 'Owner' && (
              <div className="member-actions">
                <button type="button" className="danger-button" disabled={busy} onClick={() => remove(member)}>
                  <Trash2 size={15} /> Remover
                </button>
              </div>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
