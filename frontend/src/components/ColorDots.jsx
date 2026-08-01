const COLORS = [
  { key: 'default', label: 'Kağıt' },
  { key: 'sage', label: 'Ada Çayı' },
  { key: 'sky', label: 'Gökyüzü' },
  { key: 'sand', label: 'Kum' },
  { key: 'blush', label: 'Pembe' },
  { key: 'lilac', label: 'Lila' },
]

export default function ColorDots({ value, onChange }) {
  return (
    <div className="color-dots" role="radiogroup" aria-label="Not Rengi">
      {COLORS.map((c) => (
        <button
          key={c.key}
          type="button"
          className={`color-dot color-${c.key}${value === c.key ? ' active' : ''}`}
          title={c.label}
          aria-label={c.label}
          aria-pressed={value === c.key}
          onClick={() => onChange(c.key)}
        />
      ))}
    </div>
  )
}
