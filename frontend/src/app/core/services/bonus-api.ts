import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Bonus, BonusFilter, CreateBonusRequest, Employee } from '../models/bonus';
import { PagedResponse } from '../models/paged-response';

@Injectable({ providedIn: 'root' })
export class BonusApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/Bonus`;

  getPage(filter: BonusFilter, pageIndex: number, pageSize: number): Observable<PagedResponse<Bonus>> {
    let params = new HttpParams().set('pageIndex', pageIndex).set('pageSize', pageSize);
    if (filter.departmentCode) params = params.set('departmentCode', filter.departmentCode);
    if (filter.employeeNumber !== null) params = params.set('employeeNumber', filter.employeeNumber);
    if (filter.period) params = params.set('period', filter.period);
    if (filter.stateCode) params = params.set('stateCode', filter.stateCode);

    return this.http.get<PagedResponse<Bonus>>(this.baseUrl, { params });
  }

  getEmployee(departmentCode: string, employeeNumber: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.baseUrl}/employee/${departmentCode}/${employeeNumber}`);
  }

  register(request: CreateBonusRequest): Observable<Bonus> {
    return this.http.post<Bonus>(this.baseUrl, request);
  }

  cancel(bonusId: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${bonusId}/cancel`, null);
  }

  delete(bonusId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${bonusId}`);
  }
}
