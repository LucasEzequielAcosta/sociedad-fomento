/** Shared API contract types mirroring the backend DTOs. */
export interface AdminIdentity {
  id: number;
  email: string;
  role: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface MemberSummary {
  id: number;
  dni: string;
  firstName: string;
  lastName: string;
  status: 'Active' | 'Inactive';
}

export interface MemberDetail extends MemberSummary {
  address: string;
  birthDate: string;
  phone: string;
  email: string | null;
  registrationDate: string;
}

export interface FeeRate {
  id: number;
  effectiveMonth: string;
  amount: number;
  createdAtUtc: string;
}

export interface StatementObligation {
  id: number;
  period: string;
  amount: number;
  status: 'Pending' | 'Paid' | 'Overdue' | 'Future';
}

export interface StatementPayment {
  id: number;
  paymentDate: string;
  amount: number;
  method: 'Cash' | 'BankTransfer' | 'DirectDebit';
  status: 'Active' | 'Cancelled';
}

export interface MemberStatement {
  memberId: number;
  dni: string;
  firstName: string;
  lastName: string;
  status: 'Active' | 'Inactive';
  obligations: StatementObligation[];
  payments: StatementPayment[];
  totalPaid: number;
  totalDebt: number;
}

export type PaymentMethod = 'Cash' | 'BankTransfer' | 'DirectDebit';

export interface PaymentListItem {
  id: number;
  memberId: number;
  memberDni: string;
  memberFirstName: string;
  memberLastName: string;
  paymentDate: string;
  amount: number;
  method: PaymentMethod;
  status: 'Active' | 'Cancelled';
  periods: string[];
}

export type AccountingEntryType = 'Income' | 'Expense';

export interface AccountingEntry {
  id: number;
  entryDate: string;
  type: AccountingEntryType;
  source: 'FeePayment' | 'Manual';
  concept: string;
  amount: number;
  status: 'Active' | 'Cancelled';
  paymentId: number | null;
  cancelledAtUtc: string | null;
  createdAtUtc: string;
}

export interface AccountingSummary {
  from: string;
  to: string;
  totalIncome: number;
  totalExpense: number;
  balance: number;
}

export interface Dashboard {
  members: { active: number; inactive: number };
  obligations: { pending: number; overdue: number; totalDebt: number };
  economy: AccountingSummary;
}
