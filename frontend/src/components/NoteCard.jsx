import { useState } from 'react'
import ColorDots from './ColorDots.jsx'

export default function NoteCard({ note, onUpdate, onDelete }) {
  const [editing, setEditing] = useState(false)
  const [title, setTitle] = useState(note.title)
  const [content, setContent] = useState(note.content)
  const [error, setError] = useState('')
  const [showPalette, setShowPalette] = useState(false)
  const [showReminder, setShowReminder] = useState(false)
  const isOverdue = note.reminderAt && new Date(note.reminderAt).getTime() <= Date.now()

  function saveReminder(e) {
    e.preventDefault()
    const value = new FormData(e.target).get('reminder')
    onUpdate(note.id, { reminderAt: localInputValueToIso(value) })
    setShowReminder(false)
  }

  function clearReminder() {
    onUpdate(note.id, { reminderAt: null })
    setShowReminder(false)
  }
  
   function commitEdit() {
        if (!title.trim()) {
            setError("Başlık boş bırakılamaz.")
            return
        }

        if (!content.trim()) {
            setError("Not boş bırakılamaz.")
            return
        }

        setError("")
        setEditing(false)

        if (title !== note.title || content !== note.content) {
            onUpdate(note.id, { title, content })
        }
   }

  function togglePin() {
    onUpdate(note.id, { pinned: !note.pinned })
  }

  function toggleArchive() {
    onUpdate(note.id, { archived: !note.archived })
  }

  function changeColor(color) {
    onUpdate(note.id, { color })
    setShowPalette(false)
  }

  function isoToLocalInputValue(iso) {
    if (!iso) return ''
    const d = new Date(iso)
    const offsetMs = d.getTimezoneOffset() * 60000
    return new Date(d.getTime() - offsetMs).toISOString().slice(0, 16)
  }

  function localInputValueToIso(value) {
    return value ? new Date(value).toISOString() : null
  }

  function formatReminder(iso) {
    return new Intl.DateTimeFormat('tr-TR', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(iso))
  }
  
  return (
    <article className={`note-card color-${note.color}`}>
      <button
        className={`pin-tab${note.pinned ? ' pinned' : ''}`}
        onClick={togglePin}
        aria-label={note.pinned ? 'Sabitlemeyi Kaldır' : 'Sabitle'}
        title={note.pinned ? 'Sabitlemeyi Kaldır' : 'Sabitle'}
      >
        <svg viewBox="0 0 24 24" width="15" height="15" fill="currentColor">
          <path d="M14 4v5c0 1.12.37 2.16 1 3H9c.65-.86 1-1.9 1-3V4h4m3-2H7c-.55 0-1 .45-1 1s.45 1 1 1h1v5c0 1.66-1.34 3-3 3v2h5.97v7l1 1 1-1v-7H18v-2c-1.66 0-3-1.34-3-3V4h1c.55 0 1-.45 1-1s-.45-1-1-1z" />
        </svg>
      </button>

      {note.reminderAt && (
          <span className={`reminder-badge${isOverdue ? ' overdue' : ''}`}>
            <svg viewBox="0 0 24 24" width="12" height="12" fill="none">
              <circle cx="12" cy="13" r="7" stroke="currentColor" strokeWidth="1.5" />
              <path d="M12 9.5V13l2.5 1.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
              <path d="M9 3.5L6.5 5.5M15 3.5L17.5 5.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
            </svg>
                    {formatReminder(note.reminderAt)}
          </span>
      )}

      {showReminder && (
          <form className="reminder-popover" onSubmit={saveReminder}>
            <input
                type="datetime-local"
                name="reminder"
                defaultValue={isoToLocalInputValue(note.reminderAt)}
                autoFocus
            />
            <div className="reminder-popover-actions">
              {note.reminderAt && (
                  <button type="button" className="link-btn" onClick={clearReminder}>
                    Kaldır
                  </button>
              )}
              <button type="submit" className="btn-primary btn-small">
                Kaydet
              </button>
            </div>
          </form>
      )}
      
      <button
          className={`icon-btn${note.reminderAt ? ' active' : ''}`}
          onClick={() => setShowReminder((s) => !s)}
          title="hatırlatma ekle"
          aria-label="hatırlatma ekle"
      >
        <svg viewBox="0 0 24 24" width="16" height="16" fill="none">
          <path d="M12 4a5 5 0 00-5 5v3.5l-1.5 3h13L17 12.5V9a5 5 0 00-5-5z" stroke="currentColor" strokeWidth="1.4" strokeLinejoin="round" />
          <path d="M10 18.5a2 2 0 004 0" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
        </svg>
      </button>
      
      {editing ? (
              <div className="note-edit" tabIndex={-1}
                  onBlur={(e) => {
                      if (!e.currentTarget.contains(e.relatedTarget)) {
                          commitEdit()
                      }
                  }}>
          <input
            className="note-title-input"
            value={title}
            onChange={(e) => {
                setTitle(e.target.value)
                setError("") }}
            // onBlur={commitEdit}
            placeholder="Başlık"
            autoFocus
          />
          <textarea
            className="note-content-input"
            value={content}
            onChange={(e) => {
                setContent(e.target.value)
                setError("")
            }}
            // onBlur={commitEdit}
            rows={5}
                  />

            {error && (
                <div className="note-error">
                    {error}
                </div>
            )}
        </div>
      ) : (
        <div className="note-body" onClick={() => setEditing(true)}>
          {note.title && <h3 className="note-title">{note.title}</h3>}
          <p className="note-content">{note.content || <span className="placeholder">Boş Not</span>}</p>
        </div>
      )}

      <footer className="note-footer">
        <div className="note-actions-left">
          <button
            className="icon-btn"
            onClick={() => setShowPalette((s) => !s)}
            title="Renk Değiştir"
            aria-label="Renk Değiştir"
          >
            <svg viewBox="0 0 24 24" width="16" height="16" fill="none">
              <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.4" />
            </svg>
          </button>
          <button
            className="icon-btn"
            onClick={toggleArchive}
            title={note.archived ? 'Arşivden Çıkar' : 'Arşivle'}
            aria-label="arşivle"
          >
            <svg viewBox="0 0 24 24" width="16" height="16" fill="none">
              <rect x="3" y="4" width="18" height="4" rx="1" stroke="currentColor" strokeWidth="1.4" />
              <path d="M5 8v11a1 1 0 001 1h12a1 1 0 001-1V8" stroke="currentColor" strokeWidth="1.4" />
              <path d="M10 12h4" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
            </svg>
          </button>
        </div>
        <button className="icon-btn danger" onClick={() => onDelete(note.id)} title="Sil" aria-label="Sil">
          <svg viewBox="0 0 24 24" width="16" height="16" fill="none">
            <path
              d="M4 7h16M9 7V5a1 1 0 011-1h4a1 1 0 011 1v2m-8 0v13a1 1 0 001 1h8a1 1 0 001-1V7"
              stroke="currentColor"
              strokeWidth="1.4"
              strokeLinecap="round"
            />
          </svg>
        </button>
      </footer>

      {showPalette && (
        <div className="palette-popover">
          <ColorDots value={note.color} onChange={changeColor} />
        </div>
      )}
    </article>
  )
}
