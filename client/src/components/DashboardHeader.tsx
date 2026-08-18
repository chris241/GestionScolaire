interface DashboardHeaderProps {
  userName: string;
  schoolName?: string;
}

export function DashboardHeader({ userName, schoolName = 'Votre établissement' }: DashboardHeaderProps) {
  const today = new Date().toLocaleDateString('fr-FR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });

  return (
    <div className="flex flex-col gap-1">
      <p className="text-sm font-medium text-slate-soft capitalize">{today}</p>
      <h1 className="text-2xl font-semibold text-slate">
        Bienvenue, {userName}
      </h1>
      <p className="text-sm text-slate-soft">{schoolName} — tableau de bord de gestion scolaire</p>
    </div>
  );
}
