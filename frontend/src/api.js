const BASE_URL = 'http://localhost:5080/api/notes'

export async function fetchNotes() {
  const res = await fetch(BASE_URL)
  if (!res.ok) throw new Error('Notlar Yüklenemedi')
  return res.json()
}

export async function createNote(note) {
  const res = await fetch(BASE_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(note),
  })
  if (!res.ok) throw new Error('Not Oluşturulamadı')
  return res.json()
}

export async function updateNote(id, note) {
  const res = await fetch(`${BASE_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(note),
  })
  if (!res.ok) throw new Error('Not Güncellenemedi')
  return res.json()
}

export async function deleteNote(id) {
  const res = await fetch(`${BASE_URL}/${id}`, { method: 'DELETE' })
  if (!res.ok && res.status !== 204) throw new Error('Not Silinemedi')
}

export async function fetchTrash() {
  const res = await fetch(`${BASE_URL}/trash`);

  if (!res.ok)
    throw new Error("Not Çöp Kutusu Yüklenemedi");

  return res.json();
}

export async function restoreNote(id) {
  const res = await fetch(`${BASE_URL}/${id}/restore`, {
    method: "PUT"
  });

  if (!res.ok)
    throw new Error("Not Geri Yüklenemedi");
}

export async function deleteForever(id) {
  const res = await fetch(`${BASE_URL}/${id}/permanent`, {
    method: "DELETE"
  });

  if (!res.ok)
    throw new Error("Not Kalıcı Silinemedi");
}
