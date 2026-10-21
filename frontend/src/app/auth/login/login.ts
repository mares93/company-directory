import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { Alert } from '../../shared/alert/alert';
import { AuthService } from '../../core/services/auth/auth.service';
import { errorText } from '../../core/http-errors';
import { SessionStore } from '../../core/services/session/session';

@Component({
  selector: 'app-login',
  imports: [FormsModule, Alert],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private auth = inject(AuthService);
  protected expired = inject(SessionStore).expired;

  email = signal('');
  password = signal('');
  error = signal<string | null>(null);

  login() { this.run(this.auth.login(this.email(), this.password())); }
  register() { this.run(this.auth.register(this.email(), this.password())); }

  // On success the session store changes and the app swaps this screen for the companies page.
  private run(request: Observable<unknown>) {
    request.subscribe({
      next: () => { this.email.set(''); this.password.set(''); this.error.set(null); },
      error: (e: HttpErrorResponse) => this.error.set(errorText(e))
    });
  }
}
