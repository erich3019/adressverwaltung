// F-09: Einheitliche Fehleranzeige (vorher als gleiches Markup in mehreren Seiten und Formularen).

interface Props {
  /** Fehlertext; bei null oder leerem Text wird nichts gerendert. */
  meldung: string | null;
  className?: string;
}

export default function Fehlermeldung({ meldung, className = '' }: Props) {
  if (!meldung) return null;

  return (
    <div
      role="alert"
      className={`bg-red-50 border border-red-200 text-red-700 p-3 rounded-lg text-sm ${className}`}
    >
      {meldung}
    </div>
  );
}
