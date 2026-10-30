export interface BonusListItem {
  bonusId: number;
  stateCode: string;
  stateDescription: string;
  period: string;
  departmentCode: string;
  departmentName: string;
  employeeNumber: number;
  employeeName: string;
  bonusTypeId: number;
  bonusTypeDescription: string;
  enteredValue: number;
  reasonCode: string;
  reasonDescription: string;
  grossAmount: number;
  taxAmount: number;
  netAmount: number;
  createdAt: string;
}

export interface EmployeeBonus {
  departmentCode: string;
  employeeNumber: number;
  fullName: string;
  hireDate: string;
  monthlySalary: number;
  taxRate: number;
}

export interface BonusFilter {
  departmentCode?: string;
  employeeNumber?: number;
  period?: string;
  stateCode?: string;
  pageIndex: number;
  pageSize: number;
}

export interface CreateBonusCommand {
  departmentCode: string;
  employeeNumber: number;
  period: string;
  bonusTypeId: number;
  value: number;
  reasonCode: string;
}

export interface BonusMutation {
  bonusId: number;
  stateCode: string;
}

export interface ProjectAllocation {
  employeeProjectHoursId: number;
  projectName: string;
  hours: number;
  allocatedAmount: number;
}

export interface PayBonusResult extends BonusMutation {
  grossAmount: number;
  allocations: ProjectAllocation[];
}
