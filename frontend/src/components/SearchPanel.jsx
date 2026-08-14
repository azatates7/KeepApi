import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'

function categoryInfo(note, t) {
    if (note.isDeleted) {
        return {
            label: t('search.categoryDeleted'),
            className: 'deleted',
        }
    }

    if (note.pinned) {
        return {
            label: t('search.categoryPinned'),
            className: 'pinned',
        }
    }

    if (note.archived) {
        return {
            label: t('search.categoryArchived'),
            className: 'archived',
        }
    }

    return {
        label: t('search.categoryNormal'),
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

function formatDate(value, locale) {
    if (!value) {
        return ''
    }

    const date = new Date(value)

    if (Number.isNaN(date.getTime())) {
        return ''
    }

    return new Intl.DateTimeFormat(locale, {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    }).format(date)
}

function NoteSearchResult({ note, t, locale }) {
    const category = categoryInfo(note, t)

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
                            {t('search.badgePinned')}
                        </span>
                    )}

                    {note.archived && !note.isDeleted && (
                        <span className="search-secondary-badge">
                            {t('search.badgeArchived')}
                        </span>
                    )}

                    {note.isDeleted && (
                        <span className="search-secondary-badge">
                            {t('search.badgeTrash')}
                        </span>
                    )}

                    {note.createdAt && (
                        <span className="search-result-date">
                            {formatDate(note.createdAt, locale)}
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
                        alt={note.title || t('search.imageAlt')}
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
                            {t('search.emptyNote')}
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
    const { t, i18n } = useTranslation()
    const locale = i18n.language === 'en' ? 'en-US' : 'tr-TR'

    const filteredNotes = useMemo(() => {

        const normalizedQuery =
            query
                .trim()
                .toLocaleLowerCase(locale)

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
                        ? t('search.keywordChecklist')
                        : '',

                    note.pinned
                        ? t('search.keywordPinned')
                        : '',

                    note.archived
                        ? t('search.keywordArchived')
                        : '',

                    note.isDeleted
                        ? t('search.keywordDeleted')
                        : '',
                ]
                    .filter(Boolean)
                    .join(' ')
                    .toLocaleLowerCase(locale)

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

    }, [notes, query, filter, locale, t])

    const filters = [
        {
            key: 'all',
            label: t('search.filterAll'),
        },
        {
            key: 'normal',
            label: t('search.filterNormal'),
        },
        {
            key: 'pinned',
            label: t('search.filterPinned'),
        },
        {
            key: 'archived',
            label: t('search.filterArchived'),
        },
        {
            key: 'deleted',
            label: t('search.filterDeleted'),
        },
    ]

    return (
        <section
            className="search-panel"
            aria-label={t('search.ariaLabel')}
        >

            <div className="search-panel-header">

                <div>

                    <h2>
                        {query.trim()
                            ? t('search.resultsTitle')
                            : t('search.allRecordsTitle')}
                    </h2>

                    <span>
                        {t('search.recordCount', { count: filteredNotes.length })}
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
                        {t('search.notFound')}
                    </strong>

                    <span>
                        {t('search.tryAnother')}
                    </span>

                </div>

            ) : (

                <div className="search-results-grid">

                    {filteredNotes.map((note) => (

                        <NoteSearchResult
                            key={note.id}
                            note={note}
                            t={t}
                            locale={locale}
                        />

                    ))}

                </div>

            )}

        </section>
    )
}