import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Observable, finalize, merge, startWith } from 'rxjs';
import { BonusListItem, CreateBonusCommand, EmployeeBonus } from '../../core/models/bonus';
import { BonusStates, BonusTypes, MasterStateCodes } from '../../core/models/catalog';
import { BonusApi } from '../../core/services/bonus-api';
import { CatalogApi } from '../../core/services/catalog-api';
import { Notification } from '../../core/services/notification';

type ConfirmationAction = 'pay' | 'cancel' | 'delete';

interface Confirmation {
  action: ConfirmationAction;
  bonus: BonusListItem;
  title: string;
  message: string;
  confirmText: string;
  danger: boolean;
}

interface BonusPreview {
  grossAmount: number;
  taxAmount: number;
  netAmount: number;
}

@Component({
  selector: 'app-bonuses',
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './bonuses.html',
  styleUrl: './bonuses.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Bonuses {
  private readonly catalogApi = inject(CatalogApi);
  private readonly bonusApi = inject(BonusApi);
  private readonly notification = inject(Notification);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly BonusStates = BonusStates;
  protected readonly BonusTypes = BonusTypes;
  protected readonly pageSize = 10;
  protected readonly currentPeriod = this.getCurrentPeriod();
  private readonly notFuturePeriod: ValidatorFn = (
    control: AbstractControl,
  ): ValidationErrors | null =>
    !control.value || control.value <= this.currentPeriod ? null : { futurePeriod: true };

  protected readonly departments = toSignal(this.catalogApi.getDepartments(), { initialValue: [] });
  protected readonly bonusStates = toSignal(this.catalogApi.getStates(MasterStateCodes.Bonus), {
    initialValue: [],
  });
  protected readonly bonusTypes = toSignal(this.catalogApi.getBonusTypes(), { initialValue: [] });
  protected readonly bonusReasons = toSignal(this.catalogApi.getBonusReasons(), {
    initialValue: [],
  });

  protected readonly filterForm = this.formBuilder.group({
    departmentCode: [''],
    employeeNumber: [null as number | null, [Validators.min(1)]],
    period: ['', this.notFuturePeriod],
    stateCode: [''],
  });

  protected readonly registerForm = this.formBuilder.group({
    departmentCode: ['', Validators.required],
    employeeNumber: [null as number | null, [Validators.required, Validators.min(1)]],
    period: [this.currentPeriod, [Validators.required, this.notFuturePeriod]],
    bonusTypeId: [null as number | null, Validators.required],
    value: [null as number | null, [Validators.required, Validators.min(0.01)]],
    reasonCode: ['', Validators.required],
  });

  protected readonly rows = signal<BonusListItem[]>([]);
  protected readonly totalRecords = signal(0);
  protected readonly currentPage = signal(1);
  protected readonly loading = signal(false);
  protected readonly modalOpen = signal(false);
  protected readonly selectedEmployee = signal<EmployeeBonus | null>(null);
  protected readonly searchingEmployee = signal(false);
  protected readonly saving = signal(false);
  protected readonly confirmation = signal<Confirmation | null>(null);
  protected readonly actionLoading = signal(false);

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalRecords() / this.pageSize)),
  );

  protected readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const start = Math.max(1, Math.min(current - 2, total - 4));
    const end = Math.min(total, start + 4);
    return Array.from({ length: end - start + 1 }, (_, index) => start + index);
  });

  private readonly registerValue = toSignal(
    this.registerForm.valueChanges.pipe(startWith(this.registerForm.getRawValue())),
    { initialValue: this.registerForm.getRawValue() },
  );

  protected readonly valueLabel = computed(() =>
    this.registerValue().bonusTypeId === BonusTypes.SalaryPercentage
      ? 'Porcentaje del sueldo'
      : 'Monto fijo',
  );

  protected readonly valueSuffix = computed(() =>
    this.registerValue().bonusTypeId === BonusTypes.SalaryPercentage ? '%' : 'S/',
  );

  protected readonly preview = computed<BonusPreview | null>(() => {
    const employee = this.selectedEmployee();
    const form = this.registerValue();
    const value = Number(form.value);

    if (!employee || !form.bonusTypeId || !Number.isFinite(value) || value <= 0) return null;

    const gross =
      form.bonusTypeId === BonusTypes.SalaryPercentage
        ? (employee.monthlySalary * value) / 100
        : value;
    const grossAmount = this.roundHalfUp(gross);
    const taxAmount = this.roundHalfUp((grossAmount * employee.taxRate) / 100);

    return {
      grossAmount,
      taxAmount,
      netAmount: this.roundHalfUp(grossAmount - taxAmount),
    };
  });

  constructor() {
    merge(
      this.registerForm.controls.departmentCode.valueChanges,
      this.registerForm.controls.employeeNumber.valueChanges,
    )
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.selectedEmployee.set(null);
        this.configureValueValidation();
      });

    this.registerForm.controls.bonusTypeId.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.configureValueValidation());

    this.loadPage(1);
  }

  protected search(): void {
    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      return;
    }
    this.loadPage(1);
  }

  protected clearFilters(): void {
    this.filterForm.reset({
      departmentCode: '',
      employeeNumber: null,
      period: '',
      stateCode: '',
    });
    this.loadPage(1);
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.currentPage()) return;
    this.loadPage(page);
  }

  protected openRegisterModal(): void {
    this.registerForm.reset({
      departmentCode: '',
      employeeNumber: null,
      period: this.currentPeriod,
      bonusTypeId: null,
      value: null,
      reasonCode: '',
    });
    this.selectedEmployee.set(null);
    this.modalOpen.set(true);
  }

  protected closeRegisterModal(): void {
    if (this.saving()) return;
    this.modalOpen.set(false);
  }

  protected findEmployee(): void {
    const departmentControl = this.registerForm.controls.departmentCode;
    const employeeControl = this.registerForm.controls.employeeNumber;
    departmentControl.markAsTouched();
    employeeControl.markAsTouched();

    if (departmentControl.invalid || employeeControl.invalid) return;

    this.searchingEmployee.set(true);
    this.selectedEmployee.set(null);
    this.bonusApi
      .getEmployee(departmentControl.value!, employeeControl.value!)
      .pipe(finalize(() => this.searchingEmployee.set(false)))
      .subscribe({
        next: (employee) => {
          this.selectedEmployee.set(employee);
          this.configureValueValidation();
        },
        error: () => undefined,
      });
  }

  protected saveBonus(): void {
    this.registerForm.markAllAsTouched();
    const employee = this.selectedEmployee();

    if (this.registerForm.invalid || !employee) {
      if (!employee)
        this.notification.show('warning', 'Busca y valida al empleado antes de guardar.');
      return;
    }

    const value = this.registerForm.getRawValue();
    const command: CreateBonusCommand = {
      departmentCode: value.departmentCode!,
      employeeNumber: value.employeeNumber!,
      period: value.period!,
      bonusTypeId: value.bonusTypeId!,
      value: value.value!,
      reasonCode: value.reasonCode!,
    };

    this.saving.set(true);
    this.bonusApi
      .create(command)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.modalOpen.set(false);
          this.notification.show('success', 'El bono se registró correctamente.');
          this.loadPage(1);
        },
        error: () => undefined,
      });
  }

  protected requestAction(action: ConfirmationAction, bonus: BonusListItem): void {
    const config: Record<ConfirmationAction, Omit<Confirmation, 'action' | 'bonus'>> = {
      pay: {
        title: 'Confirmar pago',
        message: `Se distribuirá el bruto de ${bonus.employeeName} entre sus proyectos y el bono pasará a Pagado.`,
        confirmText: 'Pagar bono',
        danger: false,
      },
      cancel: {
        title: 'Anular bono',
        message: `El bono de ${bonus.employeeName} quedará anulado y permitirá registrar otro para el mismo periodo.`,
        confirmText: 'Anular bono',
        danger: true,
      },
      delete: {
        title: 'Eliminar registro',
        message: `El bono de ${bonus.employeeName} dejará de mostrarse. La eliminación será lógica y conservará su auditoría.`,
        confirmText: 'Eliminar',
        danger: true,
      },
    };

    this.confirmation.set({ action, bonus, ...config[action] });
  }

  protected closeConfirmation(): void {
    if (!this.actionLoading()) this.confirmation.set(null);
  }

  protected executeAction(): void {
    const confirmation = this.confirmation();
    if (!confirmation) return;

    let operation: Observable<unknown>;
    let successMessage: string;

    switch (confirmation.action) {
      case 'pay':
        operation = this.bonusApi.pay(confirmation.bonus.bonusId);
        successMessage = 'El bono se pagó y distribuyó por proyecto correctamente.';
        break;
      case 'cancel':
        operation = this.bonusApi.cancel(confirmation.bonus.bonusId);
        successMessage = 'El bono se anuló correctamente.';
        break;
      case 'delete':
        operation = this.bonusApi.delete(confirmation.bonus.bonusId);
        successMessage = 'El bono se eliminó correctamente.';
        break;
    }

    this.actionLoading.set(true);
    operation.pipe(finalize(() => this.actionLoading.set(false))).subscribe({
      next: () => {
        const shouldGoBack =
          confirmation.action === 'delete' && this.rows().length === 1 && this.currentPage() > 1;
        this.confirmation.set(null);
        this.notification.show('success', successMessage);
        this.loadPage(shouldGoBack ? this.currentPage() - 1 : this.currentPage());
      },
      error: () => undefined,
    });
  }

  protected formatPeriod(period: string): string {
    const [year, month] = period.slice(0, 7).split('-');
    return `${month}/${year}`;
  }

  protected isRegistered(bonus: BonusListItem): boolean {
    return bonus.stateCode === BonusStates.Registered;
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.confirmation()) this.closeConfirmation();
    else if (this.modalOpen()) this.closeRegisterModal();
  }

  private loadPage(page: number): void {
    const filters = this.filterForm.getRawValue();
    this.loading.set(true);

    this.bonusApi
      .getPaged({
        departmentCode: filters.departmentCode || undefined,
        employeeNumber: filters.employeeNumber || undefined,
        period: filters.period || undefined,
        stateCode: filters.stateCode || undefined,
        pageIndex: page,
        pageSize: this.pageSize,
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.rows.set(response.data);
          this.totalRecords.set(response.totalRecords);
          this.currentPage.set(response.pageNumber);
        },
        error: () => undefined,
      });
  }

  private configureValueValidation(): void {
    const valueControl = this.registerForm.controls.value;
    const bonusType = this.registerForm.controls.bonusTypeId.value;
    const employee = this.selectedEmployee();
    const validators = [Validators.required, Validators.min(0.01)];

    if (bonusType === BonusTypes.SalaryPercentage) validators.push(Validators.max(100));
    if (bonusType === BonusTypes.FixedAmount && employee) {
      validators.push(Validators.max(employee.monthlySalary));
    }

    valueControl.setValidators(validators);
    valueControl.updateValueAndValidity({ emitEvent: false });
  }

  private roundHalfUp(value: number): number {
    const normalized = Number(value.toFixed(10));
    return Math.floor(normalized * 100 + 0.5) / 100;
  }

  private getCurrentPeriod(): string {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  }
}
