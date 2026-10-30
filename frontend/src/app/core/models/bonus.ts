export interface Employee {
  departmentCode: string;
  employeeNumber: number;
  fullName: string;
  hireDate: string;
  monthlySalary: number;
  taxRate: number;
}

export interface Bonus {
  bonusId: number;
  stateCode: string;
  stateDescription: string;
  /** Formato YYYY-MM. */
  period: string;
  departmentCode: string;
  departmentName: string;
  employeeNumber: number;
  fullName: string;
  bonusTypeId: number;
  bonusTypeDescription: string;
  /** Monto fijo o porcentaje, según bonusTypeId. */
  bonusValue: number;
  reasonCode: string;
  reasonDescription: string;
  grossAmount: number;
  taxAmount: number;
  netAmount: number;
}

export interface BonusFilter {
  departmentCode: string;
  employeeNumber: number | null;
  period: string;
  stateCode: string;
}

export interface CreateBonusRequest {
  departmentCode: string;
  employeeNumber: number;
  period: string;
  bonusTypeId: number;
  bonusValue: number;
  reasonCode: string;
}
