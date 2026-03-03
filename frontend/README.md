# BookingHub Frontend

Next.js 14 frontend for the BookingHub API.

## Getting Started

### Prerequisites

- Node.js 18+
- npm or yarn

### Installation

```bash
cd frontend
npm install
```

### Development

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

### Environment Variables

Create a `.env.local` file:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000
```

### Docker

```bash
docker build -t bookinghub-frontend .
docker run -p 3000:3000 bookinghub-frontend
```

Or use Docker Compose:

```bash
docker-compose up --build
```

## Features

- User registration and login (Owner/Customer roles)
- Company management for business owners
- Service management
- Availability configuration
- Reservation booking system
- Responsive design

## Tech Stack

- Next.js 14 (App Router)
- TypeScript
- Tailwind CSS
- React Query
- Zustand
- Axios
