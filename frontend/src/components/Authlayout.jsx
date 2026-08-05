import { useEffect, useState } from 'react'
import { getClientIp } from '../api.js'
import './AuthLayout.css'

export default function AuthLayout({ children }) {
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
            <div className="auth-left">
                <div className="auth-left-inner">
                    <div className="auth-logo">Not Defteri</div>

                    <div className="auth-content">{children}</div>

                    <p className="auth-ip">
                        IP adresiniz: {ip ?? '—'}
                    </p>
                </div>
            </div>

            <div className="auth-right">
                <div className="todo-note-card">
                    <div className="todo-note-header">
                        <span className="todo-note-pin" aria-hidden="true">📌</span>
                        Yapılacaklar
                    </div>
                    <ul className="todo-note-list">
                        <li className="todo-note-item todo-note-item-done">
                            <span className="todo-checkbox">✓</span> Notlarını organize et
                        </li>
                        <li className="todo-note-item todo-note-item-done">
                            <span className="todo-checkbox">✓</span> Hatırlatıcı ekle
                        </li>
                        <li className="todo-note-item">
                            <span className="todo-checkbox" /> Önemli bir fikri not al
                        </li>
                        <li className="todo-note-item">
                            <span className="todo-checkbox" /> Günü planla
                        </li>
                        <li className="todo-note-item">
                            <span className="todo-checkbox" /> Arşivi gözden geçir
                        </li>
                    </ul>
                </div>
            </div>
        </div>
    )
}