export interface Event {
  id: string;
  title: string;
  description: string;
  journal?: string;
  date: string;
  endDate?: string;
  location?: string;
  category?: string;
  isPublic?: boolean;
  ownerName?: string;
  isOwner?: boolean;
}

export interface Category {
  name: string;
  icon: string;
  type: string;
  primaryColor: string;
  secondaryColor: string;
}

export interface PartialEvent {
  id?: string;
  title: string;
  description: string;
  journal?: string;
  date: string;
  endDate?: string;
  location?: string;
  category?: string;
  isPublic: boolean;
}

export interface CalendarDate {
  day: number;
  date: Date;
  events: Event[];
}
