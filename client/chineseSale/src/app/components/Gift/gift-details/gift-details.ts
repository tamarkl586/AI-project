import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { GiftService } from '../../../services/Gift/gift-service';
import { CartService } from '../../../services/Cart/cart-service';
import { AuthService } from '../../../services/user/auth-service';
import { GiftModel } from '../../../models/Gift/GiftModel';
import { AddToCartModel } from '../../../models/Cart/AddToCartModel';

@Component({
  selector: 'app-gift-details',
  imports: [RouterLink],
  templateUrl: './gift-details.html',
  styleUrl: './gift-details.scss',
})
export class GiftDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private giftService = inject(GiftService);
  private cartService = inject(CartService);
  authService = inject(AuthService);

  gift?: GiftModel;
  quantity = 1;
  message = '';

  get isDrawn(): boolean {
    return !!this.gift?.winnerId;
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.giftService.getById(id).subscribe({
        next: (g) => this.gift = g,
        error: (err) => console.error('שגיאה בטעינת מתנה', err)
      });
    }
  }

  addToCart() {
    if (!this.gift) return;
    const dto = new AddToCartModel();
    dto.giftId = this.gift.id;
    dto.quantity = this.quantity;
    this.cartService.add(dto).subscribe({
      next: () => this.message = 'נוסף לסל בהצלחה!',
      error: (err) => this.message = err.error?.message || 'שגיאה בהוספה לסל'
    });
  }

  changeQty(delta: number) {
    const newQty = this.quantity + delta;
    if (newQty >= 1 && newQty <= 100) {
      this.quantity = newQty;
    }
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}
