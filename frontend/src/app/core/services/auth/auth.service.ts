import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_BASE } from '../../api';
import { Session } from '../../models/session';
import { SessionStore } from '../session/session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private store = inject(SessionStore);

  login(email: string, password: string) { return this.authenticate('login', email, password); }
  register(email: string, password: string) { return this.authenticate('register', email, password); }
  logout() { this.store.clear(); }

  private authenticate(action: 'login' | 'register', email: string, password: string): Observable<Session> {
    return this.http.post<Session>(`${API_BASE}/auth/${action}`, { email, password }).pipe(tap(s => this.store.set(s)));
  }
}
