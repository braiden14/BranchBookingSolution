const locale = 'en-ZA'
const noon = (value: string) => new Date(`${value}T12:00:00`)

export const localDate = (date: Date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`

export const getToday = () => localDate(new Date())

export const formatDate = (value: string) =>
  new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))

export const formatDateOnly = (value: string) =>
  new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(value))

export const formatTime = (value: string) =>
  new Intl.DateTimeFormat(locale, { timeStyle: 'short' }).format(new Date(value))

export const formatDay = (value: string) =>
  new Intl.DateTimeFormat(locale, { weekday: 'short' }).format(noon(value))

export const formatDayNumber = (value: string) =>
  new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' }).format(noon(value))

export const addDays = (value: string, days: number) => {
  const date = noon(value)
  date.setDate(date.getDate() + days)
  return localDate(date)
}

export const startOfWeek = (value: string) => {
  const date = noon(value)
  date.setDate(date.getDate() - ((date.getDay() + 6) % 7))
  return localDate(date)
}

export const durationMinutes = (startAt: string, endAt: string) =>
  Math.round((new Date(endAt).getTime() - new Date(startAt).getTime()) / 60000)
