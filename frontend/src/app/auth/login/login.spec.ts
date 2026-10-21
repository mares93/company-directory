import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { API_BASE } from '../../core/api';
import { SessionStore } from '../../core/services/session/session';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let component: Login;
  let controller: HttpTestingController;
  let store: SessionStore;
  let el: HTMLElement;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    controller = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SessionStore);
    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    el = fixture.nativeElement;
    fixture.detectChanges();
  });

  afterEach(() => {
    controller.verify();
    localStorage.clear();
  });

  const session = { token: 't', email: 'a@b.com', expiresAtUtc: new Date(Date.now() + 60_000).toISOString() };

  it('shows the sign-in form without any alert', () => {
    expect(el.querySelector('h2')?.textContent).toBe('Sign in');
    expect(el.querySelector('app-alert')).toBeNull();
  });

  it('shows the API message when the login fails', () => {
    component.email.set('a@b.com');
    component.password.set('wrong-password');
    component.login();

    controller.expectOne(`${API_BASE}/auth/login`)
      .flush({ title: 'Invalid email or password.' }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    expect(el.querySelector('app-alert')?.textContent).toContain('Invalid email or password.');
    expect(store.session()).toBeNull();
  });

  it('signs in and clears the form on success', () => {
    component.email.set('a@b.com');
    component.password.set('secret123');
    component.login();

    controller.expectOne(`${API_BASE}/auth/login`).flush(session);

    expect(store.session()).toEqual(session);
    expect(component.email()).toBe('');
    expect(component.password()).toBe('');
  });

  it('register uses the register endpoint', () => {
    component.email.set('new@b.com');
    component.password.set('secret123');
    component.register();

    const req = controller.expectOne(`${API_BASE}/auth/register`);
    expect(req.request.body).toEqual({ email: 'new@b.com', password: 'secret123' });
    req.flush(session);
  });

  it('tells the user when the session expired', () => {
    store.expire();
    fixture.detectChanges();

    expect(el.querySelector('app-alert')?.textContent).toContain('Your session expired');
  });

  it('explains rate limiting instead of a generic error', () => {
    component.email.set('a@b.com');
    component.password.set('x');
    component.login();

    controller.expectOne(`${API_BASE}/auth/login`).flush(null, { status: 429, statusText: 'Too Many Requests' });
    fixture.detectChanges();

    expect(el.querySelector('app-alert')?.textContent).toContain('Too many attempts');
  });
});
