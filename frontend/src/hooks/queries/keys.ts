/**
 * Query keys, one factory for every resource. Data that belongs to the signed-in user carries the user id, so
 * a cache entry can never be shown to somebody else after a sign-out/sign-in on the same tab. Invalidate by a
 * prefix (`all`) to refresh every variant of a resource.
 */
export const queryKeys = {
  reservations: {
    all: (userId: string) => ["reservations", userId] as const,
    list: (userId: string) => ["reservations", userId, "list"] as const,
  },
  services: {
    all: ["services"] as const,
    publicList: () => ["services", "public"] as const,
    ownerList: (userId: string) => ["services", "owner", userId] as const,
    detail: (serviceId: string) => ["services", "detail", serviceId] as const,
  },
  availability: {
    all: ["availability"] as const,
    slots: (serviceId: string, date: string) => ["availability", serviceId, date] as const,
  },
  workingHours: {
    all: (userId: string) => ["working-hours", userId] as const,
  },
  company: {
    me: (userId: string) => ["company", "me", userId] as const,
  },
  favorites: {
    all: (userId: string) => ["favorites", userId] as const,
  },
};
