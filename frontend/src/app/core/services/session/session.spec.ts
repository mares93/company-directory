import { TestBed } from '@angular/core/testing';
import { Session } from '../../models/session';
import { SessionStore } from './session';

describe('SessionStore', () => {
  const valid: Session = { token: 't', email: 'a@b.com', expiresAtUtc: new Date(Date.now() + 60_000).toISOString() };
  const expired: Session = { ...valid, expiresAtUtc: new Date(Date.now() - 60_000).toISOString() };

  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  it('restores a stored session that has not expired', () => {
    localStorage.setItem('session', JSON.stringify(valid));
    expect(TestBed.inject(SessionStore).session()).toEqual(valid);
  });

  it('ignores a stored session whose token already expired', () => {
    localStorage.setItem('session', JSON.stringify(expired));
    expect(TestBed.inject(SessionStore).session()).toBeNull();
  });

  it('ignores corrupt stored data', () => {
    localStorage.setItem('session', '{not json');
    expect(TestBed.inject(SessionStore).session()).toBeNull();
  });

  it('set() stores the session so a reload keeps it', () => {
    TestBed.inject(SessionStore).set(valid);
    expect(JSON.parse(localStorage.getItem('session')!)).toEqual(valid);
  });

  it('expire() clears the session and flags it, and a new login clears the flag', () => {
    const store = TestBed.inject(SessionStore);
    store.set(valid);

    store.expire();
    expect(store.session()).toBeNull();
    expect(store.expired()).toBeTrue();
    expect(localStorage.getItem('session')).toBeNull();

    store.set(valid);
    expect(store.expired()).toBeFalse();
  });

  it('clear() (a normal logout) does not flag the session as expired', () => {
    const store = TestBed.inject(SessionStore);
    store.set(valid);
    store.clear();
    expect(store.expired()).toBeFalse();
  });
});
