import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HareFound, MarkFoundRequest, PublicInstance } from './api-types';

/** Public endpoints. The API resolves the instance from the host name, so no id is passed. */
@Injectable({ providedIn: 'root' })
export class PublicApiService {
  private readonly http = inject(HttpClient);

  getInstance(): Observable<PublicInstance> {
    return this.http.get<PublicInstance>('/api/public/instance');
  }

  markFound(hareId: string, nickname: string | null): Observable<HareFound> {
    const body: MarkFoundRequest = { nickname };
    return this.http.post<HareFound>(`/api/public/hares/${encodeURIComponent(hareId)}/found`, body);
  }
}
