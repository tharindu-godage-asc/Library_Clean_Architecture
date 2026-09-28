export interface LoginResponse {
  id: string;
  email: string;
  token: string;
  expiration: string;
}

export interface BookResponse {
  id: string;
  title: string;
  author: string;
  isbn: string;
  publishedYear: number;
  totalCopies: number;
  availableCopies: number;
}

export interface MemberResponse {
  id: string;
  name: string;
  email: string;
  phoneNumber: string;
  isActive: boolean;
}

export interface BorrowingResponse {
  id: string;
  bookId: string;
  memberId: string;
  borrowedAt: string;
  dueDate: string;
  returnedAt: string | null;
  status: string;
}
