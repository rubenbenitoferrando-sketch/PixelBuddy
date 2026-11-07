import EventKit
import Foundation

/// Lee el Calendario de macOS (iCloud, Google… las cuentas de Ajustes > Cuentas de internet).
final class CalendarService {
    private(set) var store = EKEventStore()

    var status: EKAuthorizationStatus { EKEventStore.authorizationStatus(for: .event) }
    var hasAccess: Bool { status == .fullAccess }

    func requestAccess(_ done: @escaping (Bool) -> Void) {
        Task { @MainActor in
            let granted = (try? await self.store.requestFullAccessToEvents()) ?? false
            if granted { self.store = EKEventStore() }
            done(granted)
        }
    }

    // MARK: - Calendarios elegidos

    private var excluded: Set<String> {
        get { Set(UserDefaults.standard.stringArray(forKey: Config.keyExcludedCalendars) ?? []) }
        set { UserDefaults.standard.set(Array(newValue), forKey: Config.keyExcludedCalendars) }
    }

    func allCalendars() -> [EKCalendar] {
        guard hasAccess else { return [] }
        return store.calendars(for: .event).sorted {
            ($0.source.title, $0.title) < ($1.source.title, $1.title)
        }
    }

    func isEnabled(_ c: EKCalendar) -> Bool { !excluded.contains(c.calendarIdentifier) }

    func toggle(_ id: String) {
        var e = excluded
        if e.contains(id) { e.remove(id) } else { e.insert(id) }
        excluded = e
    }

    // MARK: - Eventos

    /// Evento en curso (el que antes termina) y el siguiente, solo de hoy.
    func currentAndNext(now: Date = Date()) -> (EKEvent?, EKEvent?) {
        let cals = allCalendars().filter(isEnabled)
        guard !cals.isEmpty else { return (nil, nil) }
        let endOfToday = Calendar.current.date(
            byAdding: .day, value: 1, to: Calendar.current.startOfDay(for: now)) ?? now
        let pred = store.predicateForEvents(withStart: now.addingTimeInterval(-86_400),
                                            end: endOfToday,
                                            calendars: cals)
        let events = store.events(matching: pred).filter { e in
            guard !e.isAllDay, e.status != .canceled else { return false }
            let me = e.attendees?.first { $0.isCurrentUser }
            return me?.participantStatus != .declined
        }
        let current = events.filter { $0.startDate <= now && $0.endDate > now }
            .min { $0.endDate < $1.endDate }
        let next = events.filter { $0.startDate > now && $0.startDate < endOfToday }
            .min { $0.startDate < $1.startDate }
        return (current, next)
    }
}
