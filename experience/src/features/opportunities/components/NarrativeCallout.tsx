interface NarrativeBulletShape {
  emphasis: string;
  detail: string;
}

export function NarrativeCallout({
  bullets,
}: {
  bullets: NarrativeBulletShape[];
}) {
  return (
    <div className="mt-3 border-t border-text-muted/15 pt-2">
      <ul className="space-y-1 text-[11px] leading-4 text-text-secondary">
        {bullets.slice(0, 3).map((bullet) => (
          <li key={`${bullet.emphasis}-${bullet.detail}`} className="flex gap-1">
            <span aria-hidden="true">•</span>
            <span>
              <strong className="font-semibold text-text-primary">{bullet.emphasis}</strong>
              {' '}
              {bullet.detail}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}
