import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { register, verifyEmail } from '../api.js'
import AuthLayout from './AuthLayout.jsx'
import SocialLoginButtons from './SocialLoginButtons.jsx'

export default function Register({ onNavigateLogin }) {
    const { t } = useTranslation()
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
            setError(t('auth.register.missingFields'))
            return
        }

        if (password !== confirmPassword) {
            setError(t('auth.register.mismatch'))
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
            setInfo(t('auth.register.verificationInfo', { email: email.trim() }))
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
            setError(t('auth.verify.missingCode'))
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
                    <h1 className="auth-title">{t('auth.register.title')}</h1>
                    <p className="auth-subtitle">{t('auth.register.subtitle')}</p>

                    <form className="auth-form" onSubmit={handleRegisterSubmit}>
                        <div className="auth-row-split">
                            <label className="auth-label">
                                {t('auth.register.firstName')}
                                <input
                                    className="auth-input"
                                    type="text"
                                    value={firstName}
                                    onChange={(e) => setFirstName(e.target.value)}
                                    autoFocus
                                />
                            </label>

                            <label className="auth-label">
                                {t('auth.register.lastName')}
                                <input
                                    className="auth-input"
                                    type="text"
                                    value={lastName}
                                    onChange={(e) => setLastName(e.target.value)}
                                />
                            </label>
                        </div>

                        <label className="auth-label">
                            {t('auth.register.username')}
                            <input
                                className="auth-input"
                                type="text"
                                value={userName}
                                onChange={(e) => setUserName(e.target.value)}
                                autoComplete="username"
                            />
                        </label>

                        <label className="auth-label">
                            {t('auth.register.email')}
                            <input
                                className="auth-input"
                                type="email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                autoComplete="email"
                            />
                        </label>

                        <label className="auth-label">
                            {t('auth.register.password')}
                            <input
                                className="auth-input"
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                autoComplete="new-password"
                            />
                        </label>

                        <label className="auth-label">
                            {t('auth.register.confirmPassword')}
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
                            {loading ? t('auth.register.submitLoading') : t('auth.register.submit')}
                        </button>
                    </form>

                    <p className="auth-switch">
                        {t('auth.register.haveAccount')}{' '}
                        <button type="button" className="auth-link" onClick={() => onNavigateLogin()}>
                            {t('auth.register.login')}
                        </button>
                    </p>

                    <SocialLoginButtons action="register" />
                </>
            )}

            {view === 'verify' && (
                <>
                    <h1 className="auth-title">{t('auth.verify.title')}</h1>
                    <p className="auth-subtitle">{info}</p>

                    <form className="auth-form" onSubmit={handleVerifySubmit}>
                        <label className="auth-label">
                            {t('auth.verify.code')}
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
                            {loading ? t('auth.verify.submitLoading') : t('auth.verify.submit')}
                        </button>

                        <button type="button" className="auth-link auth-back" onClick={() => onNavigateLogin()}>
                            {t('auth.verify.backToLogin')}
                        </button>
                    </form>
                </>
            )}
        </AuthLayout>
    )
}