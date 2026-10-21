import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth/auth.service';
import { SessionStore } from '../../core/services/session/session';

@Component({
  selector: 'app-header',
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class Header {
  private auth = inject(AuthService);
  protected session = inject(SessionStore).session;

  logout() { this.auth.logout(); }
}
