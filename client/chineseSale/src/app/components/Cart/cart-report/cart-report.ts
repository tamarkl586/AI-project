import { Component } from '@angular/core';
import { DatePipe } from '@angular/common';
import { PurchaserDetailsModel } from '../../../models/Cart/PurchaserDetailsModel';
import { ReportService } from '../../../services/Report/report-service';

@Component({
  selector: 'app-cart-report',
  imports: [DatePipe],
  templateUrl: './cart-report.html',
  styleUrls: ['./cart-report.scss'],
})
export class CartReport {
    reports: PurchaserDetailsModel[] = [];

  constructor(private reportService: ReportService) { }

  ngOnInit(): void {
    this.reportService.getAllPurchasers().subscribe({
      next: (res: PurchaserDetailsModel[]) => this.reports = res,
      error: (err: any) => console.error('שגיאה בטעינת הדוח', err)
    });
  }

  getTotalTickets(): number {
    return this.reports.reduce((sum, r) => sum + r.totalTicketsPurchased, 0);
  }

  getTotalRevenue(): number {
    return this.reports.reduce((sum, r) => sum + r.grandTotalSpent, 0);
  }
}
