import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SessionStore } from '../../core/services/session/session';
import { Header } from './header';

describe('Header', () => {
  let fixture: ComponentFixture<Header>;
  let store: SessionStore;
  let el: HTMLElement;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [Header],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    store = TestBed.inject(SessionStore);
    fixture = TestBed.createComponent(Header);
    el = fixture.nativeElement;
  });

  afterEach(() => localStorage.clear());

  const signIn = () => store.set({ token: 't', email: 'ana@example.com', expiresAtUtc: new Date(Date.now() + 60_000).toISOString() });

  it('always shows the title', () => {
    fixture.detectChanges();
    expect(el.querySelector('.brand')?.textContent).toBe('Company Directory');
  });

  it('shows no user bar when signed out', () => {
    fixture.detectChanges();
    expect(el.querySelector('.user')).toBeNull();
  });

  it('shows the signed-in email', () => {
    signIn();
    fixture.detectChanges();
    expect(el.querySelector('.user small')?.textContent).toContain('ana@example.com');
  });

  it('log out clears the session and hides the user bar', () => {
    signIn();
    fixture.detectChanges();

    (el.querySelector('.btn-ghost') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(store.session()).toBeNull();
    expect(el.querySelector('.user')).toBeNull();
  });
});
