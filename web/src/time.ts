// The lab runs on Berlin time. The API speaks UTC; everything shown to users is converted here, whatever the browser's zone.
export const LAB_TIME_ZONE = 'Europe/Berlin'

const timeFormat = new Intl.DateTimeFormat('en-GB', { timeZone: LAB_TIME_ZONE, hour: '2-digit', minute: '2-digit' })
const dateTimeFormat = new Intl.DateTimeFormat('en-GB', {
  timeZone: LAB_TIME_ZONE,
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
})
const partsFormat = new Intl.DateTimeFormat('en-CA', {
  timeZone: LAB_TIME_ZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
  hourCycle: 'h23',
})

export const formatTime = (iso: string) => timeFormat.format(new Date(iso))
export const formatDateTime = (iso: string) => dateTimeFormat.format(new Date(iso))

/** Wall-clock parts of an instant in Berlin. */
function berlinParts(date: Date) {
  const p = Object.fromEntries(partsFormat.formatToParts(date).map((x) => [x.type, x.value]))
  return { year: +p.year, month: +p.month, day: +p.day, hour: +p.hour, minute: +p.minute, second: +p.second }
}

/** Today's date in Berlin as yyyy-mm-dd. */
export function labToday(): string {
  const p = berlinParts(new Date())
  return `${p.year}-${String(p.month).padStart(2, '0')}-${String(p.day).padStart(2, '0')}`
}

/** Converts a Berlin wall-clock date + time ("2026-10-12", "14:30") to a UTC ISO string (DST-aware). */
export function labLocalToUtcIso(date: string, time: string): string {
  const [y, mo, d] = date.split('-').map(Number)
  const [h, mi] = time.split(':').map(Number)
  const wanted = Date.UTC(y, mo - 1, d, h, mi)
  // Guess the offset, then correct once (handles the guess landing on the other side of a DST switch).
  let utc = wanted
  for (let i = 0; i < 2; i++) {
    const p = berlinParts(new Date(utc))
    const shown = Date.UTC(p.year, p.month - 1, p.day, p.hour, p.minute)
    utc += wanted - shown
  }
  return new Date(utc).toISOString()
}

export function minutesUntil(iso: string): number {
  return Math.max(0, Math.round((new Date(iso).getTime() - Date.now()) / 60000))
}
