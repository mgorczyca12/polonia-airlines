const DATE_KEY = /^(\d{4})-(\d{2})-(\d{2})$/
const MILLISECONDS_PER_DAY = 24 * 60 * 60 * 1000

const timeFormatter = new Intl.DateTimeFormat('en-GB', {
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
  timeZone: 'UTC',
})
const dayMonthFormatter = new Intl.DateTimeFormat('en-GB', {
  day: 'numeric',
  month: 'short',
  timeZone: 'UTC',
})
const weekdayFormatter = new Intl.DateTimeFormat('en-GB', {
  weekday: 'short',
  timeZone: 'UTC',
})
const shortDateFormatter = new Intl.DateTimeFormat('en-GB', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  timeZone: 'UTC',
})
const longDateFormatter = new Intl.DateTimeFormat('en-GB', {
  day: 'numeric',
  month: 'long',
  year: 'numeric',
  timeZone: 'UTC',
})
const utcDateTimeFormatter = new Intl.DateTimeFormat('en-US', {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: 'UTC',
})

function createUtcDate(year: number, month: number, day: number): Date | null {
  const date = new Date(Date.UTC(year, month - 1, day))
  const isSameCalendarDate =
    date.getUTCFullYear() === year &&
    date.getUTCMonth() === month - 1 &&
    date.getUTCDate() === day

  return isSameCalendarDate ? date : null
}

/** Parses a `YYYY-MM-DD` calendar date as the start of that UTC day. */
export function parseDateKey(value: string): Date | null {
  const match = DATE_KEY.exec(value)

  if (!match) {
    return null
  }

  const [, year, month, day] = match.map(Number)
  return createUtcDate(year, month, day)
}

export function isDateKey(value: unknown): value is string {
  return typeof value === 'string' && parseDateKey(value) !== null
}

export function toDateKey(date: Date): string {
  return date.toISOString().slice(0, 10)
}

export function addDaysToDateKey(dateKey: string, days: number): string {
  const date = parseDateKey(dateKey)

  if (!date) {
    return dateKey
  }

  return toDateKey(new Date(date.getTime() + days * MILLISECONDS_PER_DAY))
}

export function calendarDayOffset(from: Date, to: Date): number {
  const fromDay = Date.UTC(
    from.getUTCFullYear(),
    from.getUTCMonth(),
    from.getUTCDate(),
  )
  const toDay = Date.UTC(to.getUTCFullYear(), to.getUTCMonth(), to.getUTCDate())

  return Math.round((toDay - fromDay) / MILLISECONDS_PER_DAY)
}

export function formatTime(date: Date): string {
  return timeFormatter.format(date)
}

function formatDateKey(dateKey: string, formatter: Intl.DateTimeFormat): string {
  const date = parseDateKey(dateKey)
  return date ? formatter.format(date) : dateKey
}

export function formatDayMonth(dateKey: string): string {
  return formatDateKey(dateKey, dayMonthFormatter)
}

export function formatWeekday(dateKey: string): string {
  return formatDateKey(dateKey, weekdayFormatter)
}

export function formatShortDate(dateKey: string): string {
  return formatDateKey(dateKey, shortDateFormatter)
}

export function formatLongDate(dateKey: string): string {
  return formatDateKey(dateKey, longDateFormatter)
}

export function formatDateRange(startKey: string, endKey?: string): string {
  const start = parseDateKey(startKey)
  const end = endKey ? parseDateKey(endKey) : null

  if (!start || !end) {
    return formatShortDate(startKey)
  }

  if (start.getUTCFullYear() !== end.getUTCFullYear()) {
    return `${shortDateFormatter.format(start)} – ${shortDateFormatter.format(end)}`
  }

  if (start.getUTCMonth() !== end.getUTCMonth()) {
    return `${dayMonthFormatter.format(start)} – ${shortDateFormatter.format(end)}`
  }

  return `${start.getUTCDate()} – ${shortDateFormatter.format(end)}`
}

export function formatDuration(totalMinutes: number): string {
  const hours = Math.floor(totalMinutes / 60)
  const minutes = totalMinutes % 60

  return `${hours}h ${String(minutes).padStart(2, '0')}m`
}

export function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(
    amount,
  )
}

/** Formats an ISO timestamp as a UTC date and time with an explicit "UTC" label. */
export function formatUtcDateTime(timestamp: string): string {
  return `${utcDateTimeFormatter.format(new Date(timestamp))} UTC`
}
