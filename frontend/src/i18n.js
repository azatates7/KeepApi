import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import tr from './locales/tr/translation.json'
import en from './locales/en/translation.json'
import { getToken, updateLanguage } from './api.js'

export const LANGUAGE_STORAGE_KEY = 'keepapi_lang'
export const SUPPORTED_LANGUAGES = ['tr', 'en']
export const DEFAULT_LANGUAGE = 'tr'

const storedLanguage = localStorage.getItem(LANGUAGE_STORAGE_KEY)
const initialLanguage = SUPPORTED_LANGUAGES.includes(storedLanguage)
    ? storedLanguage
    : DEFAULT_LANGUAGE

i18n
    .use(initReactI18next)
    .init({
        resources: {
            tr: { translation: tr },
            en: { translation: en },
        },
        lng: initialLanguage,
        fallbackLng: DEFAULT_LANGUAGE,
        interpolation: {
            escapeValue: false,
        },
    })

// Dili değiştirir, localStorage'a yazar ve oturum açıksa backend'e senkronlar
// (Günlük Özet job'ı kullanıcının PreferredLanguage alanını okur).
// LanguageSwitcher ve UserMenu bu tek fonksiyonu paylaşır.
export async function applyLanguage(language) {
    if (!SUPPORTED_LANGUAGES.includes(language)) return

    await i18n.changeLanguage(language)
    localStorage.setItem(LANGUAGE_STORAGE_KEY, language)

    if (getToken()) {
        try {
            await updateLanguage(language)
        } catch {
            // Sessizce geç: arayüz zaten yeni dile geçti, backend senkronu
            // bir sonraki başarılı çağrıda tamamlanır.
        }
    }
}

export default i18n