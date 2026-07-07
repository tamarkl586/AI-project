import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { GiftModel } from '../../models/Gift/GiftModel';
import { CreateGiftModel } from '../../models/Gift/CreateGiftModel';
import { GiftUpdateModel } from '../../models/Gift/GiftUpdateModel';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class GiftService {
  private baseUrl = `${environment.apiUrl}/gift`;

  // ניהול המצב של המתנות בזרם (Stream)
  private giftsSubject = new BehaviorSubject<GiftModel[]>([]);
  public gifts$ = this.giftsSubject.asObservable();
  
  constructor(private http: HttpClient) { }

  // פונקציה לרענון הנתונים מול השרת
  refreshGifts() {
    this.http.get<GiftModel[]>(this.baseUrl).subscribe({
      next: (data) => this.giftsSubject.next(data),
      error: (err) => console.error('Error fetching gifts', err)
    });
  }

  getAll(): Observable<GiftModel[]> {
    return this.http.get<GiftModel[]>(this.baseUrl);
  }

  getById(id: number): Observable<GiftModel> {
    return this.http.get<GiftModel>(`${this.baseUrl}/${id}`);
  }

  add(gift: CreateGiftModel): Observable<any> {
    return this.http.post(this.baseUrl, gift).pipe(
      tap(() => this.refreshGifts()) // זה מה שגורם לרשימה להתעדכן מייד!
    );
  }

  update(id: number, gift: GiftUpdateModel): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}`, gift).pipe(
      tap(() => this.refreshGifts())
    );
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`).pipe(
      tap(() => this.refreshGifts())
    );
  }

  search(category?: string, price?: number): Observable<GiftModel[]> {
    let params = new HttpParams();

    if (category) {
      params = params.append('category', category);
    }
    if (price) {
      params = params.append('price', price.toString());
    }

    return this.http.get<GiftModel[]>(`${this.baseUrl}/search`, { params });
  }

  managerSearch(giftName?: string, donorName?: string, buyersCount?: number): Observable<GiftModel[]> {
    let params = new HttpParams();

    if (giftName) {
      params = params.append('giftName', giftName);
    }
    if (donorName) {
      params = params.append('donorName', donorName);
    }
    if (buyersCount) {
      params = params.append('buyersCount', buyersCount.toString());
    }

    return this.http.get<GiftModel[]>(`${this.baseUrl}/manager/search`, { params });
  }

  drawWinner(id: number): Observable<any> {
    return this.http.post(`${environment.apiUrl}/reports/draw/${id}`, {});
  }
}
