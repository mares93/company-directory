import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { API_BASE } from '../../core/api';
import { Company } from '../../core/models/company';
import { CompanyDirectory } from './company-directory';

describe('CompanyDirectory', () => {
  let fixture: ComponentFixture<CompanyDirectory>;
  let component: CompanyDirectory;
  let controller: HttpTestingController;
  let el: HTMLElement;

  const company = (id: string, name: string): Company => ({
    id, name, websiteUrl: `https://${name.toLowerCase()}.com/`, domain: `${name.toLowerCase()}.com`,
    createdBy: 'a@b.com', createdAtUtc: '2026-01-01T10:00:00Z'
  });
  const page = (items: unknown[], total = items.length, pageNumber = 1) => ({ items, total, page: pageNumber, pageSize: 5 });

  const list = () => controller.expectOne(r => r.url === `${API_BASE}/companies`);
  const search = () => controller.expectOne(r => r.url === `${API_BASE}/companies/search`);

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CompanyDirectory],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    controller = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CompanyDirectory);
    component = fixture.componentInstance;
    el = fixture.nativeElement;
  });

  afterEach(() => controller.verify());

  /** Answers the request the directory makes as soon as it is created. */
  const loadInitial = (body: object) => {
    list().flush(body);
    fixture.detectChanges();
  };
  const rows = () => el.querySelectorAll('tbody tr:not(.detail-row)');

  it('loads the first page on creation with the default paging', () => {
    const req: TestRequest = list();
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('5');
    expect(req.request.params.get('sort')).toBe('relevance');
    req.flush(page([]));
  });

  it('renders a row per company with its full website and the total', () => {
    loadInitial(page([company('1', 'Acme'), company('2', 'Beta')]));

    expect(rows().length).toBe(2);
    expect(rows()[0].textContent).toContain('Acme');
    expect(rows()[0].querySelector('a.site')?.textContent).toBe('https://acme.com'); // trailing slash trimmed
    expect(el.querySelector('.hint')?.textContent).toContain('2 companies');
    expect(el.querySelector('.score')).toBeNull(); // a plain list has no relevance
  });

  it('shows the empty state when nothing matches', () => {
    loadInitial(page([]));

    expect(el.querySelector('app-alert')?.textContent).toContain('No companies found.');
    expect(el.querySelector('table')).toBeNull();
  });

  it('shows an error when the call fails', () => {
    list().flush({ title: 'boom' }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(el.querySelector('app-alert')?.textContent).toContain('boom');
  });

  it('searches by name through the relevance endpoint and shows the score', () => {
    loadInitial(page([]));

    component.nameFilter.set('acme');
    component.search();
    const req = search();
    expect(req.request.params.get('q')).toBe('acme');
    req.flush(page([{ company: company('1', 'Acme'), relevance: 0.9 }]));
    fixture.detectChanges();

    expect(el.querySelector('.score')?.textContent).toBe('0.9');
  });

  it('goes back to page 1 whenever the search changes', () => {
    loadInitial(page([company('1', 'Acme')], 12));
    component.goTo(3);
    list().flush(page([], 12, 3));
    expect(component.page()).toBe(3);

    component.domainFilter.set('acme');
    component.search();
    const req = list();
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('domain')).toBe('acme');
    req.flush(page([]));
  });

  it('asks the server for the next page and keeps the filters', () => {
    loadInitial(page([company('1', 'Acme')], 12));
    component.domainFilter.set('acme');

    component.goTo(2);
    const req = list();
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('domain')).toBe('acme');
    req.flush(page([], 12, 2));
  });

  it('changing the order or the page size asks the server again from page 1', () => {
    loadInitial(page([]));

    component.setSort('name');
    const sorted = list();
    expect(sorted.request.params.get('sort')).toBe('name');
    expect(sorted.request.params.get('page')).toBe('1');
    sorted.flush(page([]));

    component.setPageSize(20);
    const resized = list();
    expect(resized.request.params.get('pageSize')).toBe('20');
    expect(resized.request.params.get('page')).toBe('1');
    resized.flush(page([]));
  });

  it('cancels the previous request when a new one starts, so a slow answer cannot overwrite a newer one', () => {
    loadInitial(page([]));

    component.goTo(2);
    const first = list();
    component.goTo(3);
    const second = list();

    expect(first.cancelled).toBeTrue();
    second.flush(page([company('3', 'Gamma')], 12, 3));
    fixture.detectChanges();
    expect(rows().length).toBe(1);
  });

  it('opens the details of a company fetched by id, and closes them on a second click', () => {
    loadInitial(page([company('1', 'Acme')]));

    component.toggleDetails(company('1', 'Acme'));
    controller.expectOne(`${API_BASE}/companies/1`).flush(company('1', 'Acme'));
    fixture.detectChanges();
    expect(el.querySelector('.detail-row .mono')?.textContent).toBe('1');

    component.toggleDetails(company('1', 'Acme'));
    fixture.detectChanges();
    expect(el.querySelector('.detail-row')).toBeNull();
  });

  it('reload clears the filters and fetches the first page again', () => {
    loadInitial(page([]));
    component.nameFilter.set('acme');
    component.domainFilter.set('x');

    component.reload();

    expect(component.nameFilter()).toBe('');
    expect(component.domainFilter()).toBe('');
    list().flush(page([]));
  });

  it('gives each company a stable avatar color', () => {
    expect(component.avatarClass('Acme')).toBe(component.avatarClass('Acme'));
    expect(component.avatarClass('Acme')).toMatch(/^av[0-3]$/);
    list().flush(page([]));
  });
});
