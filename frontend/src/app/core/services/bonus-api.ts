import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BonusFilter,
  BonusListItem,
  BonusMutation,
  CreateBonusCommand,
  EmployeeBonus,
  PayBonusResult,
} from '../models/bonus';
import { PagedResponse } from '../models/paged-response';

@Injectable({ providedIn: 'root' })
export class BonusApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/Bonus`;

  getPaged(filter: BonusFilter): Observable<PagedResponse<BonusListItem>> {
    let params = new HttpParams()
      .set('pageIndex', filter.pageIndex)
      .set('pageSize', filter.pageSize);

    if (filter.departmentCode) params = params.set('departmentCode', filter.departmentCode);
    if (filter.employeeNumber) params = params.set('employeeNumber', filter.employeeNumber);
    if (filter.period) params = params.set('period', filter.period);
    if (filter.stateCode) params = params.set('stateCode', filter.stateCode);

    return this.http.get<PagedResponse<BonusListItem>>(this.baseUrl, { params });
  }

  getEmployee(departmentCode: string, employeeNumber: number): Observable<EmployeeBonus> {
    return this.http.get<EmployeeBonus>(
      `${this.baseUrl}/employee/${encodeURIComponent(departmentCode)}/${employeeNumber}`,
    );
  }

  create(command: CreateBonusCommand): Observable<BonusMutation> {
    return this.http.post<BonusMutation>(this.baseUrl, command);
  }

  cancel(bonusId: number): Observable<BonusMutation> {
    return this.http.put<BonusMutation>(`${this.baseUrl}/${bonusId}/cancel`, null);
  }

  delete(bonusId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${bonusId}`);
  }

  pay(bonusId: number): Observable<PayBonusResult> {
    return this.http.post<PayBonusResult>(`${this.baseUrl}/${bonusId}/pay`, null);
  }
}
