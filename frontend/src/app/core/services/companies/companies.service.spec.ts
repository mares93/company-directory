import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE } from '../../api';
import { CompaniesService } from './companies.service';
import { Company } from '../../models/company';
import { Row } from '../../models/row';

describe('CompaniesService', () => {
  let service: CompaniesService;
  let controller: HttpTestingController;
  const acme: Company = {
    id: '1', name: 'Acme', websiteUrl: 'https://acme.com/', domain: 'acme.com',
    createdBy: 'a@b.com', createdAtUtc: '2026-01-01T00:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(CompaniesService);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('uses the plain list when no name is given, sending the filters, order and paging', () => {
    let rows: Row[] = [];
    service.list({ name: '  ', domain: ' acme ', sort: 'name', page: 2, pageSize: 5 })
      .subscribe(r => (rows = r.items));

    const req = controller.expectOne(`${API_BASE}/companies?domain=acme&page=2&pageSize=5&sort=name`);
    req.flush({ items: [acme], total: 6, page: 2, pageSize: 5 });

    expect(rows).toEqual([{ company: acme }]); // a plain list has no relevance
  });

  it('uses the relevance search when a name is given and keeps each score', () => {
    let rows: Row[] = [];
    let total = 0;
    service.list({ name: ' acme ', domain: '', sort: 'relevance', page: 1, pageSize: 10 })
      .subscribe(r => { rows = r.items; total = r.total; });

    const req = controller.expectOne(r => r.url === `${API_BASE}/companies/search`);
    expect(req.request.params.get('q')).toBe('acme');
    expect(req.request.params.get('sort')).toBe('relevance');
    req.flush({ items: [{ company: acme, relevance: 0.9 }], total: 1, page: 1, pageSize: 10 });

    expect(rows).toEqual([{ company: acme, relevance: 0.9 }]);
    expect(total).toBe(1);
  });

  it('get fetches one company by id', () => {
    service.get('1').subscribe();
    const req = controller.expectOne(`${API_BASE}/companies/1`);
    expect(req.request.method).toBe('GET');
    req.flush(acme);
  });

  it('create posts the name and the website', () => {
    service.create('Acme', 'https://acme.com').subscribe();
    const req = controller.expectOne(`${API_BASE}/companies`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'Acme', websiteUrl: 'https://acme.com' });
    req.flush(acme);
  });
});
