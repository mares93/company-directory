import { Component, inject } from '@angular/core';
import { Login } from './auth/login/login';
import { CompaniesPage } from './companies/companies-page/companies-page';
import { SessionStore } from './core/services/session/session';
import { Header } from './layout/header/header';

/** Shell: the header, and either the login or the companies page depending on the session. */
@Component({
  selector: 'app-root',
  imports: [Header, Login, CompaniesPage],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected session = inject(SessionStore).session;
}
