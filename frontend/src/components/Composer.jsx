import { useState, useRef } from 'react'
import ColorDots from './ColorDots.jsx'
import { summarizeAttachment } from '../api.js'

const MAX_IMAGE_BYTES = 4 * 1024 * 1024 // 4MB
const MAX_ATTACHMENT_BYTES = 8 * 1024 * 1024 // 8MB, backend limitiyle tutarlı
const ALLOWED_ATTACHMENT_TYPES = ['image/', 'application/pdf', 'text/plain']

export default function Composer({ onCreate }) {
    const [expanded, setExpanded] = useState(false)
    const [mode, setMode] = useState('text') // 'text' | 'checklist' | 'image'
    const [title, setTitle] = useState('')
    const [content, setContent] = useState('')
    const [items, setItems] = useState([])
    const [image, setImage] = useState(null)
    const [imageError, setImageError] = useState('')
    const [color, setColor] = useState('default')
    const [attaching, setAttaching] = useState(false)
    const [attachError, setAttachError] = useState('')
    const fileInputRef = useRef(null)
    const attachInputRef = useRef(null)

    function reset() {
        setExpanded(false)
        setMode('text')
        setTitle('')
        setContent('')
        setItems([])
        setImage(null)
        setImageError('')
        setColor('default')
        setAttaching(false)
        setAttachError('')
        if (fileInputRef.current) fileInputRef.current.value = ''
        if (attachInputRef.current) attachInputRef.current.value = ''
    }

    function startChecklist() {
        setMode('checklist')
        setExpanded(true)
        setItems([{ key: 0, text: '' }])
    }

    function triggerImagePicker() {
        fileInputRef.current?.click()
    }

    function handleImageSelect(e) {
        const file = e.target.files?.[0]
        if (!file) return

        if (!file.type.startsWith('image/')) {
            setImageError('Lütfen bir görsel dosyası seçin.')
            return
        }
        if (file.size > MAX_IMAGE_BYTES) {
            setImageError('Görsel çok büyük (maks. 4MB).')
            return
        }

        setImageError('')
        const reader = new FileReader()
        reader.onload = () => {
            setImage(reader.result)
            setMode('image')
            setExpanded(true)
        }
        reader.readAsDataURL(file)
    }

    function triggerAttachPicker() {
        attachInputRef.current?.click()
    }

    async function handleAttachSelect(e) {
        const file = e.target.files?.[0]
        if (!file) return

        if (!ALLOWED_ATTACHMENT_TYPES.some((prefix) => file.type.startsWith(prefix))) {
            setAttachError('Sadece görsel, PDF veya metin dosyası yükleyebilirsiniz.')
            if (attachInputRef.current) attachInputRef.current.value = ''
            return
        }
        if (file.size > MAX_ATTACHMENT_BYTES) {
            setAttachError('Dosya çok büyük (maks. 8MB).')
            if (attachInputRef.current) attachInputRef.current.value = ''
            return
        }

        setAttachError('')
        setAttaching(true)
        setExpanded(true)

        try {
            const { title: summaryTitle, content: summaryContent } = await summarizeAttachment(file)
            await onCreate({
                title: summaryTitle,
                content: summaryContent,
                checklist: false,
                imageAdded: false,
                imageUrl: null,
                color,
                pinned: false,
            })
            reset()
        } catch (err) {
            setAttachError(err.message || 'Dosya özetlenemedi.')
            setAttaching(false)
            if (attachInputRef.current) attachInputRef.current.value = ''
        }
    }

    function removeImage() {
        setImage(null)
        if (fileInputRef.current) fileInputRef.current.value = ''
        if (mode === 'image') setMode('text')
    }

    function updateItemText(key, text) {
        setItems((prev) => prev.map((i) => (i.key === key ? { ...i, text } : i)))
    }

    function addItem(e) {
        e.preventDefault()
        const nextKey = items.length ? Math.max(...items.map((i) => i.key)) + 1 : 0
        setItems((prev) => [...prev, { key: nextKey, text: '' }])
    }

    function deleteItem(key) {
        setItems((prev) => (prev.length > 1 ? prev.filter((i) => i.key !== key) : prev))
    }

    function handleSave() {
        const trimmedTitle = title.trim()

        if (mode === 'checklist') {
            const cleanItems = items.filter((i) => i.text.trim().length > 0)
            if (!trimmedTitle && cleanItems.length === 0) {
                reset()
                return
            }
            onCreate({
                title: trimmedTitle,
                content: cleanItems.map((i) => `- [ ] ${i.text.trim()}`).join('\n'),
                checklist: true,
                imageAdded: false,
                imageUrl: null,
                color,
                pinned: false,
            })
        } else if (mode === 'image') {
            if (!image) {
                reset()
                return
            }
            onCreate({
                title: trimmedTitle,
                content: content.trim(),
                checklist: false,
                imageAdded: true,
                imageUrl: image, // artık Cloudinary'den dönen URL
                color,
                pinned: false
            })
        } else {
            const trimmedContent = content.trim()
            if (!trimmedTitle && !trimmedContent) {
                reset()
                return
            }
            onCreate({ title: trimmedTitle, content, checklist: false, imageAdded: false, imageUrl: null, color, pinned: false })
        }
        reset()
    }

    return (
        <div
            className={`composer${expanded ? ' expanded' : ''}`}
            onBlur={(e) => {
                if (!e.currentTarget.contains(e.relatedTarget)) handleSave()
            }}
        >
            <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                className="composer-file-input"
                onChange={handleImageSelect}
            />

            <input
                ref={attachInputRef}
                type="file"
                accept="image/*,application/pdf,text/plain"
                className="composer-file-input"
                onChange={handleAttachSelect}
            />

            {expanded && (
                <input
                    className="composer-title"
                    placeholder="Başlık"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    autoFocus={mode !== 'image'}
                />
            )}

            {mode === 'image' && image && (
                <div className="composer-image-preview">
                    <img src={image} alt="Seçilen görsel" />
                    <button
                        type="button"
                        className="composer-image-remove"
                        onClick={removeImage}
                        aria-label="Görseli kaldır"
                        title="Görseli kaldır"
                    >
                        ×
                    </button>
                </div>
            )}

            {mode === 'checklist' ? (
                <div className="composer-checklist">
                    {items.map((item, index) => (
                        <div key={item.key} className="checklist-item">
                            <input type="checkbox" checked={false} disabled className="checklist-checkbox" />
                            <input
                                className="checklist-text-input"
                                value={item.text}
                                autoFocus={index === 0}
                                onChange={(e) => updateItemText(item.key, e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === 'Enter') {
                                        e.preventDefault()
                                        addItem(e)
                                    }
                                }}
                                placeholder="Liste öğesi"
                            />
                            {items.length > 1 && (
                                <button
                                    type="button"
                                    className="checklist-delete"
                                    onClick={() => deleteItem(item.key)}
                                    aria-label="Öğeyi sil"
                                >
                                    ×
                                </button>
                            )}
                        </div>
                    ))}
                    <button type="button" className="checklist-add-row" onClick={addItem}>
                        <span className="checklist-add-icon">+</span> Liste öğesi
                    </button>
                </div>
            ) : (
                <textarea
                    className="composer-content"
                    placeholder={mode === 'image' ? 'Açıklama ekle (opsiyonel)...' : 'Bir Not Al...'}
                    value={content}
                    onFocus={() => setExpanded(true)}
                    onChange={(e) => setContent(e.target.value)}
                    rows={expanded ? (mode === 'image' ? 2 : 3) : 1}
                />
            )}

            {imageError && <div className="note-error">{imageError}</div>}

            {attaching && <div className="composer-attach-status">Dosya özetleniyor…</div>}
            {attachError && <div className="note-error">{attachError}</div>}

            {expanded ? (
                <div className="composer-footer">
                    <ColorDots value={color} onChange={setColor} />
                    <button className="btn-primary" onClick={handleSave}>
                        Kaydet
                    </button>
                </div>
            ) : (
                <div className="composer-footer composer-footer-collapsed">
                    <button
                        type="button"
                        className="icon-btn"
                        onClick={startChecklist}
                        title="Onay kutulu not"
                        aria-label="Onay kutulu not"
                    >
                        <svg
                            viewBox="0 0 24 24"
                            width="24"
                            height="24"
                            fill="none"
                            aria-hidden="true"
                        >
                            <rect
                                x="4"
                                y="4"
                                width="16"
                                height="16"
                                rx="1.5"
                                stroke="currentColor"
                                strokeWidth="1.8"
                            />

                            <path
                                d="M7.5 12.2L10.5 15.2L16.5 8.8"
                                stroke="currentColor"
                                strokeWidth="1.8"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                            />
                        </svg>
                    </button>
                    <button
                        type="button"
                        className="icon-btn"
                        onClick={triggerImagePicker}
                        title="Görselli not"
                        aria-label="Görselli not"
                    >
                        <svg viewBox="0 0 24 24" width="16" height="16" fill="none">
                            <rect x="3" y="4" width="18" height="16" rx="2" stroke="currentColor" strokeWidth="1.4" />
                            <circle cx="8.5" cy="9.5" r="1.5" stroke="currentColor" strokeWidth="1.2" />
                            <path d="M4 17l5-5 4 4 3-3 4 4" stroke="currentColor" strokeWidth="1.4" />
                        </svg>
                    </button>
                    <button
                        type="button"
                        className="icon-btn"
                        onClick={triggerAttachPicker}
                        disabled={attaching}
                        title="Görsel/belge yükle ve özetle"
                        aria-label="Görsel/belge yükle ve özetle"
                    >
                        <svg viewBox="0 0 24 24" width="16" height="16" fill="none">
                            <path
                                d="M16.5 6.5l-7.6 7.6a3 3 0 004.24 4.24l8-8a5 5 0 00-7.07-7.07l-8 8a7 7 0 009.9 9.9"
                                stroke="currentColor"
                                strokeWidth="1.4"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                            />
                        </svg>
                    </button>
                </div>
            )}
        </div>
    )
}