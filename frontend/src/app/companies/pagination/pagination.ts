import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { pageWindow } from '../../core/helpers';

/** "Showing 1-5 of 12", the rows-per-page selector and the page buttons. It only reports what the user picked. */
@Component({
  selector: 'app-pagination',
  imports: [FormsModule],
  templateUrl: './pagination.html',
  styleUrl: './pagination.css'
})
export class Pagination {
  page = input.required<number>();
  pageSize = input.required<number>();
  total = input.required<number>();

  pageChange = output<number>();
  pageSizeChange = output<number>();

  protected totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  protected rangeStart = computed(() => (this.total() ? (this.page() - 1) * this.pageSize() + 1 : 0));
  protected rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.total()));
  protected buttons = computed(() => pageWindow(this.totalPages(), this.page()));

  goTo(p: number) { this.pageChange.emit(Math.min(Math.max(1, p), this.totalPages())); }
}
