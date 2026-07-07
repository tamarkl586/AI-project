import { Component, inject, OnInit } from '@angular/core';
import { GiftService } from '../../../services/Gift/gift-service';
import { GiftEdit } from '../gift-edit/gift-edit';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/user/auth-service';
import { CartService } from '../../../services/Cart/cart-service';
import { CategoryService } from '../../../services/Category/category-service';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { GiftModel } from '../../../models/Gift/GiftModel';
import { CategoryModel } from '../../../models/Category/CategoryModel';
import { AddToCartModel } from '../../../models/Cart/AddToCartModel';
import { CartItemModel } from '../../../models/Cart/CartItemModel';

@Component({
  selector: 'app-gift-list',
  standalone: true,
  imports: [CommonModule, GiftEdit, FormsModule, RouterLink],
  templateUrl: './gift-list.html',
  styleUrl: './gift-list.scss',
})
export class GiftList implements OnInit {
  private giftSrv = inject(GiftService);
  private cartSrv = inject(CartService);
  private categorySrv = inject(CategoryService);
  private router = inject(Router);
  authService = inject(AuthService);

  gifts$ = this.giftSrv.gifts$;
  selectedId: number = -1;
  showForm = false;
  categories: CategoryModel[] = [];

  // User filters
  filterCategory = '';
  filterMaxPrice: number | null = null;

  // Manager filters
  mgrFilterName = '';
  mgrFilterDonor = '';
  mgrFilterCategory = '';

  // All gifts cache for client-side filtering
  allGifts: GiftModel[] = [];
  giftNames: string[] = [];
  donorNames: string[] = [];

  // Filtered results
  filteredGifts: GiftModel[] | null = null;

  // Cart messages
  cartMessages: { [giftId: number]: string } = {};

  // Drawn winners (giftId -> { name, email, emailSent })
  drawnWinners: { [giftId: number]: { name: string; email: string; emailSent: boolean } } = {};

  // Per-gift ticket quantities synced with cart
  giftQuantities: { [giftId: number]: number } = {};

  // Map giftId -> CartItemModel for update/remove
  cartItemMap: { [giftId: number]: CartItemModel } = {};

  // Loading state per gift to prevent double clicks
  loading: { [giftId: number]: boolean } = {};

  ngOnInit() {
    this.giftSrv.refreshGifts();
    this.categorySrv.getAll().subscribe(cats => this.categories = cats);
    this.loadCart();

    // Cache gifts and extract unique names/donors for filter dropdowns
    this.gifts$.subscribe(gifts => {
      this.allGifts = gifts;
      this.giftNames = [...new Set(gifts.map(g => g.name))].sort();
      this.donorNames = [...new Set(gifts.map(g => g.donorName).filter(d => !!d))].sort();

      // Keep manager filtered list fresh when underlying gifts data changes.
      if (this.filteredGifts && (this.mgrFilterName || this.mgrFilterCategory || this.mgrFilterDonor)) {
        this.applyManagerFilters();
      }
    });
  }

  isGiftDrawn(gift: GiftModel): boolean {
    return !!gift.winnerId || !!this.drawnWinners[gift.id];
  }

  /** Load user cart and populate quantities */
  loadCart() {
    if (!this.authService.isLoggedIn()) return;
    this.cartSrv.getMyCart().subscribe({
      next: (items) => {
        this.cartItemMap = {};
        this.giftQuantities = {};
        for (const item of items) {
          this.cartItemMap[item.giftId] = item;
          this.giftQuantities[item.giftId] = item.quantity;
        }
      },
      error: () => {}
    });
  }

  onIdChange(newId: number) {
    if (newId === -2) {
      this.closeForm();
      this.giftSrv.refreshGifts();
    } else {
      this.selectedId = newId;
    }
  }

  addGift() {
    this.selectedId = 0;
    this.showForm = true;
  }

  openEdit(id: number) {
    this.selectedId = id;
    this.showForm = true;
  }

  closeForm() {
    this.showForm = false;
    this.selectedId = -1;
  }

  // User search
  userSearch() {
    this.giftSrv.search(
      this.filterCategory || undefined,
      this.filterMaxPrice || undefined
    ).subscribe(res => this.filteredGifts = res);
  }

  clearUserSearch() {
    this.filterCategory = '';
    this.filterMaxPrice = null;
    this.filteredGifts = null;
  }

  // Manager search (client-side filtering via pill dropdowns)
  applyManagerFilters() {
    if (!this.mgrFilterName && !this.mgrFilterCategory && !this.mgrFilterDonor) {
      this.filteredGifts = null;
      return;
    }
    let result = [...this.allGifts];
    if (this.mgrFilterName) {
      result = result.filter(g => g.name === this.mgrFilterName);
    }
    if (this.mgrFilterCategory) {
      result = result.filter(g => g.categoryName === this.mgrFilterCategory);
    }
    if (this.mgrFilterDonor) {
      result = result.filter(g => g.donorName === this.mgrFilterDonor);
    }
    this.filteredGifts = result;
  }

  clearManagerSearch() {
    this.mgrFilterName = '';
    this.mgrFilterDonor = '';
    this.mgrFilterCategory = '';
    this.filteredGifts = null;
  }

  // Draw winner
  drawWinner(giftId: number, event: Event) {
    event.stopPropagation();
    if (confirm('האם להגריל זוכה למתנה זו?')) {
      this.giftSrv.drawWinner(giftId).subscribe({
        next: (res: any) => {
          // Store the winner immediately so the UI can show it
          this.drawnWinners[giftId] = { name: res.winnerName, email: res.winnerEmail, emailSent: res.emailSent };
          // Refresh the gift list to update winnerId from server
          this.giftSrv.refreshGifts();
        },
        error: (err: any) => alert(err.error?.message || 'שגיאה בהגרלה')
      });
    }
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }

  /** Filter by category pill */
  filterByCategory(catName: string) {
    this.filterCategory = catName;
    if (catName) {
      this.giftSrv.search(catName, undefined).subscribe(res => this.filteredGifts = res);
    } else {
      this.filteredGifts = null;
    }
  }

  // Quantity helpers
  getQuantity(giftId: number): number {
    return this.giftQuantities[giftId] || 0;
  }

  /** + button: add 1 ticket to cart */
  incrementQty(gift: GiftModel) {
    if (this.loading[gift.id]) return;
    this.loading[gift.id] = true;

    const dto = new AddToCartModel();
    dto.giftId = gift.id;
    dto.quantity = 1;
    this.cartSrv.add(dto).subscribe({
      next: () => {
        this.giftQuantities[gift.id] = (this.giftQuantities[gift.id] || 0) + 1;
        this.loading[gift.id] = false;
        this.loadCart();
      },
      error: (err: any) => {
        this.loading[gift.id] = false;
        this.showCartMsg(gift.id, err.error?.message || 'שגיאה');
      }
    });
  }

  /** - button: remove 1 ticket from cart */
  decrementQty(gift: GiftModel) {
    const current = this.giftQuantities[gift.id] || 0;
    if (current <= 0 || this.loading[gift.id]) return;
    this.loading[gift.id] = true;

    const cartItem = this.cartItemMap[gift.id];
    if (!cartItem) {
      this.giftQuantities[gift.id] = 0;
      this.loading[gift.id] = false;
      return;
    }

    const newQty = current - 1;
    if (newQty <= 0) {
      this.cartSrv.remove(cartItem.id).subscribe({
        next: () => {
          this.giftQuantities[gift.id] = 0;
          delete this.cartItemMap[gift.id];
          this.loading[gift.id] = false;
        },
        error: (err: any) => {
          this.loading[gift.id] = false;
          this.showCartMsg(gift.id, err.error?.message || 'שגיאה');
        }
      });
    } else {
      this.cartSrv.updateQuantity(cartItem.id, newQty).subscribe({
        next: () => {
          this.giftQuantities[gift.id] = newQty;
          this.loading[gift.id] = false;
          this.loadCart();
        },
        error: (err: any) => {
          this.loading[gift.id] = false;
          this.showCartMsg(gift.id, err.error?.message || 'שגיאה');
        }
      });
    }
  }

  /** Manual qty input (on blur/change) */
  onQtyChange(gift: GiftModel, event: Event) {
    const input = event.target as HTMLInputElement;
    let val = parseInt(input.value, 10);
    if (isNaN(val) || val < 0) val = 0;
    if (val > 100) val = 100;

    const oldQty = this.giftQuantities[gift.id] || 0;
    if (val === oldQty) return;
    this.giftQuantities[gift.id] = val;
    this.loading[gift.id] = true;

    const cartItem = this.cartItemMap[gift.id];

    if (val === 0 && cartItem) {
      this.cartSrv.remove(cartItem.id).subscribe({
        next: () => { delete this.cartItemMap[gift.id]; this.loading[gift.id] = false; },
        error: (err: any) => { this.giftQuantities[gift.id] = oldQty; this.loading[gift.id] = false; this.showCartMsg(gift.id, err.error?.message || 'שגיאה'); }
      });
    } else if (val > 0 && cartItem) {
      this.cartSrv.updateQuantity(cartItem.id, val).subscribe({
        next: () => { this.loading[gift.id] = false; this.loadCart(); },
        error: (err: any) => { this.giftQuantities[gift.id] = oldQty; this.loading[gift.id] = false; this.showCartMsg(gift.id, err.error?.message || 'שגיאה'); }
      });
    } else if (val > 0 && !cartItem) {
      const dto = new AddToCartModel();
      dto.giftId = gift.id;
      dto.quantity = val;
      this.cartSrv.add(dto).subscribe({
        next: () => { this.loading[gift.id] = false; this.loadCart(); },
        error: (err: any) => { this.giftQuantities[gift.id] = oldQty; this.loading[gift.id] = false; this.showCartMsg(gift.id, err.error?.message || 'שגיאה'); }
      });
    } else {
      this.loading[gift.id] = false;
    }
  }

  /** Show a temporary message on a gift card */
  private showCartMsg(giftId: number, msg: string) {
    this.cartMessages[giftId] = msg;
    setTimeout(() => delete this.cartMessages[giftId], 2500);
  }

  // Add to cart (manager view)
  addToCart(gift: GiftModel) {
    const dto = new AddToCartModel();
    dto.giftId = gift.id;
    dto.quantity = 1;
    this.cartSrv.add(dto).subscribe({
      next: () => {
        this.cartMessages[gift.id] = '✅ נוסף לסל!';
        setTimeout(() => delete this.cartMessages[gift.id], 2000);
      },
      error: (err: any) => {
        this.cartMessages[gift.id] = err.error?.message || 'שגיאה';
        setTimeout(() => delete this.cartMessages[gift.id], 3000);
      }
    });
  }
}