import { describe, it, expect } from 'vitest';
import type { User, Company, Service, Reservation, AuthResponse, UserRole } from '../index';

describe('Types', () => {
  describe('User', () => {
    it('should have correct shape', () => {
      const user: User = {
        id: '123e4567-e89b-12d3-a456-426614174000',
        email: 'test@example.com',
        role: 'Customer' as UserRole,
        companyId: '123e4567-e89b-12d3-a456-426614174001',
      };
      
      expect(user.id).toBeDefined();
      expect(user.email).toBeDefined();
      expect(user.role).toBe('Customer');
    });
  });

  describe('Company', () => {
    it('should have correct shape', () => {
      const company: Company = {
        id: '123e4567-e89b-12d3-a456-426614174001',
        name: 'Test Company',
        description: 'A test company',
        timeZone: 'America/New_York',
        isActive: true,
        ownerId: '123e4567-e89b-12d3-a456-426614174000',
      };
      
      expect(company.name).toBeDefined();
      expect(company.timeZone).toBeDefined();
      expect(company.isActive).toBe(true);
    });
  });

  describe('Service', () => {
    it('should have correct shape', () => {
      const service: Service = {
        id: '123e4567-e89b-12d3-a456-426614174002',
        name: 'Haircut',
        description: 'A standard haircut',
        durationMinutes: 30,
        price: 25.00,
        isActive: true,
        companyId: '123e4567-e89b-12d3-a456-426614174001',
      };
      
      expect(service.name).toBeDefined();
      expect(service.durationMinutes).toBe(30);
      expect(service.price).toBe(25.00);
    });
  });

  describe('Reservation', () => {
    it('should have correct shape', () => {
      const reservation: Reservation = {
        id: '123e4567-e89b-12d3-a456-426614174003',
        serviceId: '123e4567-e89b-12d3-a456-426614174002',
        customerId: '123e4567-e89b-12d3-a456-426614174000',
        companyId: '123e4567-e89b-12d3-a456-426614174001',
        serviceName: 'Haircut',
        customerEmail: 'customer@example.com',
        startTime: '2024-01-15T10:00:00Z',
        endTime: '2024-01-15T10:30:00Z',
        status: 'Pending',
        createdAt: '2024-01-10T08:00:00Z',
      };
      
      expect(reservation.serviceName).toBeDefined();
      expect(reservation.status).toBe('Pending');
    });
  });

  describe('AuthResponse', () => {
    it('should have correct shape', () => {
      const authResponse: AuthResponse = {
        token: 'jwt-token-here',
        userId: '123e4567-e89b-12d3-a456-426614174000',
        email: 'user@example.com',
        role: 'Owner',
      };
      
      expect(authResponse.token).toBeDefined();
      expect(authResponse.userId).toBeDefined();
      expect(authResponse.role).toBe('Owner');
    });
  });
});
