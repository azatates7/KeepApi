import { useActionState, useRef, useState } from 'react'
import ColorDots from './ColorDots.jsx'

const initialState = { error: null }

export default function Composer({ onCreate }) {
  const [expanded, setExpanded] = useState(false)
  const [color, setColor] = useState('default')
  const formRef = useRef(null)

  const [state, submitAction, isPending] = useActionState(async (_prevState, formData) => {
    const title = formData.get('title')?.toString().trim() ?? ''
    const content = formData.get('content')?.toString().trim() ?? ''

    if (!title && !content) {
      setExpanded(false)
      return initialState
    }

    try {
      await onCreate({ title, content, color, pinned: false, archived: false })
      formRef.current?.reset()
      setExpanded(false)
      setColor('default')
      return initialState
    } catch (err) {
      return { error: err.message }
    }
  }, initialState)

  return (
    <form ref={formRef} action={submitAction} className={`composer${expanded ? ' expanded' : ''}`}>
      {expanded && (
        <input name="title" className="composer-title" placeholder="Başlık" autoFocus />
      )}
      <textarea
        name="content"
        className="composer-content"
        placeholder="Bir Not Al…"
        rows={expanded ? 3 : 1}
        onFocus={() => setExpanded(true)}
      />
      {expanded && (
        <div className="composer-footer">
          <ColorDots value={color} onChange={setColor} />
          <button type="submit" className="btn-primary" disabled={isPending}>
            {isPending ? 'Ekleniyor…' : 'Ekle'}
          </button>
        </div>
      )}
      {state.error && <p className="status error">{state.error}</p>}
    </form>
  )
}
