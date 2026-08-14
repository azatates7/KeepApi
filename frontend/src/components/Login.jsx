import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { login, forgotPassword, resetPassword, setToken } from '../api.js'
import { LANGUAGE_STORAGE_KEY, SUPPORTED_LANGUAGES } from '../i18n.js'
import AuthLayout from './AuthLayout.jsx'
import SocialLoginButtons from './SocialLoginButtons.jsx'

const REMEMBERED_USERNAME_KEY = 'keep_todo_remembered_username'
const REMEMBER_ME_KEY = 'keep_todo_remember_me'

export default function Login({ onLoginSuccess, onNavigateRegister, prefillUsername }) {
    const { t, i18n } = useTranslation()
    // 'login' | 'forgot-email' | 'forgot-reset'
    const [view, setView] = useState('login')

    const [userNameOrEmail, setUserNameOrEmail] = useState('')
    const [password, setPassword] = useState('')
    const [rememberMe, setRememberMe] = useState(false)

    const [loading, setLoading] = useState(false)
    const [error, setError] = useState(null)
    const [info, setInfo] = useState(null)

    // Şifremi unuttum akışı
    const [forgotEmail, setForgotEmail] = useState('')
    const [resetCode, setResetCode] = useState('')
    const [newPassword, setNewPassword] = useState('')
    const [confirmNewPassword, setConfirmNewPassword] = useState('')

    useEffect(() => {
        const remembered = localStorage.getItem(REMEMBER_ME_KEY) === 'true'
        if (remembered) {
            setRememberMe(true)
            setUserNameOrEmail(localStorage.getItem(REMEMBERED_USERNAME_KEY) || '')
        }
    }, [])

    // Kayıt/doğrulama akışından yeni dönüldüyse kullanıcı adını burada önceliklendir.
    useEffect(() => {
        if (prefillUsername) {
            setUserNameOrEmail(prefillUsername)
        }
    }, [prefillUsername])

    async function handleLoginSubmit(e) {
        e.preventDefault()
        setError(null)

        if (!userNameOrEmail.trim() || !password) {
            setError(t('auth.login.missingFields'))
            return
        }

        setLoading(true)
        try {
            const result = await login(userNameOrEmail.trim(), password, rememberMe)
            setToken(result.token)

            if (rememberMe) {
                localStorage.setItem(REMEMBER_ME_KEY, 'true')
                localStorage.setItem(REMEMBERED_USERNAME_KEY, userNameOrEmail.trim())
            } else {
                localStorage.removeItem(REMEMBER_ME_KEY)
                localStorage.removeItem(REMEMBERED_USERNAME_KEY)
            }

            // Kullanıcının kayıtlı dil tercihini arayüze uygula (Günlük Özet ile aynı dil).
            if (SUPPORTED_LANGUAGES.includes(result.preferredLanguage)) {
                i18n.changeLanguage(result.preferredLanguage)
                localStorage.setItem(LANGUAGE_STORAGE_KEY, result.preferredLanguage)
            }

            onLoginSuccess(result)
        } catch (err) {
            setError(err.message)
        } finally {
            setLoading(false)
        }
    }

    function goToForgotPassword() {
        setError(null)
        setInfo(null)
        setForgotEmail(userNameOrEmail.includes('@') ? userNameOrEmail : '')
        setView('forgot-email')
    }

    async function handleForgotEmailSubmit(e) {
        e.preventDefault()
        setError(null)

        if (!forgotEmail.trim()) {
            setError(t('auth.forgot.missingEmail'))
            return
        }

        setLoading(true)
        try {
            await forgotPassword(forgotEmail.trim())
            setInfo(t('auth.forgot.sentInfo'))
            setView('forgot-reset')
        } catch (err) {
            setError(err.message)
        } finally {
            setLoading(false)
        }
    }

    async function handleResetSubmit(e) {
        e.preventDefault()
        setError(null)

        if (!resetCode.trim() || !newPassword || !confirmNewPassword) {
            setError(t('auth.reset.missingFields'))
            return
        }

        if (newPassword !== confirmNewPassword) {
            setError(t('auth.reset.mismatch'))
            return
        }

        setLoading(true)
        try {
            await resetPassword({
                email: forgotEmail.trim(),
                code: resetCode.trim(),
                newPassword,
                confirmNewPassword,
            })
            setInfo(t('auth.reset.successInfo'))
            setUserNameOrEmail(forgotEmail.trim())
            setPassword('')
            setResetCode('')
            setNewPassword('')
            setConfirmNewPassword('')
            setView('login')
        } catch (err) {
            setError(err.message)
        } finally {
            setLoading(false)
        }
    }

    function backToLogin() {
        setError(null)
        setInfo(null)
        setView('login')
    }

    return (
        <AuthLayout>
            {view === 'login' && (
                <>
                    <h1 className="auth-title">{t('auth.login.title')}</h1>
                    <p className="auth-subtitle">{t('auth.login.subtitle')}</p>

                    <form className="auth-form" onSubmit={handleLoginSubmit}>
                        <label className="auth-label">
                            {t('auth.login.usernameOrEmail')}
                            <input
                                className="auth-input"
                                type="text"
                                value={userNameOrEmail}
                                onChange={(e) => setUserNameOrEmail(e.target.value)}
                                autoComplete="username"
                                autoFocus
                            />
                        </label>

                        <label className="auth-label">
                            {t('auth.login.password')}
                            <input
                                className="auth-input"
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                autoComplete="current-password"
                            />
                        </label>

                        <div className="auth-row">
                            <label className="auth-checkbox">
                                <input
                                    type="checkbox"
                                    checked={rememberMe}
                                    onChange={(e) => setRememberMe(e.target.checked)}
                                />
                                {t('auth.login.rememberMe')}
                            </label>

                            <button type="button" className="auth-link" onClick={goToForgotPassword}>
                                {t('auth.login.forgotPassword')}
                            </button>
                        </div>

                        {error && <p className="auth-message auth-message-error">{error}</p>}

                        <button className="auth-submit" type="submit" disabled={loading}>
                            {loading ? t('auth.login.submitLoading') : t('auth.login.submit')}
                        </button>
                    </form>

                    <p className="auth-switch">
                        {t('auth.login.noAccount')}{' '}
                        <button type="button" className="auth-link" onClick={onNavigateRegister}>
                            {t('auth.login.register')}
                        </button>
                    </p>

                    <SocialLoginButtons action="login" />
                </>
            )}

            {view === 'forgot-email' && (
                <>
                    <h1 className="auth-title">{t('auth.forgot.title')}</h1>
                    <p className="auth-subtitle">{t('auth.forgot.subtitle')}</p>

                    <form className="auth-form" onSubmit={handleForgotEmailSubmit}>
                        <label className="auth-label">
                            {t('auth.forgot.email')}
                            <input
                                className="auth-input"
                                type="email"
                                value={forgotEmail}
                                onChange={(e) => setForgotEmail(e.target.value)}
                                autoComplete="email"
                                autoFocus
                            />
                        </label>

                        {error && <p className="auth-message auth-message-error">{error}</p>}

                        <button className="auth-submit" type="submit" disabled={loading}>
                            {loading ? t('auth.forgot.submitLoading') : t('auth.forgot.submit')}
                        </button>

                        <button type="button" className="auth-link auth-back" onClick={backToLogin}>
                            {t('auth.forgot.backToLogin')}
                        </button>
                    </form>
                </>
            )}

            {view === 'forgot-reset' && (
                <>
                    <h1 className="auth-title">{t('auth.reset.title')}</h1>
                    <p className="auth-subtitle">
                        {t('auth.reset.subtitle', { email: forgotEmail })}
                    </p>

                    <form className="auth-form" onSubmit={handleResetSubmit}>
                        {info && <p className="auth-message auth-message-info">{info}</p>}

                        <label className="auth-label">
                            {t('auth.reset.code')}
                            <input
                                className="auth-input auth-input-code"
                                type="text"
                                inputMode="numeric"
                                maxLength={6}
                                value={resetCode}
                                onChange={(e) => setResetCode(e.target.value.replace(/\D/g, ''))}
                                autoFocus
                            />
                        </label>

                        <label className="auth-label">
                            {t('auth.reset.newPassword')}
                            <input
                                className="auth-input"
                                type="password"
                                value={newPassword}
                                onChange={(e) => setNewPassword(e.target.value)}
                                autoComplete="new-password"
                            />
                        </label>

                        <label className="auth-label">
                            {t('auth.reset.confirmNewPassword')}
                            <input
                                className="auth-input"
                                type="password"
                                value={confirmNewPassword}
                                onChange={(e) => setConfirmNewPassword(e.target.value)}
                                autoComplete="new-password"
                            />
                        </label>

                        {error && <p className="auth-message auth-message-error">{error}</p>}

                        <button className="auth-submit" type="submit" disabled={loading}>
                            {loading ? t('auth.reset.submitLoading') : t('auth.reset.submit')}
                        </button>

                        <button type="button" className="auth-link auth-back" onClick={backToLogin}>
                            {t('auth.reset.backToLogin')}
                        </button>
                    </form>
                </>
            )}
        </AuthLayout>
    )
}