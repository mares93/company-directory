import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE } from '../../api';
import { AuthService } from './auth.service';
import { Session } from '../../models/session';
import { SessionStore } from '../session/session';

describe('AuthService', () => {
  let service: AuthService;
  let controller: HttpTestingController;
  let store: SessionStore;
  const session: Session = { token: 't', email: 'a@b.com', expiresAtUtc: new Date(Date.now() + 60_000).toISOString() };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AuthService);
    controller = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SessionStore);
  });

  afterEach(() => {
    controller.verify();
    localStorage.clear();
  });

  it('login posts the credentials and stores the returned session', () => {
    service.login('a@b.com', 'secret123').subscribe();
    const req = controller.expectOne(`${API_BASE}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.com', password: 'secret123' });
    req.flush(session);

    expect(store.session()).toEqual(session);
  });

  it('register uses the register endpoint and also signs the user in', () => {
    service.register('a@b.com', 'secret123').subscribe();
    controller.expectOne(`${API_BASE}/auth/register`).flush(session);

    expect(store.session()).toEqual(session);
  });

  it('does not store anything when the call fails', () => {
    service.login('a@b.com', 'wrong').subscribe({ error: () => {} });
    controller.expectOne(`${API_BASE}/auth/login`).flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(store.session()).toBeNull();
  });

  it('logout clears the session', () => {
    store.set(session);
    service.logout();
    expect(store.session()).toBeNull();
  });
});
