import { test, expect, request } from '@playwright/test';
import { registerMember, login, authHeaders, randomSuffix } from '../utils/auth';
import { BookResponse } from '../utils/types';

/**
 * Chain: Registration -> Login -> View Book details -> Borrow Book ->
 * View Borrowings -> Return Book -> View Profile -> Update Profile.
 *
 * "View Book details" / "Borrow Book" need a book to already exist, which isn't part
 * of the member-facing chain itself — an Admin token creates one throwaway book in
 * beforeAll (and deletes it in afterAll) purely as test setup/teardown.
 */
test.describe('Member journey: register -> login -> borrow -> return -> profile', () => {
  let adminToken: string;
  let book: BookResponse;

  test.beforeAll(async () => {
    const ctx = await request.newContext({ baseURL: process.env.BASE_URL ?? 'https://localhost:7282', ignoreHTTPSErrors: true });
    const adminLogin = await login(ctx, process.env.ADMIN_EMAIL!, process.env.ADMIN_PASSWORD!);
    adminToken = adminLogin.token;

    const res = await ctx.post('/api/books', {
      headers: authHeaders(adminToken),
      data: {
        title: `Member Journey Book ${randomSuffix()}`,
        author: 'Test Author',
        isbn: `ISBN-${randomSuffix()}`,
        publishedYear: 2020,
        totalCopies: 2,
      },
    });
    expect(res.status()).toBe(201);
    book = await res.json();
    await ctx.dispose();
  });

  test.afterAll(async () => {
    const ctx = await request.newContext({ baseURL: process.env.BASE_URL ?? 'https://localhost:7282', ignoreHTTPSErrors: true });
    await ctx.delete(`/api/books/${book.id}`, { headers: authHeaders(adminToken) });
    await ctx.dispose();
  });

  test('member can register, login, borrow/return a book, and manage their profile', async ({ request }) => {
    const credentials = await test.step('Register', async () => {
      return registerMember(request);
    });

    const memberToken = await test.step('Login', async () => {
      const res = await login(request, credentials.email, credentials.password);
      expect(res.token).toBeTruthy();
      return res.token;
    });
    const headers = authHeaders(memberToken);

    await test.step('View Book details', async () => {
      const res = await request.get(`/api/books/${book.id}`, { headers });
      expect(res.status()).toBe(200);
      expect((await res.json()).id).toBe(book.id);
    });

    const borrowing = await test.step('Borrow Book', async () => {
      const res = await request.post('/api/borrowings', {
        headers,
        data: { bookId: book.id, memberId: credentials.id },
      });
      expect(res.status()).toBe(201);
      return res.json();
    });

    await test.step('View Borrowings', async () => {
      const res = await request.get(`/api/members/${credentials.id}/borrowings`, { headers });
      expect(res.status()).toBe(200);
      const borrowings = await res.json();
      expect(borrowings.some((b: { id: string }) => b.id === borrowing.id)).toBe(true);
    });

    await test.step('Return Book', async () => {
      const res = await request.post(`/api/borrowings/${borrowing.id}/return`, { headers });
      expect(res.status()).toBe(204);
    });

    const profile = await test.step('View Profile', async () => {
      const res = await request.get('/api/members/me', { headers });
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.id).toBe(credentials.id);
      return body;
    });

    await test.step('Update Profile', async () => {
      const res = await request.put('/api/members/me', {
        headers,
        data: {
          name: 'Updated Member Name',
          email: profile.email,
          phoneNumber: '+15559876543',
        },
      });
      expect(res.status()).toBe(200);
      const updated = await res.json();
      expect(updated.name).toBe('Updated Member Name');
      expect(updated.phoneNumber).toBe('+15559876543');
    });
  });
});
