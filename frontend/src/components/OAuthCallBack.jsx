import { useEffect, useRef, useState } from 'react'
import { externalLogin, setToken } from '../api.js'
import { consumeOAuthState, getRedirectUri } from '../OAuthConfig.jsx'
import AuthLayout from './AuthLayout.jsx'

// Google/Microsoft/GitHub'ın kullanıcıyı geri yönlendirdiği sayfa.
// URL: /oauth/callback?code=...&state=...  (veya hata durumunda ?error=...)
export default function OAuthCallBack({ onLoginSuccess, onCancel }) {
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
                setError('Giriş isteği doğrulanamadı (geçersiz veya süresi dolmuş state). Lütfen tekrar deneyin.')
                setStatus('error')
                return
            }

            if (!code) {
                setError('Sağlayıcıdan bir yetkilendirme kodu alınamadı.')
                setStatus('error')
                return
            }

            try {
                const result = await externalLogin(provider, code, getRedirectUri())
                setToken(result.token)
                window.history.replaceState({}, '', '/')
                onLoginSuccess(result)
            } catch (err) {
                setError(err.message)
                setStatus('error')
            }
        }

        run()
    }, [onLoginSuccess])

    if (status === 'processing') {
        return (
            <AuthLayout>
                <h1 className="auth-title">Giriş yapılıyor…</h1>
                <p className="auth-subtitle">Sağlayıcı ile bağlantı doğrulanıyor, lütfen bekleyin.</p>
            </AuthLayout>
        )
    }

    return (
        <AuthLayout>
            <h1 className="auth-title">Giriş başarısız</h1>
            <p className="auth-message auth-message-error">{error}</p>
            <button
                type="button"
                className="auth-submit"
                onClick={() => {
                    window.history.replaceState({}, '', '/')
                    onCancel()
                }}
            >
                Girişe Dön
            </button>
        </AuthLayout>
    )
}