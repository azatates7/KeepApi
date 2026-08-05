import { useEffect, useState } from 'react'
import { login, forgotPassword, resetPassword, setToken } from '../api.js'
import AuthLayout from './AuthLayout.jsx'

const REMEMBERED_USERNAME_KEY = 'keep_todo_remembered_username'
const REMEMBER_ME_KEY = 'keep_todo_remember_me'

export default function Login({ onLoginSuccess, onNavigateRegister, prefillUsername }) {
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
            setError('Kullanıcı adı/e-posta ve şifre gerekli.')
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
            setError('E-posta adresi gerekli.')
            return
        }

        setLoading(true)
        try {
            await forgotPassword(forgotEmail.trim())
            setInfo('E-posta adresiniz sistemde kayıtlıysa, doğrulama kodu gönderildi.')
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
            setError('Kod ve yeni şifre alanları gerekli.')
            return
        }

        if (newPassword !== confirmNewPassword) {
            setError('Yeni şifreler eşleşmiyor.')
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
            setInfo('Şifreniz güncellendi. Şimdi yeni şifrenizle giriş yapabilirsiniz.')
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
                    <h1 className="auth-title">Hoşgeldiniz</h1>
                    <p className="auth-subtitle">Notlarına ulaşmak için giriş yap</p>

                    <form className="auth-form" onSubmit={handleLoginSubmit}>
                        <label className="auth-label">
                            Kullanıcı Adı veya E-posta
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
                            Şifre
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
                                Beni Hatırla
                            </label>

                            <button type="button" className="auth-link" onClick={goToForgotPassword}>
                                Şifremi Unuttum
                            </button>
                        </div>

                        {error && <p className="auth-message auth-message-error">{error}</p>}

                        <button className="auth-submit" type="submit" disabled={loading}>
                            {loading ? 'Giriş Yapılıyor…' : 'Giriş Yap'}
                        </button>
                    </form>

                    <p className="auth-switch">
                        Hesabın yok mu?{' '}
                        <button type="button" className="auth-link" onClick={onNavigateRegister}>
                            Kayıt Ol
                        </button>
                    </p>
                </>
            )}

            {view === 'forgot-email' && (
                <>
                    <h1 className="auth-title">Şifremi Unuttum</h1>
                    <p className="auth-subtitle">Şifreni sıfırlamak için e-posta adresini gir</p>

                    <form className="auth-form" onSubmit={handleForgotEmailSubmit}>
                        <label className="auth-label">
                            E-posta
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
                            {loading ? 'Gönderiliyor…' : 'Doğrulama Kodu Gönder'}
                        </button>

                        <button type="button" className="auth-link auth-back" onClick={backToLogin}>
                            ← Girişe Dön
                        </button>
                    </form>
                </>
            )}

            {view === 'forgot-reset' && (
                <>
                    <h1 className="auth-title">Kodu Doğrula</h1>
                    <p className="auth-subtitle">
                        {forgotEmail} adresine gönderilen kodu ve yeni şifreni gir
                    </p>

                    <form className="auth-form" onSubmit={handleResetSubmit}>
                        {info && <p className="auth-message auth-message-info">{info}</p>}

                        <label className="auth-label">
                            Doğrulama Kodu
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
                            Yeni Şifre
                            <input
                                className="auth-input"
                                type="password"
                                value={newPassword}
                                onChange={(e) => setNewPassword(e.target.value)}
                                autoComplete="new-password"
                            />
                        </label>

                        <label className="auth-label">
                            Yeni Şifre (Tekrar)
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
                            {loading ? 'Güncelleniyor…' : 'Şifreyi Güncelle'}
                        </button>

                        <button type="button" className="auth-link auth-back" onClick={backToLogin}>
                            ← Girişe Dön
                        </button>
                    </form>
                </>
            )}
        </AuthLayout>
    )
}