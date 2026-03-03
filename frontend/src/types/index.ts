export type UserRole = "Owner" | "Customer";

export interface User {
  id: string;
  email: string;
  role: UserRole;
  companyId?: string;
}

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  role: UserRole;
}

export interface RegisterRequest {
  email: string;
  password: string;
  role: UserRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface Company {
  id: string;
  name: string;
  description?: string;
  timeZone: string;
  isActive: boolean;
  ownerId: string;
}

export interface Service {
  id: string;
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  isActive: boolean;
  companyId: string;
}

export interface Reservation {
  id: string;
  serviceId: string;
  customerId: string;
  companyId: string;
  serviceName: string;
  customerEmail: string;
  startTime: string;
  endTime: string;
  status: ReservationStatus;
  createdAt: string;
}

export type ReservationStatus = "Pending" | "Confirmed" | "Cancelled" | "Completed";

export interface Availability {
  id: string;
  companyId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isActive: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
