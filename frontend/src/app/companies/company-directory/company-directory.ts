import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { Alert } from '../../shared/alert/alert';
import { CompaniesService } from '../../core/services/companies/companies.service';
import { errorText } from '../../core/http-errors';
import { Company } from '../../core/models/company';
import { Row } from '../../core/models/row';
import { Sort } from '../../core/models/sort';
import { Pagination } from '../pagination/pagination';

@Component({
  selector: 'app-company-directory',
  imports: [FormsModule, DatePipe, Alert, Pagination],
  templateUrl: './company-directory.html',
  styleUrl: './company-directory.css'
})
export class CompanyDirectory {
  private companies = inject(CompaniesService);

  nameFilter = signal('');
  domainFilter = signal('');

  // The server filters, sorts and pages; these signals only describe the page being shown.
  sort = signal<Sort>('relevance');
  rows = signal<Row[]>([]);
  total = signal(0);
  page = signal(1);
  pageSize = signal(5);
  detail = signal<Company | null>(null);
  error = signal<string | null>(null);

  private request?: Subscription;

  constructor() { this.search(); }

  /** Clears the filters and shows the first page again (used after a company is added). */
  reload() {
    this.nameFilter.set('');
    this.domainFilter.set('');
    this.search();
  }

  search() { this.page.set(1); this.fetch(); }
  setSort(value: Sort) { this.sort.set(value); this.search(); }
  setPageSize(value: number) { this.pageSize.set(value); this.search(); }
  goTo(p: number) { this.page.set(p); this.fetch(); }

  toggleDetails(c: Company) {
    if (this.detail()?.id === c.id) { this.detail.set(null); return; }
    this.companies.get(c.id).subscribe(d => this.detail.set(d));
  }

  // The API stores the URL as parsed ("https://www.acme.com/"); the trailing slash is noise in the table.
  displayUrl(url: string) { return url.replace(/\/$/, ''); }

  // Avatar color is derived from the name so each company keeps its own.
  avatarClass(name: string) {
    return 'av' + ([...name].reduce((sum, ch) => sum + ch.charCodeAt(0), 0) % 4);
  }

  private fetch() {
    // Cancel the previous request: a slow old response must not overwrite a newer one.
    this.request?.unsubscribe();
    this.detail.set(null);
    this.request = this.companies.list({
      name: this.nameFilter(), domain: this.domainFilter(), sort: this.sort(), page: this.page(), pageSize: this.pageSize()
    }).subscribe({
      next: r => { this.error.set(null); this.total.set(r.total); this.rows.set(r.items); },
      error: (e: HttpErrorResponse) => this.error.set(errorText(e))
    });
  }
}
