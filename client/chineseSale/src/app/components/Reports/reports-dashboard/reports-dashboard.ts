import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../../services/Report/report-service';
import { GiftWinnerReportModel } from '../../../models/Report/GiftWinnerReportModel';
import { RevenueSummaryModel } from '../../../models/Report/RevenueSummaryModel';
import { TopGiftStatsModel } from '../../../models/Cart/TopGiftStatsModel';
import { PurchaserDetailsModel } from '../../../models/Cart/PurchaserDetailsModel';

@Component({
  selector: 'app-reports-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './reports-dashboard.html',
  styleUrls: ['./reports-dashboard.scss'],
})
export class ReportsDashboard implements OnInit {
  activeTab: 'revenue' | 'winners' | 'topGift' | 'purchasers' = 'revenue';

  // Revenue
  revenueSummary?: RevenueSummaryModel;

  // Winners
  winners: GiftWinnerReportModel[] = [];

  // Top gift
  topGiftCriteria = 'tickets';
  topGift?: TopGiftStatsModel | null;

  // Purchasers
  purchasers: PurchaserDetailsModel[] = [];
  expandedPurchaser?: PurchaserDetailsModel;

  loading = false;

  constructor(private reportService: ReportService) {}

  ngOnInit(): void {
    this.loadRevenue();
  }

  switchTab(tab: typeof this.activeTab) {
    this.activeTab = tab;
    switch (tab) {
      case 'revenue': this.loadRevenue(); break;
      case 'winners': this.loadWinners(); break;
      case 'topGift': this.loadTopGift(); break;
      case 'purchasers': this.loadPurchasers(); break;
    }
  }

  loadRevenue() {
    this.loading = true;
    this.reportService.getRevenueSummary().subscribe({
      next: (res) => { this.revenueSummary = res; this.loading = false; },
      error: () => this.loading = false
    });
  }

  loadWinners() {
    this.loading = true;
    this.reportService.getWinners().subscribe({
      next: (res) => { this.winners = res; this.loading = false; },
      error: () => this.loading = false
    });
  }

  loadTopGift() {
    this.loading = true;
    this.reportService.getTopGift(this.topGiftCriteria).subscribe({
      next: (res) => { this.topGift = res; this.loading = false; },
      error: () => this.loading = false
    });
  }

  loadPurchasers() {
    this.loading = true;
    this.reportService.getAllPurchasers().subscribe({
      next: (res) => { this.purchasers = res; this.loading = false; },
      error: () => this.loading = false
    });
  }

  togglePurchaser(p: PurchaserDetailsModel) {
    this.expandedPurchaser = this.expandedPurchaser?.userId === p.userId ? undefined : p;
  }

  getPurchasersTotalTickets(): number {
    return this.purchasers.reduce((sum, p) => sum + p.totalTicketsPurchased, 0);
  }

  getPurchasersTotalRevenue(): number {
    return this.purchasers.reduce((sum, p) => sum + p.grandTotalSpent, 0);
  }
}
