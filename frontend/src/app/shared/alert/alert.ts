import { Component, computed, input } from '@angular/core';
import { AlertKind } from './alert-kind';

const ICONS: Record<AlertKind, string> = {
  ok: 'bi-check-circle-fill',
  error: 'bi-exclamation-circle-fill',
  empty: 'bi-search'
};

/** Success, error and "nothing found" messages, with the text projected inside. */
@Component({
  selector: 'app-alert',
  templateUrl: './alert.html',
  styleUrl: './alert.css'
})
export class Alert {
  kind = input.required<AlertKind>();
  protected icon = computed(() => ICONS[this.kind()]);
}
