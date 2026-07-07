import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DonorModel } from '../../models/Donor/DonorModel';
import { DonorCreateModel } from '../../models/Donor/DonorCreateModel';
import { DonorUpdateModel } from '../../models/Donor/DonorUpdateModel';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DonorService {

  private baseUrl = `${environment.apiUrl}/donor`;

  constructor(private http: HttpClient) { }

  getAll(): Observable<DonorModel[]> {
    return this.http.get<DonorModel[]>(this.baseUrl);
  }

  getById(id: number): Observable<DonorModel> {
    return this.http.get<DonorModel>(`${this.baseUrl}/${id}`);
  }

  add(dto: DonorCreateModel): Observable<any> {
    return this.http.post(this.baseUrl, dto);
  }

  update(id: number, dto: DonorUpdateModel): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }

  search(donorName?: string, giftName?: string, email?: string): Observable<DonorModel[]> {
    let params = new HttpParams();

    if (donorName) {
      params = params.append('donorName', donorName);
    }
    if (giftName) {
      params = params.append('giftName', giftName);
    }
    if (email) {
      params = params.append('email', email);
    }

    return this.http.get<DonorModel[]>(`${this.baseUrl}/search`, { params });
  }

}