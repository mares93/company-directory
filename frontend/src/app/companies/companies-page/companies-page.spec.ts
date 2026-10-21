import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { API_BASE } from '../../core/api';
import { CompanyDirectory } from '../company-directory/company-directory';
import { CompanyForm } from '../company-form/company-form';
import { CompaniesPage } from './companies-page';

describe('CompaniesPage', () => {
  let fixture: ComponentFixture<CompaniesPage>;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CompaniesPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    controller = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CompaniesPage);
    fixture.detectChanges();
    // the directory loads its first page as soon as it appears
    controller.expectOne(r => r.url === `${API_BASE}/companies`).flush({ items: [], total: 0, page: 1, pageSize: 5 });
  });

  afterEach(() => controller.verify());

  it('shows the form next to the directory', () => {
    expect(fixture.debugElement.query(By.directive(CompanyForm))).not.toBeNull();
    expect(fixture.debugElement.query(By.directive(CompanyDirectory))).not.toBeNull();
  });

  it('refreshes the directory when the form reports a new company', () => {
    const directory = fixture.debugElement.query(By.directive(CompanyDirectory)).componentInstance as CompanyDirectory;
    const reload = spyOn(directory, 'reload');

    (fixture.debugElement.query(By.directive(CompanyForm)).componentInstance as CompanyForm).created.emit();

    expect(reload).toHaveBeenCalledTimes(1);
  });
});
