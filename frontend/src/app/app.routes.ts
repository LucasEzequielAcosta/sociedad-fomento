import { Routes } from '@angular/router';
import { adminGuard } from './core/auth.guard';
import { memberGuard } from './core/portal.guard';
import { LoginComponent } from './features/authentication/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { MembersListComponent } from './features/members/members-list.component';
import { MemberDetailComponent } from './features/members/member-detail.component';
import { MemberFormComponent } from './features/members/member-form.component';
import { FeeRatesComponent } from './features/fees/fee-rates.component';
import { PaymentsListComponent } from './features/payments/payments-list.component';
import { PaymentFormComponent } from './features/payments/payment-form.component';
import { AccountingComponent } from './features/accounting/accounting.component';
import { PortalLoginComponent } from './features/member-portal/portal-login.component';
import { PortalStatementComponent } from './features/member-portal/portal-statement.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'portal', component: PortalLoginComponent },
  { path: 'portal/estado', component: PortalStatementComponent, canActivate: [memberGuard] },
  { path: 'dashboard', component: DashboardComponent, canActivate: [adminGuard] },
  { path: 'socios', component: MembersListComponent, canActivate: [adminGuard] },
  { path: 'socios/nuevo', component: MemberFormComponent, canActivate: [adminGuard] },
  { path: 'socios/:id', component: MemberDetailComponent, canActivate: [adminGuard] },
  { path: 'cuotas', component: FeeRatesComponent, canActivate: [adminGuard] },
  { path: 'pagos', component: PaymentsListComponent, canActivate: [adminGuard] },
  { path: 'pagos/nuevo', component: PaymentFormComponent, canActivate: [adminGuard] },
  { path: 'contabilidad', component: AccountingComponent, canActivate: [adminGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];
