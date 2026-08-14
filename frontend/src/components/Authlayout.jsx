import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getClientIp } from '../api.js'
import LanguageSwitcher from './LanguageSwitcher.jsx'
import './AuthLayout.css'

export default function AuthLayout({ children }) {
    const { t } = useTranslation()
    const [ip, setIp] = useState(null)

    useEffect(() => {
        let cancelled = false
        getClientIp().then((value) => {
            if (!cancelled) setIp(value)
        })
        return () => {
            cancelled = true
        }
    }, [])

    return (
        <div className="auth-shell">
            <LanguageSwitcher className="auth-language-switcher" />

            <div className="auth-left">
                <div className="auth-left-inner">
                    <div className="auth-logo">{t('auth.brandTagline')}</div>

                    <div className="auth-content">{children}</div>

                    <p className="auth-ip">
                        {t('auth.ipLabel', { ip: ip ?? '—' })}
                    </p>
                </div>
            </div>

            <div className="auth-right">
                <div className="todo-note-card">
                    <div className="todo-note-header">
                        <span className="todo-note-pin" aria-hidden="true">📌</span>
                        {t('auth.todoTitle')}
                    </div>
                    <ul className="todo-note-list">
                        <li className="todo-note-item todo-note-item-done">
                            <span className="todo-checkbox">✓</span> {t('auth.todoOrganize')}
                        </li>
                        <li className="todo-note-item todo-note-item-done">
                            <span className="todo-checkbox">✓</span> {t('auth.todoReminder')}
                        </li>
                        <li className="todo-note-item">
                            <span className="todo-checkbox" /> {t('auth.todoIdea')}
                        </li>
                        <li className="todo-note-item">
                            <span className="todo-checkbox" /> {t('auth.todoPlan')}
                        </li>
                        <li className="todo-note-item">
                            <span className="todo-checkbox" /> {t('auth.todoArchive')}
                        </li>
                    </ul>
                </div>
            </div>
        </div>
    )
}