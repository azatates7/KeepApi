import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'

function fireNotification(note, t) {

    if (typeof Notification === 'undefined')
        return;

    if (Notification.permission !== 'granted')
        return;

    new Notification(note.title || t('note.reminderTitle'), {
        body: note.content || t('note.reminderBody'),
        icon: '/favicon.ico'
    });
}

export function useReminders(notes) {

    const { t } = useTranslation();
    const notifiedRef = useRef(new Set());

    useEffect(() => {

        if (
            typeof Notification !== 'undefined' &&
            Notification.permission === 'default'
        ) {
            Notification.requestPermission();
        }

    }, []);

    useEffect(() => {

        const checkReminders = () => {

            const now = Date.now();

            notes.forEach(note => {

                if (!note.reminderAt || note.archived)
                    return;

                if (notifiedRef.current.has(note.id))
                    return;

                const reminderTime = new Date(note.reminderAt).getTime();

                if (reminderTime <= now) {

                    notifiedRef.current.add(note.id);

                    fireNotification(note, t);
                }

            });

        };

        checkReminders();

        const interval = setInterval(checkReminders, 20000);

        return () => clearInterval(interval);

    }, [notes, t]);
}