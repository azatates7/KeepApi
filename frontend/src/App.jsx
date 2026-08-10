import { useCallback, useEffect, useState } from 'react'
import Composer from './components/Composer.jsx'
import NoteCard from './components/NoteCard.jsx'
import { createNote, deleteNote, fetchNotes, updateNote, getToken, clearToken } from './api.js'
import { useReminders } from './components/useReminders.jsx'
import Trash from './components/Trash.jsx'
import Login from './components/Login.jsx'
import Register from './components/Register.jsx'
import OAuthCallback from './components/OAuthCallback.jsx'

export default function App() {
    const [isAuthenticated, setIsAuthenticated] = useState(() => Boolean(getToken()))
    // 'login' | 'register'
    const [authView, setAuthView] = useState('login')
    const [prefillUsername, setPrefillUsername] = useState('')
    const [notes, setNotes] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const [showArchived, setShowArchived] = useState(false)
    const [showTrash, setShowTrash] = useState(false)

    useReminders(notes)

    const load = useCallback(async () => {
        try {
            setLoading(true)
            setNotes(await fetchNotes())
            setError(null)
        } catch (err) {
            if (err.message === 'UNAUTHORIZED') {
                setIsAuthenticated(false)
                return
            }
            setError(err.message)
        } finally {
            setLoading(false)
        }
    }, [])

    useEffect(() => {
        if (isAuthenticated) {
            load()
        }
    }, [isAuthenticated, load])

    function handleLoginSuccess() {
        setIsAuthenticated(true)
    }

    function handleLogout() {
        clearToken()
        setNotes([])
        setShowTrash(false)
        setIsAuthenticated(false)
    }

    async function handleCreate(newNote) {
        if (!newNote.imageAdded && !newNote.title?.trim()) {
            // alert("Başlık boş bırakılamaz.")
            // return
            newNote.title = newNote.checklist ? 'Yeni Liste' : newNote.image ? 'Yeni Görsel' : 'Yeni Not'
        }

        if (!newNote.imageAdded && !newNote.content?.trim()) {
            alert("Not boş bırakılamaz. İmage " + newNote.image + " Content " + newNote)
            return
        }

        // alert(
        //     "Image Status2: " + newNote.image +
        //     "\n\nContent:\n" +
        //     JSON.stringify(newNote, null, 2)
        // );
        const created = await createNote(newNote)
        setNotes((prev) => [created, ...prev])
    }

    async function handleUpdate(id, patch) {
        console.log("handleUpdate", id, patch);
        if ("title" in patch && !patch.title.trim()) {
            alert("Başlık boş bırakılamaz.")
            return
        }

        if ("content" in patch && !patch.content.trim()) {
            alert("Not boş bırakılamaz, resim kontrol edilmeliiii.")
            return
        }

        setNotes((prev) => prev.map((n) => (n.id === id ? { ...n, ...patch } : n)))
        const current = notes.find((n) => n.id === id)
        if (!current) return

        const updated = await updateNote(id, { ...current, ...patch })
        setNotes((prev) => prev.map((n) => (n.id === id ? updated : n)))
        console.log("handleUpdateDone", id, patch);
    }

    async function handleDelete(id) {
        // setNotes((prev) => prev.filter((n) => n.id !== id))
        await deleteNote(id)
        await load()
    }

    function handleOpenTrash() {
        setShowTrash(true)
    }

    function handleCloseTrash() {
        setShowTrash(false)
        load();
    }

    // Google/Microsoft/GitHub bu sayfaya (redirect_uri) geri yönlendirir.
    if (!isAuthenticated && window.location.pathname === '/oauth/callback') {
        return (
            <OAuthCallback
                onLoginSuccess={handleLoginSuccess}
                onCancel={() => setAuthView('login')}
            />
        )
    }

    if (!isAuthenticated) {
        if (authView === 'register') {
            return (
                <Register
                    onNavigateLogin={(userName) => {
                        if (userName) setPrefillUsername(userName)
                        setAuthView('login')
                    }}
                />
            )
        }

        return (
            <Login
                onLoginSuccess={handleLoginSuccess}
                onNavigateRegister={() => setAuthView('register')}
                prefillUsername={prefillUsername}
            />
        )
    }

    if (showTrash) {
        return (
            <Trash onBack={handleCloseTrash} />
        );
    }

    const visible = notes.filter((n) => (showArchived ? n.archived : !n.archived))
    const pinned = visible.filter((n) => n.pinned)
    const others = visible.filter((n) => !n.pinned)

    return (
        <div className="app">
            <header className="app-header">
                <h1 className="wordmark">Not Defteri</h1>
                <button className="toggle-archive" onClick={() => setShowArchived((s) => !s)}>
                    {showArchived ? '← Aktif Notlar' : 'Arşiv'}
                </button>

                <button
                    className="toggle-trash"
                    onClick={handleOpenTrash}
                >
                    🗑 Çöp Kutusu
                </button>

                <button className="toggle-archive" onClick={handleLogout}>
                    Çıkış Yap
                </button>
            </header>

            {!showArchived && <Composer onCreate={handleCreate} />}

            {loading && <p className="status">Yükleniyor…</p>}
            {error && <p className="status error">Bağlantı Hatası: {error}</p>}

            {!loading && visible.length === 0 && (
                <p className="empty-state">
                    {showArchived ? 'Arşivde Not Yok' : 'Henüz Not Yok — Yukarıdan Bir Tane Ekle'}
                </p>
            )}

            {pinned.length > 0 && (
                <section className="note-section">
                    <h2 className="section-label">Sabitlenmiş</h2>
                    <div className="note-grid">
                        {pinned.map((note) => (
                            <NoteCard key={note.id} note={note} onUpdate={handleUpdate} onDelete={handleDelete} />
                        ))}
                    </div>
                </section>
            )}

            {others.length > 0 && (
                <section className="note-section">
                    {pinned.length > 0 && <h2 className="section-label">Notlar</h2>}
                    <div className="note-grid">
                        {
                            others.map((note) => (
                                <NoteCard key={note.id} note={note} onUpdate={handleUpdate} onDelete={handleDelete} />
                            ))
                        }
                    </div>
                </section>
            )}
        </div>
    )
}