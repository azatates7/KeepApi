// Google/Microsoft/GitHub "Sign in with..." butonları için yapılandırma.
//
// Her sağlayıcının Client Id'sini kendi geliştirici konsolunda oluşturduğunuz
// OAuth uygulamasından alıp bir .env dosyasına yazmalısınız (bkz. .env.example).
// Client Secret asla buraya/frontend'e YAZILMAZ; secret sadece backend'de
// appsettings.json > ExternalProviders altında tutulur.
//
// Her sağlayıcının konsolunda "Authorized redirect URI" olarak
// getRedirectUri() ile üretilen adresi (örn. http://localhost:5173/oauth/callback)
// birebir kaydetmeniz gerekir.

export function getRedirectUri() {
    return `${window.location.origin}/oauth/callback`
}

export const OAUTH_PROVIDERS = {
    google: {
        label: 'Google',
        clientId: import.meta.env.VITE_GOOGLE_CLIENT_ID || '',
        authorizeUrl: 'https://accounts.google.com/o/oauth2/v2/auth',
        scope: 'openid email profile',
        extraParams: { access_type: 'offline', prompt: 'select_account' },
    },
    microsoft: {
        label: 'Microsoft',
        clientId: import.meta.env.VITE_MICROSOFT_CLIENT_ID || '',
        authorizeUrl: 'https://login.microsoftonline.com/common/oauth2/v2.0/authorize',
        scope: 'openid email profile User.Read',
        extraParams: { response_mode: 'query', prompt: 'select_account' },
    },
    github: {
        label: 'GitHub',
        clientId: import.meta.env.VITE_GITHUB_CLIENT_ID || '',
        authorizeUrl: 'https://github.com/login/oauth/authorize',
        scope: 'read:user user:email',
        extraParams: {},
    },
}

const STATE_STORAGE_KEY = 'keep_todo_oauth_state'

function randomNonce() {
    const bytes = new Uint8Array(16)
    crypto.getRandomValues(bytes)
    return Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('')
}

// Kullanıcıyı sağlayıcının yetkilendirme (authorize) sayfasına yönlendirir.
// state = "<provider>:<nonce>" olarak kodlanır; nonce sessionStorage'a yazılır ve
// dönüşte (OAuthCallBack.jsx) CSRF koruması için karşılaştırılır.
export function startOAuthLogin(provider) {
    const config = OAUTH_PROVIDERS[provider]
    if (!config) {
        throw new Error(`Bilinmeyen giriş sağlayıcısı: ${provider}`)
    }

    if (!config.clientId) {
        alert(
            `${config.label} ile giriş için Client Id tanımlı değil. ` +
            `frontend/.env dosyasına VITE_${provider.toUpperCase()}_CLIENT_ID değerini ekleyin.`
        )
        return
    }

    const nonce = randomNonce()
    sessionStorage.setItem(STATE_STORAGE_KEY, `${provider}:${nonce}`)

    const params = new URLSearchParams({
        client_id: config.clientId,
        redirect_uri: getRedirectUri(),
        response_type: 'code',
        scope: config.scope,
        state: `${provider}:${nonce}`,
        ...config.extraParams,
    })

    window.location.href = `${config.authorizeUrl}?${params.toString()}`
}

// OAuthCallBack.jsx tarafından çağrılır: URL'deki state ile sessionStorage'daki
// beklenen state'i karşılaştırır, eşleşirse provider adını döner.
export function consumeOAuthState(stateFromUrl) {
    const expected = sessionStorage.getItem(STATE_STORAGE_KEY)
    sessionStorage.removeItem(STATE_STORAGE_KEY)

    if (!stateFromUrl || !expected || stateFromUrl !== expected) {
        return null
    }

    const [provider] = stateFromUrl.split(':')
    return OAUTH_PROVIDERS[provider] ? provider : null
}