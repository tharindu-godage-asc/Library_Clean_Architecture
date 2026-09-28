import { defineConfig } from '@playwright/test';
import * as dotenv from 'dotenv';

dotenv.config();

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  reporter: 'html',
  use: {
    baseURL: process.env.BASE_URL ?? 'https://localhost:7282',
    ignoreHTTPSErrors: true, // local dev cert is self-signed
    extraHTTPHeaders: { 'Content-Type': 'application/json' },
  },
});
