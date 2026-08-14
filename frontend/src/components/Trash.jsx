import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { fetchTrash, restoreNote, deleteForever } from "../api";
import TrashCard from "./TrashCard.jsx";
import "./Trash.css";

export default function Trash() {
    const { t } = useTranslation();
    const [notes, setNotes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    async function load() {
        try {
            setLoading(true);

            const result = await fetchTrash();

            setNotes(result);
            setError(null);
        } catch (err) {
            setError(err.message || t('trash.loadFailed'));
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        load();
    }, []);

    async function restore(id) {
        try {
            await restoreNote(id);
            await load();
        } catch (err) {
            setError(err.message || t('trash.restoreFailed'));
        }
    }

    async function remove(id) {
        if (!window.confirm(t('trash.confirmDelete'))) {
            return;
        }

        try {
            await deleteForever(id);
            await load();
        } catch (err) {
            setError(err.message || t('trash.deleteFailed'));
        }
    }

    return (
        <main className="trash-page">

            <div className="trash-toolbar">
                <div className="trash-heading">
                    <h1>{t('trash.title')}</h1>

                    {!loading && (
                        <span>
                            {t('trash.noteCount', { count: notes.length })}
                        </span>
                    )}
                </div>
            </div>

            {loading && (
                <p className="status">
                    {t('trash.loading')}
                </p>
            )}

            {error && (
                <p className="status error">
                    {error}
                </p>
            )}

            {!loading && !error && notes.length === 0 && (
                <p className="empty-state">
                    {t('trash.empty')}
                </p>
            )}

            {!loading && notes.length > 0 && (
                <section className="note-section">

                    <div className="note-grid">

                        {notes.map((note) => (
                            <TrashCard
                                key={note.id}
                                note={note}
                                onRestore={restore}
                                onDelete={remove}
                            />
                        ))}

                    </div>

                </section>
            )}

        </main>
    );
}