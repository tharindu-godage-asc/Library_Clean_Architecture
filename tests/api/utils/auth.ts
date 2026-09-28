import { APIRequestContext } from '@playwright/test';
import { LoginResponse } from './types';

export function randomSuffix(): string {
  return Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
}

export async function registerMember(
  request: APIRequestContext,
  overrides: Partial<{ name: string; email: string; phoneNumber: string; password: string }> = {},
): Promise<{ id: string; email: string; password: string }> {
  const suffix = randomSuffix();
  const body = {
    name: overrides.name ?? `Playwright Member ${suffix}`,
    email: overrides.email ?? `member.${suffix}@test.local`,
    phoneNumber: overrides.phoneNumber ?? '+15550001234',
    password: overrides.password ?? 'TestPass123!',
  };
  const res = await request.post('/api/auth/register', { data: body });
  if (res.status() !== 201) {
    throw new Error(`Register failed: ${res.status()} ${await res.text()}`);
  }
  const created = await res.json();
  return { id: created.id, email: body.email, password: body.password };
}

export async function login(
  request: APIRequestContext,
  email: string,
  password: string,
): Promise<LoginResponse> {
  const res = await request.post('/api/auth/login', { data: { email, password } });
  if (res.status() !== 200) {
    throw new Error(`Login failed for ${email}: ${res.status()} ${await res.text()}`);
  }
  return res.json();
}

export function authHeaders(token: string): { Authorization: string } {
  return { Authorization: `Bearer ${token}` };
}
