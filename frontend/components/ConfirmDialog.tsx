'use client';

// F-03: Eigener Bestätigungsdialog als Ersatz für nativen confirm()-Browser-Dialog.
// Vorteile: gestaltbar, barrierefrei, funktioniert in allen Umgebungen (auch iFrames).

interface Props {
  offen: boolean;
  meldung: string;
  onBestaetigen: () => void;
  onAbbrechen: () => void;
}

export default function ConfirmDialog({ offen, meldung, onBestaetigen, onAbbrechen }: Props) {
  if (!offen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
      role="dialog"
      aria-modal="true"
      aria-labelledby="dialog-meldung"
    >
      <div className="bg-white rounded-xl shadow-xl p-6 max-w-sm w-full mx-4">
        <p id="dialog-meldung" className="text-gray-800 mb-6 text-base">
          {meldung}
        </p>
        <div className="flex justify-end gap-3">
          <button
            onClick={onAbbrechen}
            className="px-4 py-2 rounded-lg border border-gray-300 text-gray-700 hover:bg-gray-50 font-medium transition-colors"
          >
            Abbrechen
          </button>
          <button
            onClick={onBestaetigen}
            className="px-4 py-2 rounded-lg bg-red-600 text-white hover:bg-red-700 font-medium transition-colors"
          >
            Löschen
          </button>
        </div>
      </div>
    </div>
  );
}
