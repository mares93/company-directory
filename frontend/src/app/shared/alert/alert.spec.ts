import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Alert } from './alert';
import { AlertKind } from './alert-kind';

// The alert shows projected text, so it is tested through a small host.
@Component({
  imports: [Alert],
  template: `<app-alert [kind]="kind()">Something happened</app-alert>`
})
class Host {
  kind = signal<AlertKind>('ok');
}

describe('Alert', () => {
  let fixture: ComponentFixture<Host>;
  let el: HTMLElement;

  beforeEach(() => {
    fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    el = fixture.nativeElement;
  });

  it('shows the projected text', () => {
    expect(el.querySelector('.state')?.textContent).toContain('Something happened');
  });

  it('styles and iconizes each kind', () => {
    const cases: [AlertKind, string, string][] = [
      ['ok', 'state-ok', 'bi-check-circle-fill'],
      ['error', 'state-err', 'bi-exclamation-circle-fill'],
      ['empty', 'state-empty', 'bi-search']
    ];
    for (const [kind, cls, icon] of cases) {
      fixture.componentInstance.kind.set(kind);
      fixture.detectChanges();
      expect(el.querySelector('.state')?.classList).withContext(kind).toContain(cls);
      expect(el.querySelector('i')?.classList).withContext(kind).toContain(icon);
    }
  });

  it('is announced to screen readers', () => {
    expect(el.querySelector('.state')?.getAttribute('role')).toBe('alert');
  });
});
