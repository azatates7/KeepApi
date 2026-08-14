const NOTES_BASE_URL = 'http://localhost:5080/api/notes'
const AUTH_BASE_URL = 'http://localhost:5080/api/auth'

const TOKEN_KEY = 'keep_todo_token'

export function getToken() {
    return localStorage.getItem(TOKEN_KEY)
}

export function setToken(token) {
    localStorage.setItem(TOKEN_KEY, token)
}

export function clearToken() {
    localStorage.removeItem(TOKEN_KEY)
}

function authHeaders() {
    const token = getToken()
    return token ? { Authorization: `Bearer ${token}` } : {}
}

// Token geçersiz/süresi dolmuşsa (401) App.jsx'in login ekranına dönebilmesi için
// özel bir hata fırlatıyoruz.
class UnauthorizedError extends Error {
    constructor() {
        super('UNAUTHORIZED')
        this.name = 'UnauthorizedError'
    }
}

async function apiFetch(url, options = {}) {
    const res = await fetch(url, {
        ...options,
        headers: {
            ...authHeaders(),
            ...(options.headers || {}),
        },
    })

    if (res.status === 401) {
        clearToken()
        throw new UnauthorizedError()
    }

    return res
}

// ---- auth ----

async function unwrap(res, fallbackError) {
    let body = null
    try {
        body = await res.json()
    } catch {
        // body yok/parse edilemedi
    }

    if (!res.ok || (body && body.success === false)) {
        throw new Error(body?.message || fallbackError)
    }

    return body?.data
}

export async function login(userNameOrEmail, password, rememberMe) {
    const res = await fetch(`${AUTH_BASE_URL}/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userNameOrEmail, password, rememberMe }),
    })
    return unwrap(res, 'Giriş başarısız. Kullanıcı adı/e-posta veya şifre hatalı.')
}

export async function register({ userName, email, password, confirmPassword, firstName, lastName }) {
    const res = await fetch(`${AUTH_BASE_URL}/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userName, email, password, confirmPassword, firstName, lastName }),
    })
    return unwrap(res, 'Kayıt oluşturulamadı.')
}

export async function externalLogin(provider, code, redirectUri) {
    const res = await fetch(`${AUTH_BASE_URL}/external/${provider}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code, redirectUri }),
    })
    return unwrap(res, 'Giriş başarısız.')
}

export async function verifyEmail(email, code) {
    const res = await fetch(`${AUTH_BASE_URL}/verify-email`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, code }),
    })
    return unwrap(res, 'Doğrulama başarısız.')
}

// Login ekranında kullanıcıya gösterilen "IP adresiniz" bilgisi için.
// Backend'e bağımlı değil; başarısız olursa sessizce null döner (ekranda satır gizlenir).
export async function getClientIp() {
    try {
        const res = await fetch('https://api.ipify.org?format=json')
        if (!res.ok) return null
        const data = await res.json()
        return data.ip || null
    } catch {
        return null
    }
}

export async function forgotPassword(email) {
    const res = await fetch(`${AUTH_BASE_URL}/forgot-password`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email }),
    })
    return unwrap(res, 'İstek gönderilemedi.')
}

export async function resetPassword({ email, code, newPassword, confirmNewPassword }) {
    const res = await fetch(`${AUTH_BASE_URL}/reset-password`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, code, newPassword, confirmNewPassword }),
    })
    return unwrap(res, 'Şifre güncellenemedi.')
}

// Kullanıcının arayüz/özet dilini backend'e yazar (Günlük Özet job'ı bu değeri okur).
export async function updateLanguage(language) {
    const res = await apiFetch(`${AUTH_BASE_URL}/me/language`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ language }),
    })
    return unwrap(res, 'Dil güncellenemedi.')
}

// ---- notes ----

export async function fetchNotes() {
    const res = await apiFetch(NOTES_BASE_URL)
    if (!res.ok) throw new Error('Notlar Yüklenemedi')
    return res.json()
}

// Arama ekranı için aktif, arşivlenmiş, sabitlenmiş ve çöp kutusundaki
// kayıtları birlikte getirir. Kalıcı olarak silinen (Status = 0) kayıtlar dönmez.
export async function fetchSearchNotes() {
    const res = await apiFetch(`${NOTES_BASE_URL}/search`)

    if (!res.ok) {
        throw new Error('Arama kayıtları yüklenemedi')
    }

    return res.json()
}

export async function createNote(note) {
    const res = await apiFetch(NOTES_BASE_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(note),
    })
    if (!res.ok) throw new Error('Not Oluşturulamadı')
    return res.json()
}

export async function updateNote(id, note) {
    const res = await apiFetch(`${NOTES_BASE_URL}/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(note),
    })

    if (!res.ok) throw new Error('Not Güncellenemedi')
    return res.json()
}

export async function deleteNote(id) {
    const res = await apiFetch(`${NOTES_BASE_URL}/${id}`, { method: 'DELETE' })
    if (!res.ok && res.status !== 204) throw new Error('Not Silinemedi')
}

export async function fetchTrash() {
    const res = await apiFetch(`${NOTES_BASE_URL}/trash`)

    if (!res.ok)
        throw new Error("Not Çöp Kutusu Yüklenemedi")

    return res.json()
}

export async function restoreNote(id) {
    const res = await apiFetch(`${NOTES_BASE_URL}/${id}/restore`, {
        method: "PUT"
    })

    if (!res.ok)
        throw new Error("Not Geri Yüklenemedi")
}

export async function deleteForever(id) {
    const res = await apiFetch(`${NOTES_BASE_URL}/${id}/permanent`, {
        method: "DELETE"
    })

    if (!res.ok)
        throw new Error("Not Kalıcı Silinemedi")
}