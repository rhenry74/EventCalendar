const dateOnlyPattern = /^(\d{4}-\d{2}-\d{2})$/;
const utcMidnightPattern = /^(\d{4}-\d{2}-\d{2})[T ]00:00:00(?:\.\d+)?(?:Z|\+00:00)$/;

export const dateOnlyPart = (value: string) =>
  dateOnlyPattern.exec(value)?.[1] ?? utcMidnightPattern.exec(value)?.[1] ?? null;

export const isTimedEventValue = (value: string) =>
  dateOnlyPart(value) === null && /[T ]\d{2}:\d{2}/.test(value);

export const parseEventDate = (value: string) => {
  const dateOnly = dateOnlyPart(value);
  if (dateOnly) {
    const [year, month, day] = dateOnly.split('-').map(Number);
    return new Date(year, month - 1, day);
  }
  return new Date(value);
};
