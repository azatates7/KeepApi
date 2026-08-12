import { useEffect, useState } from "react";
import { fetchTrash, restoreNote, deleteForever } from "../api";
import TrashCard from "./TrashCard.jsx";
import "./Trash.css";

export default function Trash() {
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
            setError(err.message || "Çöp kutusu yüklenemedi.");
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
            setError(err.message || "Not geri yüklenemedi.");
        }
    }

    async function remove(id) {
        if (!window.confirm("Not kalıcı olarak silinsin mi?")) {
            return;
        }

        try {
            await deleteForever(id);
            await load();
        } catch (err) {
            setError(err.message || "Not kalıcı olarak silinemedi.");
        }
    }

    return (
        <main className="trash-page">

            <div className="trash-toolbar">
                <div className="trash-heading">
                    <h1>Çöp Kutusu</h1>

                    {!loading && (
                        <span>
                            {notes.length} not
                        </span>
                    )}
                </div>
            </div>

            {loading && (
                <p className="status">
                    Yükleniyor…
                </p>
            )}

            {error && (
                <p className="status error">
                    {error}
                </p>
            )}

            {!loading && !error && notes.length === 0 && (
                <p className="empty-state">
                    Çöp kutusu boş.
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