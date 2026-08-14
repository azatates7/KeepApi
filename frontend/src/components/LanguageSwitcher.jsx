import { useTranslation } from 'react-i18next'
import { SUPPORTED_LANGUAGES, applyLanguage } from '../i18n.js'

// Header'da ve auth ekranlarında kullanılabilen basit TR/EN toggle butonu.
// Oturum açıksa yeni dili backend'e de yazar (Günlük Özet job'ı bu değeri okur).
export default function LanguageSwitcher({ className = '' }) {
    const { i18n } = useTranslation()
    const current = SUPPORTED_LANGUAGES.includes(i18n.language) ? i18n.language : 'tr'

    function handleToggle() {
        applyLanguage(current === 'tr' ? 'en' : 'tr')
    }

    return (
        <button
            type="button"
            className={`language-switcher${className ? ` ${className}` : ''}`}
            onClick={handleToggle}
            aria-label={i18n.t('language.toggleAriaLabel')}
            title={i18n.t('language.toggleAriaLabel')}
        >
            {current === 'tr' ? i18n.t('language.en') : i18n.t('language.tr')}
        </button>
    )
}