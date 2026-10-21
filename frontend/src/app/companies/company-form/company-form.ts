import { Component, inject, output, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Alert } from '../../shared/alert/alert';
import { CompaniesService } from '../../core/services/companies/companies.service';
import { splitProtocol } from '../../core/helpers';
import { errorText } from '../../core/http-errors';
import { Protocol } from '../../core/models/protocol';

@Component({
  selector: 'app-company-form',
  imports: [FormsModule, Alert],
  templateUrl: './company-form.html',
  styleUrl: './company-form.css'
})
export class CompanyForm {
  private companies = inject(CompaniesService);

  /** Emitted after the API accepted the company, so the list can refresh. */
  created = output<void>();

  name = signal('');
  protocol = signal<Protocol>('https://');
  websiteUrl = signal(''); // typed without the protocol; the dropdown supplies it
  message = signal<{ ok: boolean; text: string } | null>(null);

  onUrlChange(value: string) {
    const { protocol, rest } = splitProtocol(value, this.protocol());
    this.protocol.set(protocol);
    this.websiteUrl.set(rest);
  }

  submit() {
    const url = this.websiteUrl().trim();
    this.companies.create(this.name(), url ? this.protocol() + url : '').subscribe({
      next: () => {
        this.message.set({ ok: true, text: 'Company added successfully.' });
        this.name.set('');
        this.websiteUrl.set('');
        this.created.emit();
      },
      error: (e: HttpErrorResponse) => this.message.set({ ok: false, text: errorText(e) })
    });
  }
}
