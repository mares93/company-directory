import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE } from './api';
import { authInterceptor } from './auth.interceptor';
import { SessionStore } from './services/session/session';

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let store: SessionStore;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SessionStore);
    store.set({ token: 'abc', email: 'a@b.com', expiresAtUtc: new Date(Date.now() + 60_000).toISOString() });
  });

  afterEach(() => {
    controller.verify();
    localStorage.clear();
  });

  const unauthorized = { status: 401, statusText: 'Unauthorized' };

  it('adds the bearer token to API calls', () => {
    http.get(`${API_BASE}/companies`).subscribe();
    const req = controller.expectOne(`${API_BASE}/companies`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    req.flush([]);
  });

  it('does not send the token to other hosts', () => {
    http.get('https://example.com/data').subscribe();
    const req = controller.expectOne('https://example.com/data');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('expires the session when a company call answers 401', () => {
    http.get(`${API_BASE}/companies`).subscribe({ error: () => {} });
    controller.expectOne(`${API_BASE}/companies`).flush({}, unauthorized);

    expect(store.session()).toBeNull();
    expect(store.expired()).toBeTrue();
  });

  it('keeps the session when login answers 401 (that only means wrong credentials)', () => {
    http.post(`${API_BASE}/auth/login`, {}).subscribe({ error: () => {} });
    controller.expectOne(`${API_BASE}/auth/login`).flush({}, unauthorized);

    expect(store.session()).not.toBeNull();
    expect(store.expired()).toBeFalse();
  });

  it('passes other errors through untouched', () => {
    let status = 0;
    http.get(`${API_BASE}/companies`).subscribe({ error: e => (status = e.status) });
    controller.expectOne(`${API_BASE}/companies`).flush({}, { status: 500, statusText: 'Server Error' });

    expect(status).toBe(500);
    expect(store.session()).not.toBeNull();
  });
});
