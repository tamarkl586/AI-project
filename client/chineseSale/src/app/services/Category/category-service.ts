import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CategoryModel } from '../../models/Category/CategoryModel';
import { CategoryCreateModel } from '../../models/Category/CategoryCreateModel';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  private baseUrl = `${environment.apiUrl}/category`;

  constructor(private http: HttpClient) { }

  getAll(): Observable<CategoryModel[]> {
    return this.http.get<CategoryModel[]>(this.baseUrl);
  }

  add(category: CategoryCreateModel): Observable<any> {
    return this.http.post(this.baseUrl, category);
  }

  update(id: number, category: CategoryCreateModel): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}`, category);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }
}
