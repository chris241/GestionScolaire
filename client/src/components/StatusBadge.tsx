const STATUS_STYLES: Record<string, { label: string; className: string }> = {
  Paye: { label: 'Payé', className: 'bg-success-soft text-success' },
  Present: { label: 'Présent', className: 'bg-success-soft text-success' },
  EnAttente: { label: 'En attente', className: 'bg-warning-soft text-warning' },
  EnRetard: { label: 'En retard', className: 'bg-danger-soft text-danger' },
  Absent: { label: 'Absent', className: 'bg-danger-soft text-danger' },
  Annule: { label: 'Annulé', className: 'bg-border text-slate-soft' },
  Submitted: { label: 'Soumis', className: 'bg-border text-slate-soft' },
  UnderReview: { label: 'En examen', className: 'bg-warning-soft text-warning' },
  Accepted: { label: 'Accepté', className: 'bg-success-soft text-success' },
  Rejected: { label: 'Refusé', className: 'bg-danger-soft text-danger' },
  Enrolled: { label: 'Inscrit', className: 'bg-success-soft text-success' },
};

export function StatusBadge({ status }: { status: string }) {
  const style = STATUS_STYLES[status] ?? { label: status, className: 'bg-border text-slate-soft' };

  return (
    <span
      className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-medium ${style.className}`}
    >
      {style.label}
    </span>
  );
}
