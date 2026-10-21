import { pageWindow, splitProtocol } from './helpers';

describe('pageWindow', () => {
  it('lists every page when there are few', () => {
    expect(pageWindow(1, 1)).toEqual([1]);
    expect(pageWindow(7, 4)).toEqual([1, 2, 3, 4, 5, 6, 7]);
  });

  it('collapses the middle with an ellipsis', () => {
    expect(pageWindow(20, 10)).toEqual([1, -1, 9, 10, 11, -1, 20]);
  });

  it('has no ellipsis on the side where the window touches the edge', () => {
    expect(pageWindow(20, 1)).toEqual([1, 2, -1, 20]);
    expect(pageWindow(20, 2)).toEqual([1, 2, 3, -1, 20]);
    expect(pageWindow(20, 20)).toEqual([1, -1, 19, 20]);
    expect(pageWindow(20, 19)).toEqual([1, -1, 18, 19, 20]);
  });
});

describe('splitProtocol', () => {
  it('keeps the current protocol when none is typed', () => {
    expect(splitProtocol('www.acme.com', 'https://')).toEqual({ protocol: 'https://', rest: 'www.acme.com' });
  });

  it('moves a pasted protocol to the dropdown, whatever its case', () => {
    expect(splitProtocol('http://acme.com', 'https://')).toEqual({ protocol: 'http://', rest: 'acme.com' });
    expect(splitProtocol('  HTTPS://Acme.com/x ', 'http://')).toEqual({ protocol: 'https://', rest: 'Acme.com/x' });
  });

  it('does not touch other schemes', () => {
    expect(splitProtocol('ftp://acme.com', 'https://')).toEqual({ protocol: 'https://', rest: 'ftp://acme.com' });
  });
});
