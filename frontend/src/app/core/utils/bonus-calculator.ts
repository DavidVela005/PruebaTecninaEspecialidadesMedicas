import { BonusTypes } from '../models/catalog';

export interface BonusAmounts {
  gross: number;
  tax: number;
  net: number;
}

export interface BonusValueErrors {
  positive?: true;
  max?: number;
}

/** Reglas del monto/porcentaje: > 0 y, según el tipo, hasta el sueldo mensual o hasta 100. */
export function validateBonusValue(bonusTypeId: number | null, value: number, monthlySalary: number | null): BonusValueErrors | null {
  if (value <= 0) return { positive: true };
  if (bonusTypeId === BonusTypes.SalaryPercentage && value > 100) return { max: 100 };
  if (bonusTypeId === BonusTypes.FixedAmount && monthlySalary !== null && value > monthlySalary) return { max: monthlySalary };
  return null;
}

/** Redondea a 2 decimales con el medio hacia arriba (los montos nunca son negativos). */
function round(amount: number): number {
  return Math.round(Number((amount * 100).toPrecision(15))) / 100;
}

/** Misma fórmula que el backend; solo se usa para la vista previa del modal. */
export function calculateBonus(bonusTypeId: number, value: number, monthlySalary: number, taxRate: number): BonusAmounts {
  const gross = round(bonusTypeId === BonusTypes.FixedAmount ? value : (monthlySalary * value) / 100);
  const tax = round((gross * taxRate) / 100);
  return { gross, tax, net: round(gross - tax) };
}
