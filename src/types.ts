export interface Event {
  id: string;
  title: string;
  description: string;
  date: string;
  location?: string;
  category?: string;
}

export interface PartialEvent {
  id?: string;
  title: string;
  description: string;
  date: string;
  location?: string;
  category?: string;
}

export interface CalendarDate {
  day: number;
  date: Date;
  events: Event[];
}
