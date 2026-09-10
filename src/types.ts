export interface Event {
  id: string;
  title: string;
  description: string;
  date: string;
  location?: string;
  category?: string;
  isPublic?: boolean;
  ownerName?: string;
  isOwner?: boolean;
}

export interface PartialEvent {
  id?: string;
  title: string;
  description: string;
  date: string;
  location?: string;
  category?: string;
  isPublic: boolean;
}

export interface CalendarDate {
  day: number;
  date: Date;
  events: Event[];
}
