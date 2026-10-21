import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { API_BASE } from '../../core/api';
import { CompanyForm } from './company-form';

describe('CompanyForm', () => {
  let fixture: ComponentFixture<CompanyForm>;
  let component: CompanyForm;
  let controller: HttpTestingController;
  let el: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CompanyForm],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    controller = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CompanyForm);
    component = fixture.componentInstance;
    el = fixture.nativeElement;
    fixture.detectChanges();
  });

  afterEach(() => controller.verify());

  const created = { id: '1', name: 'Acme', websiteUrl: 'https://acme.com/', domain: 'acme.com', createdBy: 'a@b.com', createdAtUtc: '' };

  it('defaults to https', () => {
    expect(component.protocol()).toBe('https://');
  });

  it('joins the chosen protocol and the typed host before sending', () => {
    component.name.set('Acme');
    component.onUrlChange('acme.com');
    component.submit();

    const req = controller.expectOne(`${API_BASE}/companies`);
    expect(req.request.body).toEqual({ name: 'Acme', websiteUrl: 'https://acme.com' });
    req.flush(created);
  });

  it('moves a pasted protocol into the dropdown and sends it', () => {
    component.name.set('Acme');
    component.onUrlChange('http://acme.com');
    expect(component.protocol()).toBe('http://');
    expect(component.websiteUrl()).toBe('acme.com');

    component.submit();
    const req = controller.expectOne(`${API_BASE}/companies`);
    expect(req.request.body.websiteUrl).toBe('http://acme.com');
    req.flush(created);
  });

  it('sends an empty website when nothing was typed, so the API explains what is missing', () => {
    component.name.set('Acme');
    component.submit();

    const req = controller.expectOne(`${API_BASE}/companies`);
    expect(req.request.body.websiteUrl).toBe('');
    req.flush({ title: 'Website URL is required.' }, { status: 400, statusText: 'Bad Request' });
  });

  it('on success: tells the user, clears the fields and emits created', () => {
    let emitted = 0;
    component.created.subscribe(() => emitted++);
    component.name.set('Acme');
    component.onUrlChange('acme.com');
    component.submit();

    controller.expectOne(`${API_BASE}/companies`).flush(created);
    fixture.detectChanges();

    expect(el.querySelector('app-alert')?.textContent).toContain('Company added successfully.');
    expect(component.name()).toBe('');
    expect(component.websiteUrl()).toBe('');
    expect(emitted).toBe(1);
  });

  it('on failure: shows the API message, keeps the fields and does not emit', () => {
    let emitted = 0;
    component.created.subscribe(() => emitted++);
    component.name.set('Acme');
    component.onUrlChange('google.com');
    component.submit();

    controller.expectOne(`${API_BASE}/companies`)
      .flush({ title: "Company name 'Acme' is not relevant to the website 'google.com'." }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(el.querySelector('app-alert')?.textContent).toContain('not relevant');
    expect(component.name()).toBe('Acme');
    expect(emitted).toBe(0);
  });
});
