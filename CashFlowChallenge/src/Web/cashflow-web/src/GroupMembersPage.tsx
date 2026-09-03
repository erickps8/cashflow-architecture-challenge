import { useEffect, useState } from 'react';
import { Check, Users, X } from 'lucide-react';
import * as api from './api';

type GroupMember = Awaited<ReturnType<typeof api.getGroupMembers>>[number];

function memberStatus(member: GroupMember) {
  if (member.role === 'Owner') {
    return 'Gestor';
  }

  if (member.status === 'Pending') {
    return 'Aguardando aprovação';
  }

  if (member.status === 'Active') {
    return 'Ativo';
  }

  return 'Rejeitado';
}

export default function GroupMembersPage() {
  const [items, setItems] = useState<GroupMember[]>([]);
  const [error, setError] = useState('');

  const load = async () => {
    try {
      setItems(await api.getGroupMembers());
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Falha ao carregar membros');
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const decide = async (id: string, approve: boolean) => {
    await api.decideGroupMember(id, approve);
    await load();
  };

  return (
    <section className="card modern-list-card">
      <div className="modern-list-head">
        <div>
          <span className="eyebrow">GRUPO</span>
          <h2>Membros e solicitações</h2>
          <p>O gestor controla quem pode acessar as finanças deste grupo.</p>
        </div>
        <Users />
      </div>

      {error && <div className="error">{error}</div>}

      <div className="modern-list">
        {items.map((member) => (
          <div className="modern-row" key={member.id}>
            <div className="modern-row-icon">
              <Users />
            </div>
            <div>
              <strong>{member.username || member.email}</strong>
              <span>
                {member.email} · {memberStatus(member)}
              </span>
            </div>

            {member.status === 'Pending' && (
              <div className="member-actions">
                <button
                  type="button"
                  className="danger-button"
                  onClick={() => void decide(member.id, false)}
                >
                  <X size={15} /> Rejeitar
                </button>
                <button
                  type="button"
                  className="primary-button"
                  onClick={() => void decide(member.id, true)}
                >
                  <Check size={15} /> Aprovar
                </button>
              </div>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
