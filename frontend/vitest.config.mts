import path from 'node:path';
import { defineConfig } from 'vitest/config';

// Mirrors the "@/*" path alias of tsconfig.json so tests can import app modules the way the app does.
export default defineConfig({
  resolve: {
    alias: { '@': path.resolve(__dirname, 'src') },
  },
});
