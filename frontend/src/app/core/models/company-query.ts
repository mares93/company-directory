import { Sort } from './sort';

export interface CompanyQuery {
  name: string;
  domain: string;
  sort: Sort;
  page: number;
  pageSize: number;
}
