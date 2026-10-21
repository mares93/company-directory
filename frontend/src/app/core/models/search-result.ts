import { Company } from './company';

export interface SearchResult {
  company: Company;
  relevance: number;
}
