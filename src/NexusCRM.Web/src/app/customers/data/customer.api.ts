import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import {
  CreateCustomerRequest,
  CustomerDetailDto,
  CustomerDto,
  PagedResponse,
} from './customer.models';

@Injectable({ providedIn: 'root' })
export class CustomerApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/v1/customers`;

  search(search = '', page = 1, pageSize = 20) {
    const params = new HttpParams()
      .set('search', search)
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PagedResponse<CustomerDto>>(this.base, { params });
  }

  getById(id: string) {
    return this.http.get<CustomerDetailDto>(`${this.base}/${id}`);
  }

  create(request: CreateCustomerRequest) {
    return this.http.post<CustomerDto>(this.base, request);
  }

  addNote(id: string, body: string) {
    return this.http.post(`${this.base}/${id}/notes`, { body });
  }

  addContact(id: string, name: string, email?: string, phone?: string, isPrimary = false) {
    return this.http.post(`${this.base}/${id}/contacts`, { name, email, phone, isPrimary });
  }

  addTag(id: string, tag: string) {
    return this.http.post(`${this.base}/${id}/tags`, { tag });
  }
}
