import { useTranslation } from 'react-i18next'

export default function TrashCard({
    note,
    onRestore,
    onDelete
}) {
    const { t } = useTranslation()
    return (
        <article
            className={`note-card trash-card ${note.color ? `color-${note.color}` : ""
                }`}
        >

            <div className="trash-card-body">

                <h3 className="note-title">
                    {note.title || t('trash.untitledNote')}
                </h3>

                {note.content && (
                    <p className="note-content">
                        {note.content}
                    </p>
                )}

                {note.image && (
                    <img
                        src={note.image}
                        alt=""
                        className="note-image"
                    />
                )}

            </div>

            <footer className="trash-card-footer">

                <button
                    type="button"
                    className="trash-action restore"
                    onClick={() => onRestore(note.id)}
                >
                    {t('trash.restore')}
                </button>

                <button
                    type="button"
                    className="trash-action permanent"
                    onClick={() => onDelete(note.id)}
                >
                    {t('trash.deleteForever')}
                </button>

            </footer>

        </article>
    );
}