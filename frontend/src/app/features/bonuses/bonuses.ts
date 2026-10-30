import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Subscription, finalize } from 'rxjs';
import { Bonus, BonusFilter } from '../../core/models/bonus';
import { BonusStates, BonusTypes, MasterStateCodes } from '../../core/models/catalog';
import { PagedResponse } from '../../core/models/paged-response';
import { BonusApi } from '../../core/services/bonus-api';
import { CatalogApi } from '../../core/services/catalog-api';
import { Notification } from '../../core/services/notification';
import { ConfirmDialog } from '../../shared/confirm-dialog/confirm-dialog';
import { BonusForm } from './bonus-form/bonus-form';

const PAGE_SIZE = 10;
const VISIBLE_PAGES = 5;

type RowAction = 'cancel' | 'delete';

const ACTION_TEXT: Record<RowAction, { title: string; message: string; confirm: string; done: string }> = {
  cancel: {
    title: 'Anular bono',
    message: '¿Seguro que deseas anular este bono? Esta acción no se puede deshacer.',
    confirm: 'Anular',
    done: 'El bono fue anulado.',
  },
  delete: {
    title: 'Eliminar bono',
    message: '¿Seguro que deseas eliminar este bono? Dejará de aparecer en el listado.',
    confirm: 'Eliminar',
    done: 'El bono fue eliminado.',
  },
};

/** Pantalla de bonos de desempeño: filtros, listado paginado desde el servidor y registro. */
@Component({
  selector: 'app-bonuses',
  imports: [ReactiveFormsModule, DecimalPipe, BonusForm, ConfirmDialog],
  templateUrl: './bonuses.html',
  styleUrl: './bonuses.css',
})
export class Bonuses {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly bonusApi = inject(BonusApi);
  private readonly catalogApi = inject(CatalogApi);
  private readonly notification = inject(Notification);

  protected readonly BonusStates = BonusStates;
  protected readonly BonusTypes = BonusTypes;

  protected readonly departments = toSignal(this.catalogApi.getDepartments(), { initialValue: [] });
  protected readonly bonusStates = toSignal(this.catalogApi.getStates(MasterStateCodes.Bonus), { initialValue: [] });

  protected readonly filterForm = this.fb.group({
    departmentCode: '',
    employeeNumber: this.fb.control<number | null>(null),
    period: '',
    stateCode: '',
  });

  protected readonly page = signal<PagedResponse<Bonus> | null>(null);
  protected readonly loading = signal(false);
  protected readonly showForm = signal(false);
  protected readonly pending = signal<{ action: RowAction; bonus: Bonus } | null>(null);
  protected readonly pendingText = computed(() => {
    const pending = this.pending();
    return pending ? ACTION_TEXT[pending.action] : null;
  });

  /** Números de página visibles, centrados en la página actual. */
  protected readonly pageNumbers = computed(() => {
    const page = this.page();
    if (!page) return [];
    const first = Math.max(1, Math.min(page.pageNumber - Math.floor(VISIBLE_PAGES / 2), page.totalPages - VISIBLE_PAGES + 1));
    const last = Math.min(page.totalPages, first + VISIBLE_PAGES - 1);
    return Array.from({ length: last - first + 1 }, (_, i) => first + i);
  });

  /** Los filtros se aplican al pulsar Buscar, no mientras se escribe. */
  private appliedFilter: BonusFilter = this.filterForm.getRawValue();
  private loadSubscription?: Subscription;

  constructor() {
    this.load(1);
  }

  protected search(): void {
    this.appliedFilter = this.filterForm.getRawValue();
    this.load(1);
  }

  protected clear(): void {
    this.filterForm.reset();
    this.search();
  }

  protected goToPage(pageIndex: number): void {
    const totalPages = this.page()?.totalPages ?? 1;
    if (pageIndex >= 1 && pageIndex <= totalPages) this.load(pageIndex);
  }

  protected canAct(bonus: Bonus): boolean {
    return bonus.stateCode === BonusStates.Registered;
  }

  protected askConfirmation(action: RowAction, bonus: Bonus): void {
    this.pending.set({ action, bonus });
  }

  protected confirmAction(): void {
    const pending = this.pending();
    if (!pending) return;

    const request =
      pending.action === 'cancel' ? this.bonusApi.cancel(pending.bonus.bonusId) : this.bonusApi.delete(pending.bonus.bonusId);

    this.notification.clear();
    this.pending.set(null);
    request.subscribe({
      next: () => {
        this.notification.show('success', ACTION_TEXT[pending.action].done);
        this.load(this.page()?.pageNumber ?? 1);
      },
      error: () => this.load(this.page()?.pageNumber ?? 1), // El interceptor ya muestra el error; se refresca por si el estado cambió.
    });
  }

  protected onSaved(): void {
    this.showForm.set(false);
    this.notification.show('success', 'El bono fue registrado.');
    this.load(1);
  }

  private load(pageIndex: number): void {
    this.loadSubscription?.unsubscribe();
    this.loading.set(true);
    this.loadSubscription = this.bonusApi
      .getPage(this.appliedFilter, pageIndex, PAGE_SIZE)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (page) => {
          // Si eliminar dejó vacía la última página, se retrocede a la última que exista.
          if (page.data.length === 0 && page.totalPages > 0 && pageIndex > page.totalPages) {
            this.load(page.totalPages);
            return;
          }
          this.page.set(page);
        },
        error: () => undefined, // El interceptor ya muestra el error.
      });
  }
}
