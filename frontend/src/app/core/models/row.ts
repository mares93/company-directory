import { Company } from './company';

/** One table row: a plain listing has no relevance, a name search does. */
export interface Row {
  company: Company;
  relevance?: number;
}
