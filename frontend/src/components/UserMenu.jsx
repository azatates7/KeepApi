import { useTranslation } from 'react-i18next'
import { SUPPORTED_LANGUAGES, applyLanguage } from '../i18n.js'

// Avatar + kullanıcı adı üzerine gelindiğinde açılan dropdown: dil seçimi (TR/EN) ve çıkış.
export default function UserMenu({ username, onLogout }) {
    const { t, i18n } = useTranslation()
    const current = SUPPORTED_LANGUAGES.includes(i18n.language) ? i18n.language : 'tr'
    const displayName = username || t('app.user')

    return (
        <div className="user-menu">
            <button type="button" className="current-user user-menu-trigger" title={displayName}>
                <div className="user-avatar">
                    {displayName.trim().charAt(0).toUpperCase()}
                </div>

                <span className="username">{displayName}</span>

                <svg className="user-menu-chevron" viewBox="0 0 24 24" width="14" height="14" fill="none" aria-hidden="true">
                    <path d="m7 10 5 5 5-5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
            </button>

            <div className="user-menu-dropdown">
                {/* Dil adları kasıtlı olarak çevrilmiyor: bir kullanıcı arayüzü İngilizce'yken bile
                    "Türkçe" seçeneğini kendi dilinde görmeli, "Turkish" olarak değil. */}
                <button
                    type="button"
                    className={`user-menu-item${current === 'tr' ? ' active' : ''}`}
                    onClick={() => applyLanguage('tr')}
                >
                    Türkçe (TR)
                </button>
                <button
                    type="button"
                    className={`user-menu-item${current === 'en' ? ' active' : ''}`}
                    onClick={() => applyLanguage('en')}
                >
                    English (EN)
                </button>

                <div className="user-menu-divider" />

                <button
                    type="button"
                    className="user-menu-item danger"
                    onClick={onLogout}
                >
                    {t('app.logout')}
                </button>
            </div>
        </div>
    )
}