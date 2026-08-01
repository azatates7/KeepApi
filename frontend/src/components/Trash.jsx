import { useEffect, useState } from "react";
import { fetchTrash, restoreNote, deleteForever } from "../api";
import "./Trash.css";

export default function Trash({ onBack }) {

    const [notes, setNotes] = useState([]);

    async function load() {
        const result = await fetchTrash();
        setNotes(result);
    }

    useEffect(() => {
        load();
    }, []);

    async function restore(id) {
        await restoreNote(id);
        load();
    }

    async function remove(id) {

        if (!window.confirm("Not kalıcı olarak silinsin mi?"))
            return;

        await deleteForever(id);

        load();
    }

    return (
        <div className="trash-page">

            <header className="trash-header">

                <button
                    className="back-button"
                    onClick={onBack}
                >
                    ← Notlara Dön
                </button>

                <div className="trash-title">
                    <h1>🗑 Çöp Kutusu</h1>
                    <span>{notes.length} not</span>
                </div>

            </header>

            {
                notes.length === 0 && (
                    <div className="empty-trash">
                        Çöp kutusu boş.
                    </div>
                )
            }

            <div className="trash-list">

                {
                    notes.map(note => (

                        <article
                            key={note.id}
                            className="trash-card"
                        >

                            <div className="trash-content">

                                <h3>{note.title || "Başlıksız Not"}</h3>

                                <p>{note.content}</p>

                            </div>

                            <footer className="trash-footer">

                                <button
                                    className="restore-btn"
                                    onClick={() => restore(note.id)}
                                >
                                    ↩ Geri Yükle
                                </button>

                                <button
                                    className="delete-btn"
                                    onClick={() => remove(note.id)}
                                >
                                    🗑 Kalıcı Sil
                                </button>

                            </footer>

                        </article>

                    ))
                }

            </div>

        </div>
    );
}