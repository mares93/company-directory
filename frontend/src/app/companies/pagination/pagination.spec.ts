import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Pagination } from './pagination';

describe('Pagination', () => {
  let fixture: ComponentFixture<Pagination>;
  let el: HTMLElement;

  function setup(page: number, pageSize: number, total: number) {
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', pageSize);
    fixture.componentRef.setInput('total', total);
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Pagination] });
    fixture = TestBed.createComponent(Pagination);
    el = fixture.nativeElement;
  });

  it('shows the range and no page buttons when everything fits on one page', () => {
    setup(1, 5, 3);
    expect(el.textContent).toContain('Showing 1-3 of 3');
    expect(el.querySelector('.pages')).toBeNull();
  });

  it('shows 0-0 when there are no results', () => {
    setup(1, 5, 0);
    expect(el.textContent).toContain('Showing 0-0 of 0');
  });

  it('shows the range of the last, partial page and marks the current page', () => {
    setup(3, 5, 12);
    expect(el.textContent).toContain('Showing 11-12 of 12');
    expect(el.querySelector('.page-btn.active')?.textContent?.trim()).toBe('3');
  });

  it('disables previous on the first page and next on the last', () => {
    setup(1, 5, 12);
    const [prev] = Array.from(el.querySelectorAll<HTMLButtonElement>('.page-btn'));
    expect(prev.disabled).toBeTrue();

    setup(3, 5, 12);
    const buttons = Array.from(el.querySelectorAll<HTMLButtonElement>('.page-btn'));
    expect(buttons[buttons.length - 1].disabled).toBeTrue();
  });

  it('collapses many pages with an ellipsis', () => {
    setup(10, 5, 100);
    expect(el.querySelectorAll('.ellipsis').length).toBe(2);
  });

  it('emits the page that was clicked', () => {
    setup(1, 5, 12);
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe(p => emitted.push(p));

    const two = Array.from(el.querySelectorAll<HTMLButtonElement>('.page-btn')).find(b => b.textContent?.trim() === '2')!;
    two.click();

    expect(emitted).toEqual([2]);
  });

  it('never asks for a page outside 1..last', () => {
    setup(3, 5, 12);
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe(p => emitted.push(p));
    fixture.componentInstance.goTo(99);
    fixture.componentInstance.goTo(0);
    expect(emitted).toEqual([3, 1]);
  });

  it('emits the new page size as a number', () => {
    setup(1, 5, 30);
    const emitted: number[] = [];
    fixture.componentInstance.pageSizeChange.subscribe(s => emitted.push(s));

    const select = el.querySelector('select') as HTMLSelectElement;
    select.value = select.options[2].value; // 20
    select.dispatchEvent(new Event('change'));

    expect(emitted).toEqual([20]);
  });
});
