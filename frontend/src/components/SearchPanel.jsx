import { useMemo } from 'react'

function categoryInfo(note) {
    if (note.isDeleted) {
        return {
            label: 'Silinmiş',
            className: 'deleted',
        }
    }

    if (note.pinned) {
        return {
            label: 'Sabitlenmiş',
            className: 'pinned',
        }
    }

    if (note.archived) {
        return {
            label: 'Arşivlenmiş',
            className: 'archived',
        }
    }

    return {
        label: 'Normal Not',
        className: 'normal',
    }
}

function matchesFilter(note, filter) {
    if (filter === 'normal') {
        return (
            !note.isDeleted &&
            !note.archived &&
            !note.pinned
        )
    }

    if (filter === 'pinned') {
        return !note.isDeleted && note.pinned
    }

    if (filter === 'archived') {
        return !note.isDeleted && note.archived
    }

    if (filter === 'deleted') {
        return note.isDeleted
    }

    return true
}

function formatDate(value) {
    if (!value) {
        return ''
    }

    const date = new Date(value)

    if (Number.isNaN(date.getTime())) {
        return ''
    }

    return new Intl.DateTimeFormat('tr-TR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    }).format(date)
}

function NoteSearchResult({ note }) {
    const category = categoryInfo(note)

    const hasImage = Boolean(
        note.imageAdded &&
        note.imageUrl
    )

    return (
        <article
            className={`search-result-card color-${note.color || 'default'}`}
        >
            <div className="search-result-main">

                <div className="search-result-top">

                    <span
                        className={`search-category-badge ${category.className}`}
                    >
                        {category.label}
                    </span>

                    {note.pinned && !note.isDeleted && (
                        <span className="search-secondary-badge">
                            Sabit
                        </span>
                    )}

                    {note.archived && !note.isDeleted && (
                        <span className="search-secondary-badge">
                            Arşiv
                        </span>
                    )}

                    {note.isDeleted && (
                        <span className="search-secondary-badge">
                            Çöp Kutusu
                        </span>
                    )}

                    {note.createdAt && (
                        <span className="search-result-date">
                            {formatDate(note.createdAt)}
                        </span>
                    )}

                </div>

                {note.title && (
                    <h3>
                        {note.title}
                    </h3>
                )}

                {hasImage && (
                    <img
                        className="search-result-image"
                        src={note.imageUrl}
                        alt={note.title || 'Not görseli'}
                    />
                )}

                {note.content && (
                    <p>
                        {note.content}
                    </p>
                )}

                {!note.title &&
                    !note.content &&
                    !hasImage && (
                        <p className="search-result-empty">
                            Boş Not
                        </p>
                    )}

            </div>
        </article>
    )
}

export default function SearchPanel({
    notes,
    query,
    filter,
    onFilterChange,
}) {
    const filteredNotes = useMemo(() => {

        const normalizedQuery =
            query
                .trim()
                .toLocaleLowerCase('tr-TR')

        return notes
            .filter((note) =>
                matchesFilter(note, filter)
            )
            .filter((note) => {

                if (!normalizedQuery) {
                    return true
                }

                const searchableText = [
                    note.title,
                    note.content,
                    note.color,

                    note.checklist
                        ? 'liste'
                        : '',

                    note.pinned
                        ? 'sabitlenmiş sabit'
                        : '',

                    note.archived
                        ? 'arşivlenmiş arşiv'
                        : '',

                    note.isDeleted
                        ? 'silinmiş çöp kutusu'
                        : '',
                ]
                    .filter(Boolean)
                    .join(' ')
                    .toLocaleLowerCase('tr-TR')

                return searchableText.includes(
                    normalizedQuery
                )
            })
            .sort((a, b) => {

                const aTime =
                    new Date(
                        a.updatedAt ||
                        a.createdAt ||
                        0
                    ).getTime()

                const bTime =
                    new Date(
                        b.updatedAt ||
                        b.createdAt ||
                        0
                    ).getTime()

                return bTime - aTime
            })

    }, [notes, query, filter])

    const filters = [
        {
            key: 'all',
            label: 'Tümü',
        },
        {
            key: 'normal',
            label: 'Normal Not',
        },
        {
            key: 'pinned',
            label: 'Sabitlenmiş',
        },
        {
            key: 'archived',
            label: 'Arşivlenmiş',
        },
        {
            key: 'deleted',
            label: 'Silinmiş',
        },
    ]

    return (
        <section
            className="search-panel"
            aria-label="Notlarda arama"
        >

            <div className="search-panel-header">

                <div>

                    <h2>
                        {query.trim()
                            ? 'Arama sonuçları'
                            : 'Tüm kayıtlar'}
                    </h2>

                    <span>
                        {filteredNotes.length} kayıt
                    </span>

                </div>

                <div className="search-filter-row">

                    {filters.map((item) => (

                        <button
                            key={item.key}
                            type="button"
                            className={
                                `search-filter${filter === item.key
                                    ? ' active'
                                    : ''
                                }`
                            }
                            onClick={() =>
                                onFilterChange(item.key)
                            }
                        >
                            {item.label}
                        </button>

                    ))}

                </div>

            </div>

            {filteredNotes.length === 0 ? (

                <div className="search-empty">

                    <div className="search-empty-icon">
                        ⌕
                    </div>

                    <strong>
                        Kayıt bulunamadı
                    </strong>

                    <span>
                        Başka bir kelime veya kategori deneyin.
                    </span>

                </div>

            ) : (

                <div className="search-results-grid">

                    {filteredNotes.map((note) => (

                        <NoteSearchResult
                            key={note.id}
                            note={note}
                        />

                    ))}

                </div>

            )}

        </section>
    )
}