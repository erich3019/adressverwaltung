// Blättern in Listen: «Zurück», «Seite x von y», «Weiter».
// Server Components übergeben hrefFuer (Links), Client Components onWechsel (Schaltflächen).
import Link from 'next/link';

interface Props {
  seite: number;
  seiten: number;
  hrefFuer?: (seite: number) => string;
  onWechsel?: (seite: number) => void;
}

const aktivClass   = 'px-3 py-1 rounded-md border border-gray-300 bg-white text-gray-700 hover:bg-gray-50 transition-colors';
const inaktivClass = 'px-3 py-1 rounded-md border border-gray-200 bg-gray-50 text-gray-300';

export default function Seitenwahl({ seite, seiten, hrefFuer, onWechsel }: Props) {
  if (seiten <= 1) return null;

  function schritt(ziel: number, text: string) {
    if (ziel < 1 || ziel > seiten) {
      return <span className={inaktivClass}>{text}</span>;
    }
    if (hrefFuer) {
      return <Link href={hrefFuer(ziel)} className={aktivClass}>{text}</Link>;
    }
    return (
      <button type="button" onClick={() => onWechsel?.(ziel)} className={aktivClass}>
        {text}
      </button>
    );
  }

  return (
    <nav aria-label="Seiten" className="flex items-center justify-between mt-4 text-sm">
      {schritt(seite - 1, '← Zurück')}
      <span className="text-gray-500">Seite {seite} von {seiten}</span>
      {schritt(seite + 1, 'Weiter →')}
    </nav>
  );
}
