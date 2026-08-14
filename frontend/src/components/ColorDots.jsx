import { useTranslation } from 'react-i18next'

const COLOR_KEYS = ['default', 'sage', 'sky', 'sand', 'blush', 'lilac']

export default function ColorDots({ value, onChange }) {
    const { t } = useTranslation()
    return (
        <div className="color-dots" role="radiogroup" aria-label={t('colors.ariaLabel')}>
            {COLOR_KEYS.map((key) => {
                const label = t(`colors.${key}`)
                return (
                    <button
                        key={key}
                        type="button"
                        className={`color-dot color-${key}${value === key ? ' active' : ''}`}
                        title={label}
                        aria-label={label}
                        aria-pressed={value === key}
                        onClick={() => onChange(key)}
                    />
                )
            })}
        </div>
    )
}