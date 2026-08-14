import { useTranslation } from 'react-i18next'
import { startOAuthLogin } from '../OAuthConfig.jsx'

const ICONS = {
    google: (
        <svg viewBox="0 0 18 18" width="18" height="18" aria-hidden="true">
            <path fill="#4285F4" d="M17.64 9.2c0-.64-.06-1.25-.16-1.84H9v3.48h4.84a4.14 4.14 0 0 1-1.8 2.72v2.26h2.9c1.7-1.57 2.7-3.88 2.7-6.62Z" />
            <path fill="#34A853" d="M9 18c2.43 0 4.47-.8 5.96-2.18l-2.9-2.26c-.81.54-1.85.86-3.06.86-2.35 0-4.34-1.59-5.05-3.72H.96v2.33A9 9 0 0 0 9 18Z" />
            <path fill="#FBBC05" d="M3.95 10.7A5.4 5.4 0 0 1 3.67 9c0-.59.1-1.17.28-1.7V4.97H.96A9 9 0 0 0 0 9c0 1.45.35 2.83.96 4.03l2.99-2.33Z" />
            <path fill="#EA4335" d="M9 3.58c1.32 0 2.51.46 3.44 1.35l2.58-2.58C13.46.89 11.43 0 9 0A9 9 0 0 0 .96 4.97l2.99 2.33C4.66 5.17 6.65 3.58 9 3.58Z" />
        </svg>
    ),
    microsoft: (
        <svg viewBox="0 0 18 18" width="18" height="18" aria-hidden="true">
            <rect x="1" y="1" width="7.5" height="7.5" fill="#F35325" />
            <rect x="9.5" y="1" width="7.5" height="7.5" fill="#81BC06" />
            <rect x="1" y="9.5" width="7.5" height="7.5" fill="#05A6F0" />
            <rect x="9.5" y="9.5" width="7.5" height="7.5" fill="#FFBA08" />
        </svg>
    ),
    github: (
        <svg viewBox="0 0 18 18" width="18" height="18" aria-hidden="true">
            <path
                fill="#181717"
                d="M9 .3a9 9 0 0 0-2.85 17.55c.45.08.6-.2.6-.43v-1.68c-2.5.55-3.03-1.06-3.03-1.06-.41-1.03-1-1.31-1-1.31-.82-.56.06-.55.06-.55.9.06 1.38.93 1.38.93.8 1.38 2.11.98 2.63.75.08-.58.32-.98.57-1.21-2-.23-4.1-1-4.1-4.45 0-.98.35-1.79.92-2.42-.09-.23-.4-1.15.09-2.4 0 0 .76-.24 2.48.92a8.53 8.53 0 0 1 4.5 0c1.72-1.16 2.48-.92 2.48-.92.5 1.25.18 2.17.09 2.4.57.63.92 1.44.92 2.42 0 3.46-2.1 4.22-4.11 4.44.33.28.62.84.62 1.7v2.51c0 .24.15.52.61.43A9 9 0 0 0 9 .3Z"
            />
        </svg>
    ),
}

const PROVIDER_NAMES = {
    google: 'Google',
    microsoft: 'Microsoft',
    github: 'GitHub',
}

const PROVIDERS = ['google', 'microsoft', 'github']

// action: 'login' | 'register' — sadece buton metnini değiştirir.
export default function SocialLoginButtons({ action = 'login' }) {
    const { t } = useTranslation()
    const translationKey = action === 'register' ? 'auth.social.registerWithProvider' : 'auth.social.loginWithProvider'

    return (
        <div className="social-login">
            <div className="social-login-divider">
                <span>{t('auth.social.or')}</span>
            </div>

            <div className="social-login-grid">
                {PROVIDERS.map((provider) => (
                    <button
                        key={provider}
                        type="button"
                        className="social-login-button"
                        onClick={() => startOAuthLogin(provider)}
                    >
                        {ICONS[provider]}
                        <span>
                            {t(translationKey, { provider: PROVIDER_NAMES[provider] })}
                        </span>
                    </button>
                ))}
            </div>
        </div>
    )
}