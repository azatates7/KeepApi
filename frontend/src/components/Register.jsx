import { useState } from 'react'
import { register, verifyEmail } from '../api.js'
import AuthLayout from './AuthLayout.jsx'

export default function Register({ onNavigateLogin }) {
    // 'form' | 'verify'
    const [view, setView] = useState('form')

    const [userName, setUserName] = useState('')
    const [email, setEmail] = useState('')
    const [firstName, setFirstName] = useState('')
    const [lastName, setLastName] = useState('')
    const [password, setPassword] = useState('')
    const [confirmPassword, setConfirmPassword] = useState('')

    const [code, setCode] = useState('')

    const [loading, setLoading] = useState(false)
    const [error, setError] = useState(null)
    const [info, setInfo] = useState(null)

    async function handleRegisterSubmit(e) {
        e.preventDefault()
        setError(null)

        if (!userName.trim() || !email.trim() || !firstName.trim() || !lastName.trim() || !password) {
            setError('Tüm alanlar gerekli.')
            return
        }

        if (password !== confirmPassword) {
            setError('Şifreler eşleşmiyor.')
            return
        }

        setLoading(true)
        try {
            await register({
                userName: userName.trim(),
                email: email.trim(),
                firstName: firstName.trim(),
                lastName: lastName.trim(),
                password,
                confirmPassword,
            })
            setInfo(`${email.trim()} adresine gönderilen doğrulama kodunu gir.`)
            setView('verify')
        } catch (err) {
            setError(err.message)
        } finally {
            setLoading(false)
        }
    }

    async function handleVerifySubmit(e) {
        e.preventDefault()
        setError(null)

        if (!code.trim()) {
            setError('Doğrulama kodu gerekli.')
            return
        }

        setLoading(true)
        try {
            await verifyEmail(email.trim(), code.trim())
            onNavigateLogin(userName.trim())
        } catch (err) {
            setError(err.message)
        } finally {
            setLoading(false)
        }
    }

    return (
        <AuthLayout>
            {view === 'form' && (
                <>
                    <h1 className="auth-title">Kayıt Ol</h1>
                    <p className="auth-subtitle">Notlarını tutmaya başlamak için hesap oluştur</p>

                    <form className="auth-form" onSubmit={handleRegisterSubmit}>
                        <div className="auth-row-split">
                            <label className="auth-label">
                                Ad
                                <input
                                    className="auth-input"
                                    type="text"
                                    value={firstName}
                                    onChange={(e) => setFirstName(e.target.value)}
                                    autoFocus
                                />
                            </label>

                            <label className="auth-label">
                                Soyad
                                <input
                                    className="auth-input"
                                    type="text"
                                    value={lastName}
                                    onChange={(e) => setLastName(e.target.value)}
                                />
                            </label>
                        </div>

                        <label className="auth-label">
                            Kullanıcı Adı
                            <input
                                className="auth-input"
                                type="text"
                                value={userName}
                                onChange={(e) => setUserName(e.target.value)}
                                autoComplete="username"
                            />
                        </label>

                        <label className="auth-label">
                            E-posta
                            <input
                                className="auth-input"
                                type="email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                autoComplete="email"
                            />
                        </label>

                        <label className="auth-label">
                            Şifre
                            <input
                                className="auth-input"
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                autoComplete="new-password"
                            />
                        </label>

                        <label className="auth-label">
                            Şifre (Tekrar)
                            <input
                                className="auth-input"
                                type="password"
                                value={confirmPassword}
                                onChange={(e) => setConfirmPassword(e.target.value)}
                                autoComplete="new-password"
                            />
                        </label>

                        {error && <p className="auth-message auth-message-error">{error}</p>}

                        <button className="auth-submit" type="submit" disabled={loading}>
                            {loading ? 'Kayıt Oluşturuluyor…' : 'Kayıt Ol'}
                        </button>
                    </form>

                    <p className="auth-switch">
                        Zaten hesabın var mı?{' '}
                        <button type="button" className="auth-link" onClick={() => onNavigateLogin()}>
                            Giriş Yap
                        </button>
                    </p>
                </>
            )}

            {view === 'verify' && (
                <>
                    <h1 className="auth-title">E-postanı Doğrula</h1>
                    <p className="auth-subtitle">{info}</p>

                    <form className="auth-form" onSubmit={handleVerifySubmit}>
                        <label className="auth-label">
                            Doğrulama Kodu
                            <input
                                className="auth-input auth-input-code"
                                type="text"
                                inputMode="numeric"
                                maxLength={6}
                                value={code}
                                onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
                                autoFocus
                            />
                        </label>

                        {error && <p className="auth-message auth-message-error">{error}</p>}

                        <button className="auth-submit" type="submit" disabled={loading}>
                            {loading ? 'Doğrulanıyor…' : 'Doğrula ve Giriş Yap'}
                        </button>

                        <button type="button" className="auth-link auth-back" onClick={() => onNavigateLogin()}>
                            ← Girişe Dön
                        </button>
                    </form>
                </>
            )}
        </AuthLayout>
    )
}