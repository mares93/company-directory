import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { API_BASE } from './core/api';
import { SessionStore } from './core/services/session/session';

describe('App', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    localStorage.clear();
  });

  it('shows the login when there is no session', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('app-login')).not.toBeNull();
    expect(el.querySelector('app-companies-page')).toBeNull();
  });

  it('shows the companies page when signed in, and goes back to the login on logout', () => {
    const store = TestBed.inject(SessionStore);
    store.set({ token: 't', email: 'a@b.com', expiresAtUtc: new Date(Date.now() + 60_000).toISOString() });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController)
      .expectOne(r => r.url === `${API_BASE}/companies`)
      .flush({ items: [], total: 0, page: 1, pageSize: 5 });
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('app-companies-page')).not.toBeNull();
    expect(el.querySelector('app-login')).toBeNull();

    store.clear();
    fixture.detectChanges();
    expect(el.querySelector('app-login')).not.toBeNull();
  });

  it('shows the page title and subtitle', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent).toBe('Companies');
  });
});
