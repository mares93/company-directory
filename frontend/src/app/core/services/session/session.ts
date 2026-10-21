import { Injectable, signal } from '@angular/core';
import { Session } from '../../models/session';

const KEY = 'session';

@Injectable({ providedIn: 'root' })
export class SessionStore {
  readonly session = signal<Session | null>(this.read());
  /** True after the API rejected our token, so the login screen can explain why we are back there. */
  readonly expired = signal(false);

  set(s: Session) {
    try { localStorage.setItem(KEY, JSON.stringify(s)); } catch { /* storage unavailable: session lives in memory only */ }
    this.expired.set(false);
    this.session.set(s);
  }

  clear() {
    try { localStorage.removeItem(KEY); } catch { /* ignore */ }
    this.session.set(null);
  }

  expire() {
    this.clear();
    this.expired.set(true);
  }

  // Ignores a stored session whose token has already expired.
  private read(): Session | null {
    try {
      const s = JSON.parse(localStorage.getItem(KEY) ?? 'null') as Session | null;
      return s && new Date(s.expiresAtUtc) > new Date() ? s : null;
    } catch { return null; }
  }
}
