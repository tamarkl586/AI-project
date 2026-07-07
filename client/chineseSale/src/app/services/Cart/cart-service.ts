import { Injectable } from '@angular/core';
import { PurchaserDetailsModel } from '../../models/Cart/PurchaserDetailsModel';
import { environment } from '../../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { CartItemModel } from '../../models/Cart/CartItemModel';
import { map, Observable } from 'rxjs';
import { AddToCartModel } from '../../models/Cart/AddToCartModel';
import { GiftPurchasesSummaryModel } from '../../models/Cart/GiftPurchasesSummaryModel';
import { TopGiftStatsModel } from '../../models/Cart/TopGiftStatsModel';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private apiUrl = `${environment.apiUrl}/cart`;
  private webOrdersUrl = `${environment.apiUrl}/web/orders`;
  private reportsUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  getMyCart(): Observable<CartItemModel[]> {
    return this.http.get<WebOrderSummaryModel>(`${this.webOrdersUrl}/me`).pipe(
      map(summary => (summary.items ?? []).map(item => ({
        ...item.cartItem,
        giftName: item.gift?.name ?? item.cartItem.giftName,
        giftDescription: item.gift?.description ?? item.cartItem.giftDescription,
        giftPicture: item.gift?.picture ?? item.cartItem.giftPicture,
        price: item.gift?.price ?? item.cartItem.price,
        isDrawn: item.gift?.winnerId != null ? true : item.cartItem.isDrawn,
      })))
    );
  }

  add(dto: AddToCartModel): Observable<any> {
    return this.http.post(this.apiUrl, dto);
  }

  updateQuantity(cartId: number, newQuantity: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/${cartId}`, newQuantity);
  }

  remove(cartId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${cartId}`);
  }

  purchase(): Observable<any> {
    return this.http.post(`${this.apiUrl}/purchase`, {});
  }

  clearCart(): Observable<any> {
    return this.http.delete(`${this.apiUrl}/clear`);
  }

  // --- Manager endpoints ---

  getPurchasesByGift(giftId: number): Observable<GiftPurchasesSummaryModel> {
    return this.http.get<GiftPurchasesSummaryModel>(`${this.reportsUrl}/gift/${giftId}/purchases`);
  }

  getAllPurchasers(): Observable<PurchaserDetailsModel[]> {
    return this.http.get<PurchaserDetailsModel[]>(`${this.reportsUrl}/purchasers`);
  }

  getPurchaserDetails(userId: number): Observable<PurchaserDetailsModel> {
    return this.http.get<PurchaserDetailsModel>(`${this.reportsUrl}/purchaser/${userId}`);
  }

  getTopGift(criteria?: string): Observable<TopGiftStatsModel> {
    let params = new HttpParams();
    if (criteria) params = params.append('criteria', criteria);
    return this.http.get<TopGiftStatsModel>(`${this.reportsUrl}/top-gift`, { params });
  }
}

interface WebOrderSummaryModel {
  items: WebOrderItemModel[];
}

interface WebOrderItemModel {
  cartItem: CartItemModel;
  gift?: WebGiftModel;
}

interface WebGiftModel {
  winnerId?: number | null;
  name: string;
  description: string;
  picture: string;
  price: number;
}
