export interface User {
    id: string;
    subject: string;
    displayName: string;
    email: string;
    picture?: string | null;
}

export interface Event {
    id: string;
    title: string;
    description: string;
    date: string;
    location?: string;
    category?: string;
}

export interface PartialEvent extends Omit<Event, 'id'> {
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