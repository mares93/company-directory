import { Protocol } from './models/protocol';

/**
 * Page numbers to show: all of them when few, otherwise first, current +-1 and last.
 * A -1 marks an ellipsis between non-adjacent numbers.
 */
export function pageWindow(totalPages: number, current: number): number[] {
  if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1);
  const pages = [...new Set([1, current - 1, current, current + 1, totalPages].filter(p => p >= 1 && p <= totalPages))]
    .sort((a, b) => a - b);
  return pages.flatMap((p, i) => (i > 0 && p - pages[i - 1] > 1 ? [-1, p] : [p]));
}

/** Moves a typed or pasted "http(s)://" prefix out of the URL so the dropdown is the only place it lives. */
export function splitProtocol(value: string, current: Protocol): { protocol: Protocol; rest: string } {
  const m = /^(https?:\/\/)/i.exec(value.trim());
  if (!m) return { protocol: current, rest: value };
  return { protocol: m[1].toLowerCase() as Protocol, rest: value.trim().slice(m[1].length) };
}
