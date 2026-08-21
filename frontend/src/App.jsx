import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import Composer from './components/Composer.jsx'
import NoteCard from './components/NoteCard.jsx'
import SearchPanel from './components/SearchPanel.jsx'
import UserMenu from './components/UserMenu.jsx'
import {
    createNote,
    deleteNote,
    fetchNotes,
    fetchSearchNotes,
    updateNote,
    getToken,
    clearToken,
    logout,
    runDailySummary,
} from './api.js'
import { useReminders } from './components/useReminders.jsx'
import Trash from './components/Trash.jsx'
import Login from './components/Login.jsx'
import Register from './components/Register.jsx'
import OAuthCallBack from './components/OAuthCallBack.jsx'

export default function App() {
    const { t, i18n } = useTranslation()
    const [isAuthenticated, setIsAuthenticated] = useState(() => Boolean(getToken()))
    // 'login' | 'register'
    const [authView, setAuthView] = useState('login')
    const [prefillUsername, setPrefillUsername] = useState('')
    const [notes, setNotes] = useState([])
    const [searchNotes, setSearchNotes] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const [showArchived, setShowArchived] = useState(false)
    const [showTrash, setShowTrash] = useState(false)
    const [summaryRunning, setSummaryRunning] = useState(false)
    const [summaryError, setSummaryError] = useState(null)
    const summaryAbortRef = useRef(null)

    const [searchOpen, setSearchOpen] = useState(false)
    const [searchQuery, setSearchQuery] = useState('')
    const [searchFilter, setSearchFilter] = useState('all')

    useReminders(notes)

    const load = useCallback(async () => {
        try {
            setLoading(true)

            const [activeNotes, allSearchableNotes] = await Promise.all([
                fetchNotes(),
                fetchSearchNotes(),
            ])

            setNotes(activeNotes)
            setSearchNotes(allSearchableNotes)
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
        const savedUsername =
            localStorage.getItem('keepapi_username')

        if (savedUsername) {
            setPrefillUsername(savedUsername)
        }
    }, [isAuthenticated, load])

    function handleLoginSuccess(result) {
        console.log('Login result:', result)
        console.log('Username:', result?.userName)

        setIsAuthenticated(true)

        if (result?.userName) {
            setPrefillUsername(result.userName)

            localStorage.setItem(
                'keepapi_username',
                result.userName
            )
        }
    }

    function handleLogout() {
        cancelDailySummary()
        logout() // fire-and-forget: sunucu iptali best-effort, local çıkış onu beklemiyor
        clearToken()
        setNotes([])
        setSearchNotes([])
        setShowTrash(false)
        setSearchOpen(false)
        setSearchQuery('')
        setPrefillUsername('')
        localStorage.removeItem('keepapi_username')
        setIsAuthenticated(false)
    }

    function updateSearchNote(id, patch) {
        setSearchNotes((prev) =>
            prev.map((note) =>
                note.id === id ? { ...note, ...patch, updatedAt: new Date().toISOString() } : note
            )
        )
    }

    async function handleCreate(newNote) {
        if (!newNote.imageAdded && !newNote.title?.trim()) {
            newNote.title = newNote.checklist
                ? t('app.untitledList')
                : newNote.image
                    ? t('app.untitledImage')
                    : t('app.untitledNote')
        }

        if (!newNote.imageAdded && !newNote.content?.trim()) {
            alert(t('app.noteEmptyAlert'))
            return
        }

        const created = await createNote(newNote)
        setNotes((prev) => [created, ...prev])
        setSearchNotes((prev) => [created, ...prev])
    }

    async function handleUpdate(id, patch) {
        if ('title' in patch && !patch.title.trim()) {
            alert(t('app.titleEmptyAlert'))
            return
        }

        if (
            'content' in patch &&
            !patch.content.trim() &&
            !current?.checklist
        ) {
            alert(t('app.noteEmptyAlert'))
            return
        }

        const current = notes.find((n) => n.id === id)
            || searchNotes.find((n) => n.id === id)

        if (!current) return

        const optimistic = { ...current, ...patch }

        setNotes((prev) =>
            prev.map((note) => (note.id === id ? optimistic : note))
        )
        updateSearchNote(id, patch)

        try {
            const updated = await updateNote(id, { ...current, ...patch })

            setNotes((prev) =>
                prev.map((note) => (note.id === id ? updated : note))
            )

            setSearchNotes((prev) =>
                prev.map((note) => (note.id === id ? updated : note))
            )
        } catch (err) {
            await load()
            throw err
        }
    }

    async function handleDelete(id) {
        await deleteNote(id)

        // Ana liste çöp kutusundaki kayıtları göstermez.
        setNotes((prev) => prev.filter((note) => note.id !== id))

        // Arama ekranında kayıt görünmeye devam eder; kategorisi Silinmiş olur.
        setSearchNotes((prev) =>
            prev.map((note) =>
                note.id === id
                    ? {
                        ...note,
                        isDeleted: true,
                        status: 1,
                        updatedAt: new Date().toISOString(),
                    }
                    : note
            )
        )
    }

    function handleOpenTrash() {
        setSearchOpen(false)
        setShowTrash(true)
    }

    function openSearch() {
        setShowTrash(false)
        setSearchOpen(true)
    }

    function closeSearch() {
        setSearchOpen(false)
        setSearchQuery('')
        setSearchFilter('all')
    }

    function toggleArchiveView() {
        cancelDailySummary()
        setShowTrash(false)
        setSearchOpen(false)
        setShowArchived((current) => !current)
    }

    async function handleRunDailySummary() {
        if (summaryRunning) return

        const controller = new AbortController()
        summaryAbortRef.current = controller

        setSummaryRunning(true)
        setSummaryError(null)

        try {
            await runDailySummary(controller.signal)
            // Özet notu yeni oluşmuş/güncellenmiş olabilir; listeyi tazele.
            await load()
        } catch (err) {
            if (err.name === 'AbortError') {
                // Kullanıcı başka bir görünüme geçti / sayfadan ayrıldı — sessizce geç.
                return
            }

            if (err.message === 'UNAUTHORIZED') {
                setIsAuthenticated(false)
                return
            }

            setSummaryError(err.message)
        } finally {
            // Bu arada yeni bir çalıştırma başlatılmadıysa (ref hâlâ bizim controller'ımızsa) temizle.
            if (summaryAbortRef.current === controller) {
                summaryAbortRef.current = null
            }
            setSummaryRunning(false)
        }
    }

    // Arşiv/Aktif Notlar/Çöp Kutusu'na geçiş, dil değişimi, çıkış veya sayfa yenilemesi/kapatma —
    // hepsinde sürmekte olan bir özet oluşturma isteği varsa iptal edilir.
    function cancelDailySummary() {
        if (summaryAbortRef.current) {
            summaryAbortRef.current.abort()
            summaryAbortRef.current = null
        }
        setSummaryRunning(false)
    }

    useEffect(() => {
        i18n.on('languageChanged', cancelDailySummary)
        return () => i18n.off('languageChanged', cancelDailySummary)
    }, [i18n])

    useEffect(() => {
        window.addEventListener('beforeunload', cancelDailySummary)
        return () => window.removeEventListener('beforeunload', cancelDailySummary)
    }, [])

    const visible = useMemo(
        () => notes.filter((n) => (showArchived ? n.archived : !n.archived)),
        [notes, showArchived]
    )

    const pinned = visible.filter((n) => n.pinned)
    const others = visible.filter((n) => !n.pinned)

    // Google/Microsoft/GitHub bu sayfaya (redirect_uri) geri yönlendirir.
    if (!isAuthenticated && window.location.pathname === '/oauth/callback') {
        return (
            <OAuthCallBack
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

    return (
        <div className="app">
            <header className={`app-header${searchOpen ? ' search-header-active' : ''}`}>
                <div className="app-brand">
                    <a href="http://localhost:5173/" className="app-brand" aria-label={t('app.brandAlt')} >
                        <img
                            src="/keepapi-logo.png"
                            alt="KeepApi"
                            className="brand-logo"
                        />

                        <h1 className="wordmark">
                            KeepApi
                        </h1>
                    </a>
                </div>

                <div className={`search-box${searchOpen ? ' active' : ''}`}>
                    <svg viewBox="0 0 24 24" width="21" height="21" fill="none" aria-hidden="true">
                        <circle cx="11" cy="11" r="6.5" stroke="currentColor" strokeWidth="1.8" />
                        <path d="m16 16 4.2 4.2" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
                    </svg>

                    <input
                        value={searchQuery}
                        onFocus={openSearch}
                        onChange={(event) => {
                            setSearchQuery(event.target.value)
                            setSearchOpen(true)
                        }}
                        placeholder={t('app.searchPlaceholder')}
                        aria-label={t('app.searchAriaLabel')}
                    />

                    {searchOpen && (
                        <button
                            type="button"
                            className="search-clear"
                            onClick={() => {
                                if (searchQuery) {
                                    setSearchQuery('')
                                } else {
                                    closeSearch()
                                }
                            }}
                            aria-label={searchQuery ? t('app.searchClear') : t('app.searchClose')}
                            title={searchQuery ? t('app.searchClear') : t('app.searchClose')}
                        >
                            ×
                        </button>
                    )}
                </div>

                <div className="header-actions">
                    <button
                        className="header-action"
                        onClick={handleRunDailySummary}
                        disabled={summaryRunning}
                        title={t('app.dailySummaryTitle')}
                    >
                        {summaryRunning ? t('app.dailySummaryRunning') : t('app.dailySummaryButton')}
                    </button>

                    <button
                        className="header-action"
                        onClick={toggleArchiveView}
                        title={showArchived ? t('app.backToActiveNotes') : t('app.openArchive')}
                    >
                        {showArchived ? t('app.activeNotes') : t('app.archive')}
                    </button>

                    {!showTrash && (
                        <button
                            className="header-action"
                            onClick={() => {
                                cancelDailySummary()
                                setSearchOpen(false)
                                setShowTrash(true)
                            }}
                            title={t('app.openTrash')}
                        >
                            {t('app.trash')}
                        </button>
                    )}

                    <UserMenu username={prefillUsername} onLogout={handleLogout} />
                </div>
            </header>

            {showTrash ? (
                <Trash />
            ) : searchOpen ? (
                <SearchPanel
                    notes={searchNotes}
                    query={searchQuery}
                    filter={searchFilter}
                    onFilterChange={setSearchFilter}
                />
            ) : (
                <>
                    {!showArchived && <Composer onCreate={handleCreate} />}

                    {loading && <p className="status">{t('app.loading')}</p>}
                    {error && <p className="status error">{t('app.connectionError', { message: error })}</p>}
                    {summaryError && <p className="status error">{summaryError}</p>}

                    {!loading && visible.length === 0 && (
                        <p className="empty-state">
                            {showArchived
                                ? t('app.noNotesArchive')
                                : t('app.noNotesEmpty')}
                        </p>
                    )}

                    {pinned.length > 0 && (
                        <section className="note-section">
                            <h2 className="section-label">{t('app.pinnedSection')}</h2>
                            <div className="note-grid">
                                {pinned.map((note) => (
                                    <NoteCard
                                        key={note.id}
                                        note={note}
                                        onUpdate={handleUpdate}
                                        onDelete={handleDelete}
                                    />
                                ))}
                            </div>
                        </section>
                    )}

                    {others.length > 0 && (
                        <section className="note-section">
                            {pinned.length > 0 && <h2 className="section-label">{t('app.notesSection')}</h2>}
                            <div className="note-grid">
                                {others.map((note) => (
                                    <NoteCard
                                        key={note.id}
                                        note={note}
                                        onUpdate={handleUpdate}
                                        onDelete={handleDelete}
                                    />
                                ))}
                            </div>
                        </section>
                    )}
                </>
            )}
        </div>
    )
}