import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE } from '../../api';
import { Company } from '../../models/company';
import { CompanyQuery } from '../../models/company-query';
import { Paged } from '../../models/paged';
import { Row } from '../../models/row';
import { SearchResult } from '../../models/search-result';

const COMPANIES = `${API_BASE}/companies`;

@Injectable({ providedIn: 'root' })
export class CompaniesService {
  private http = inject(HttpClient);

  /** A name search uses the relevance endpoint; otherwise the plain list. Both filter, sort and page on the server. */
  list(q: CompanyQuery): Observable<Paged<Row>> {
    const name = q.name.trim();
    const params = { domain: q.domain.trim(), page: q.page, pageSize: q.pageSize, sort: q.sort };
    return name
      ? this.http.get<Paged<SearchResult>>(`${COMPANIES}/search`, { params: { ...params, q: name } }).pipe(
          map(r => ({ ...r, items: r.items.map(x => ({ company: x.company, relevance: x.relevance })) })))
      : this.http.get<Paged<Company>>(COMPANIES, { params }).pipe(
          map(r => ({ ...r, items: r.items.map(company => ({ company })) })));
  }

  get(id: string) { return this.http.get<Company>(`${COMPANIES}/${id}`); }

  create(name: string, websiteUrl: string) { return this.http.post<Company>(COMPANIES, { name, websiteUrl }); }
}
