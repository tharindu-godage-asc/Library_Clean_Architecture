import { test, expect } from '@playwright/test';
import { login, authHeaders, randomSuffix } from '../utils/auth';

/**
 * Chain: Admin Login -> Create Book -> Update Book -> Delete Book ->
 * View all Borrowings -> View all Users -> View User by Id -> Delete User.
 *
 * "View User by Id" / "Delete User" need a concrete, disposable user to target — a
 * throwaway member (created via POST /api/members, no login needed) is inserted right
 * before "View all Users" so those steps aren't guessing at pre-existing data.
 */
test.describe('Admin journey: login -> book CRUD -> view all -> user CRUD', () => {
  test('admin can manage books and users end-to-end', async ({ request }) => {
    const headers = await test.step('Admin Login', async () => {
      const res = await login(request, process.env.ADMIN_EMAIL!, process.env.ADMIN_PASSWORD!);
      expect(res.token).toBeTruthy();
      return authHeaders(res.token);
    });

    const book = await test.step('Create Book', async () => {
      const res = await request.post('/api/books', {
        headers,
        data: {
          title: `Admin Journey Book ${randomSuffix()}`,
          author: 'Test Author',
          isbn: `ISBN-${randomSuffix()}`,
          publishedYear: 2021,
          totalCopies: 1,
        },
      });
      expect(res.status()).toBe(201);
      return res.json();
    });

    await test.step('Update Book', async () => {
      const res = await request.put(`/api/books/${book.id}`, {
        headers,
        data: {
          title: `${book.title} (updated)`,
          author: book.author,
          isbn: book.isbn,
          publishedYear: book.publishedYear,
          totalCopies: 2,
        },
      });
      expect(res.status()).toBe(200);
      expect((await res.json()).totalCopies).toBe(2);
    });

    await test.step('Delete Book', async () => {
      const res = await request.delete(`/api/books/${book.id}`, { headers });
      expect(res.status()).toBe(204);

      const getRes = await request.get(`/api/books/${book.id}`, { headers });
      expect(getRes.status()).toBe(404);
    });

    await test.step('View all Borrowings', async () => {
      const res = await request.get('/api/borrowings', { headers });
      expect(res.status()).toBe(200);
      expect(Array.isArray(await res.json())).toBe(true);
    });

    const user = await test.step('Create throwaway user (setup for the next 3 steps)', async () => {
      const suffix = randomSuffix();
      const res = await request.post('/api/members', {
        headers,
        data: {
          name: `Admin Journey User ${suffix}`,
          email: `adminjourney.${suffix}@test.local`,
          phoneNumber: '+15551110000',
        },
      });
      expect(res.status()).toBe(201);
      return res.json();
    });

    await test.step('View all Users', async () => {
      const res = await request.get('/api/members', { headers });
      expect(res.status()).toBe(200);
      const users = await res.json();
      expect(users.some((u: { id: string }) => u.id === user.id)).toBe(true);
    });

    await test.step('View User by Id', async () => {
      const res = await request.get(`/api/members/${user.id}`, { headers });
      expect(res.status()).toBe(200);
      expect((await res.json()).id).toBe(user.id);
    });

    await test.step('Delete User', async () => {
      const res = await request.delete(`/api/members/${user.id}`, { headers });
      expect(res.status()).toBe(204);

      const getRes = await request.get(`/api/members/${user.id}`, { headers });
      expect(getRes.status()).toBe(404);
    });
  });
});
