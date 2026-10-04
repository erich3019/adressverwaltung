// F-09: Gemeinsame Ladeanzeige für ganze Seiten.

interface Props {
  text: string;
}

export default function Ladeanzeige({ text }: Props) {
  return (
    <div className="flex justify-center items-center min-h-64">
      <div className="text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mx-auto mb-3"></div>
        <p className="text-gray-500">{text}</p>
      </div>
    </div>
  );
}
