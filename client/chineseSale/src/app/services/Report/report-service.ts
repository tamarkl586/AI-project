import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GiftPurchasesSummaryModel } from '../../models/Cart/GiftPurchasesSummaryModel';
import { PurchaserDetailsModel } from '../../models/Cart/PurchaserDetailsModel';
import { TopGiftStatsModel } from '../../models/Cart/TopGiftStatsModel';
import { GiftWinnerReportModel } from '../../models/Report/GiftWinnerReportModel';
import { RevenueSummaryModel } from '../../models/Report/RevenueSummaryModel';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private baseUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  getWinners(): Observable<GiftWinnerReportModel[]> {
    return this.http.get<GiftWinnerReportModel[]>(`${this.baseUrl}/winners`);
  }

  getRevenueSummary(): Observable<RevenueSummaryModel> {
    return this.http.get<RevenueSummaryModel>(`${this.baseUrl}/revenue-summary`);
  }

  getPurchasesByGift(giftId: number): Observable<GiftPurchasesSummaryModel> {
    return this.http.get<GiftPurchasesSummaryModel>(`${this.baseUrl}/gift/${giftId}/purchases`);
  }

  getAllPurchasers(): Observable<PurchaserDetailsModel[]> {
    return this.http.get<PurchaserDetailsModel[]>(`${this.baseUrl}/purchasers`);
  }

  getPurchaserDetails(userId: number): Observable<PurchaserDetailsModel> {
    return this.http.get<PurchaserDetailsModel>(`${this.baseUrl}/purchaser/${userId}`);
  }

  getTopGift(criteria?: string): Observable<TopGiftStatsModel> {
    const suffix = criteria ? `?criteria=${encodeURIComponent(criteria)}` : '';
    return this.http.get<TopGiftStatsModel>(`${this.baseUrl}/top-gift${suffix}`);
  }
}
