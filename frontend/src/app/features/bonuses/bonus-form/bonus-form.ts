import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { Bonus, Employee } from '../../../core/models/bonus';
import { BonusTypes, Department } from '../../../core/models/catalog';
import { BonusApi } from '../../../core/services/bonus-api';
import { CatalogApi } from '../../../core/services/catalog-api';
import { Notification } from '../../../core/services/notification';
import { calculateBonus, validateBonusValue } from '../../../core/utils/bonus-calculator';

/** Modal de registro: busca al empleado, captura el bono y muestra la vista previa del cálculo. */
@Component({
  selector: 'app-bonus-form',
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe],
  templateUrl: './bonus-form.html',
  styleUrl: './bonus-form.css',
})
export class BonusForm {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly bonusApi = inject(BonusApi);
  private readonly catalogApi = inject(CatalogApi);
  private readonly notification = inject(Notification);

  readonly departments = input.required<Department[]>();
  readonly saved = output<Bonus>();
  readonly closed = output<void>();

  protected readonly bonusTypes = toSignal(this.catalogApi.getBonusTypes(), { initialValue: [] });
  protected readonly reasons = toSignal(this.catalogApi.getBonusReasons(), { initialValue: [] });

  protected readonly employee = signal<Employee | null>(null);
  protected readonly searching = signal(false);
  protected readonly saving = signal(false);

  /** Mes actual (YYYY-MM): el periodo no puede ser posterior. */
  protected readonly maxPeriod = currentPeriod();

  protected readonly searchForm = this.fb.group({
    departmentCode: ['', Validators.required],
    employeeNumber: this.fb.control<number | null>(null, [Validators.required, Validators.min(1)]),
  });

  protected readonly bonusForm = this.fb.group({
    period: [this.maxPeriod, [Validators.required, (c: AbstractControl) => (c.value > this.maxPeriod ? { future: true } : null)]],
    bonusTypeId: this.fb.control<number | null>(null, Validators.required),
    bonusValue: this.fb.control<number | null>(null, [Validators.required, (c: AbstractControl) => this.validateValue(c)]),
    reasonCode: ['', Validators.required],
  });

  private readonly formValue = toSignal(this.bonusForm.valueChanges, { initialValue: this.bonusForm.getRawValue() });

  protected readonly valueLabel = computed(() => {
    switch (this.formValue().bonusTypeId) {
      case BonusTypes.FixedAmount:
        return 'Monto';
      case BonusTypes.SalaryPercentage:
        return 'Porcentaje (%)';
      default:
        return 'Monto o porcentaje';
    }
  });

  protected readonly preview = computed(() => {
    const employee = this.employee();
    const { bonusTypeId, bonusValue } = this.formValue();
    if (!employee || bonusTypeId == null || bonusValue == null) return null;
    if (validateBonusValue(bonusTypeId, bonusValue, employee.monthlySalary)) return null;
    return calculateBonus(bonusTypeId, bonusValue, employee.monthlySalary, employee.taxRate);
  });

  constructor() {
    // El monto y el sueldo se validan en conjunto: al cambiar el tipo hay que revalidar el valor.
    this.bonusForm.controls.bonusTypeId.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.bonusForm.controls.bonusValue.updateValueAndValidity());

    // Si cambia el área o el número, el empleado mostrado deja de corresponder.
    this.searchForm.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.employee.set(null));
  }

  protected showError(control: AbstractControl): boolean {
    return control.invalid && (control.touched || control.dirty);
  }

  protected maxValueError(): number | null {
    return this.bonusForm.controls.bonusValue.errors?.['max'] ?? null;
  }

  protected searchEmployee(): void {
    this.searchForm.markAllAsTouched();
    if (this.searchForm.invalid) return;

    const { departmentCode, employeeNumber } = this.searchForm.getRawValue();
    this.notification.clear();
    this.employee.set(null);
    this.searching.set(true);
    this.bonusApi
      .getEmployee(departmentCode, employeeNumber!)
      .pipe(finalize(() => this.searching.set(false)))
      .subscribe({
        next: (employee) => {
          this.employee.set(employee);
          this.bonusForm.controls.bonusValue.updateValueAndValidity();
        },
        error: () => undefined, // El interceptor ya muestra el error.
      });
  }

  protected save(): void {
    const employee = this.employee();
    this.bonusForm.markAllAsTouched();
    if (!employee || this.bonusForm.invalid) return;

    const { period, bonusTypeId, bonusValue, reasonCode } = this.bonusForm.getRawValue();
    this.notification.clear();
    this.saving.set(true);
    this.bonusApi
      .register({
        departmentCode: employee.departmentCode,
        employeeNumber: employee.employeeNumber,
        period,
        bonusTypeId: bonusTypeId!,
        bonusValue: bonusValue!,
        reasonCode,
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (bonus) => this.saved.emit(bonus),
        error: () => undefined, // El interceptor ya muestra el error.
      });
  }

  private validateValue(control: AbstractControl): ValidationErrors | null {
    if (control.value === null) return null;
    const bonusTypeId = control.parent?.get('bonusTypeId')?.value ?? null;
    return validateBonusValue(bonusTypeId, control.value, this.employee()?.monthlySalary ?? null);
  }
}

function currentPeriod(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}
