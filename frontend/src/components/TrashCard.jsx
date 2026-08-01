export default function TrashCard({
                                      note,
                                      onRestore,
                                      onDelete
                                  }) {

    return (

        <article className="trash-card">

            <div className="trash-body">

                <h3>{note.title}</h3>

                <p>{note.content}</p>

            </div>

            <footer className="trash-footer">

                <button
                    className="restore-btn"
                    onClick={() => onRestore(note.id)}
                >
                    ↩ Geri Yükle
                </button>

                <button
                    className="delete-btn"
                    onClick={() => onDelete(note.id)}
                >
                    🗑 Kalıcı Sil
                </button>

            </footer>

        </article>

    );
}