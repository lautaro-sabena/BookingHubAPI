import Link from "next/link";

export default function Home() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-24">
      <h1 className="text-4xl font-bold mb-8">Welcome to BookingHub</h1>
      <div className="flex gap-4">
        <Link 
          href="/login"
          className="px-6 py-3 bg-primary text-primary-foreground rounded-md hover:opacity-90"
        >
          Login
        </Link>
        <Link 
          href="/register"
          className="px-6 py-3 bg-secondary text-secondary-foreground rounded-md hover:opacity-90"
        >
          Register
        </Link>
      </div>
    </main>
  );
}
