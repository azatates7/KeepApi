import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { externalLogin, setToken } from '../api.js'
import { consumeOAuthState, getRedirectUri } from '../OAuthConfig.jsx'
import { LANGUAGE_STORAGE_KEY, SUPPORTED_LANGUAGES } from '../i18n.js'
import AuthLayout from './AuthLayout.jsx'

// Google/Microsoft/GitHub'ın kullanıcıyı geri yönlendirdiği sayfa.
// URL: /oauth/callback?code=...&state=...  (veya hata durumunda ?error=...)
export default function OAuthCallBack({ onLoginSuccess, onCancel }) {
    const { t, i18n } = useTranslation()
    const [status, setStatus] = useState('processing') // 'processing' | 'error'
    const [error, setError] = useState(null)
    const ranOnce = useRef(false)

    useEffect(() => {
        if (ranOnce.current) return
        ranOnce.current = true

        async function run() {
            const params = new URLSearchParams(window.location.search)
            const providerError = params.get('error_description') || params.get('error')
            const code = params.get('code')
            const state = params.get('state')

            if (providerError) {
                setError(providerError)
                setStatus('error')
                return
            }

            const provider = consumeOAuthState(state)

            if (!provider) {
                setError(t('auth.oauth.invalidState'))
                setStatus('error')
                return
            }

            if (!code) {
                setError(t('auth.oauth.missingCode'))
                setStatus('error')
                return
            }

            try {
                const result = await externalLogin(provider, code, getRedirectUri())
                setToken(result.token)

                if (SUPPORTED_LANGUAGES.includes(result.preferredLanguage)) {
                    i18n.changeLanguage(result.preferredLanguage)
                    localStorage.setItem(LANGUAGE_STORAGE_KEY, result.preferredLanguage)
                }

                window.history.replaceState({}, '', '/')
                onLoginSuccess(result)
            } catch (err) {
                setError(err.message)
                setStatus('error')
            }
        }

        run()
    }, [onLoginSuccess, t, i18n])

    if (status === 'processing') {
        return (
            <AuthLayout>
                <h1 className="auth-title">{t('auth.oauth.processingTitle')}</h1>
                <p className="auth-subtitle">{t('auth.oauth.processingSubtitle')}</p>
            </AuthLayout>
        )
    }

    return (
        <AuthLayout>
            <h1 className="auth-title">{t('auth.oauth.failedTitle')}</h1>
            <p className="auth-message auth-message-error">{error}</p>
            <button
                type="button"
                className="auth-submit"
                onClick={() => {
                    window.history.replaceState({}, '', '/')
                    onCancel()
                }}
            >
                {t('auth.oauth.backToLogin')}
            </button>
        </AuthLayout>
    )
}