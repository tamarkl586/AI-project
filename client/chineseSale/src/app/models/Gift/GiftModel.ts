export class GiftModel {
    id!: number;
    name!: string;
    description!: string;
    picture!: string;
    price!: number;
    donorName!: string;
    categoryName!: string;
    winnerId?: number | null;
    winnerName?: string;
    winnerEmail?: string;
}