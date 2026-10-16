import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MemberService } from './member.service';
import { MemberSummary, PagedResult } from '../../core/api-models';

/** Member list with DNI/name filters, pagination and CSV export. */
@Component({
  selector: 'app-members-list',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './members-list.component.html',
})
export class MembersListComponent {
  private readonly formBuilder = inject(FormBuilder);
  protected readonly memberService = inject(MemberService);

  protected readonly columns = ['dni', 'name', 'status', 'actions'];
  protected readonly filters = this.formBuilder.nonNullable.group({
    dni: [''],
    name: [''],
    lastName: [''],
  });
  protected readonly result = signal<PagedResult<MemberSummary> | null>(null);

  constructor() {
    this.search(1);
  }

  protected search(page: number): void {
    const { dni, name, lastName } = this.filters.getRawValue();
    this.memberService
      .search({ dni, name, lastName, page, pageSize: this.result()?.pageSize ?? 20 })
      .subscribe((result) => this.result.set(result));
  }

  protected onPage(event: PageEvent): void {
    const { dni, name, lastName } = this.filters.getRawValue();
    this.memberService
      .search({ dni, name, lastName, page: event.pageIndex + 1, pageSize: event.pageSize })
      .subscribe((result) => this.result.set(result));
  }

  protected exportUrl(): string {
    return this.memberService.exportCsv(this.filters.getRawValue());
  }
}
