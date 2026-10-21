import { Component, viewChild } from '@angular/core';
import { CompanyDirectory } from '../company-directory/company-directory';
import { CompanyForm } from '../company-form/company-form';

/** Signed-in screen: the add form next to the directory. A new company refreshes the directory. */
@Component({
  selector: 'app-companies-page',
  imports: [CompanyForm, CompanyDirectory],
  templateUrl: './companies-page.html',
  styleUrl: './companies-page.css'
})
export class CompaniesPage {
  protected directory = viewChild.required(CompanyDirectory);
}
