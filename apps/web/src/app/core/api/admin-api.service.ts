import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AddOrganizerRequest,
  AdminHare,
  CreateInstanceRequest,
  CustomDomainCheck,
  GpxImportMode,
  GpxImportResult,
  GpxPreview,
  InstanceDetail,
  InstanceStatus,
  InstanceSummary,
  Me,
  Organizer,
  SaveHareRequest,
  SlugCheck,
  UpdateInstanceRequest,
  UpdateUserRequest,
  User,
} from './api-types';

/**
 * Administration endpoints (admin host only). The session cookie is sent automatically;
 * Angular adds the X-XSRF-TOKEN header to changing requests.
 */
@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);

  me(): Observable<Me> {
    return this.http.get<Me>('/api/auth/me');
  }

  instances(): Observable<InstanceSummary[]> {
    return this.http.get<InstanceSummary[]>('/api/instances');
  }

  instance(id: string): Observable<InstanceDetail> {
    return this.http.get<InstanceDetail>(`/api/instances/${id}`);
  }

  createInstance(request: CreateInstanceRequest): Observable<InstanceDetail> {
    return this.http.post<InstanceDetail>('/api/instances', request);
  }

  updateInstance(id: string, request: UpdateInstanceRequest): Observable<InstanceDetail> {
    return this.http.put<InstanceDetail>(`/api/instances/${id}`, request);
  }

  setStatus(id: string, status: InstanceStatus): Observable<InstanceDetail> {
    return this.http.post<InstanceDetail>(`/api/instances/${id}/status`, { status });
  }

  deleteInstance(id: string): Observable<void> {
    return this.http.delete<void>(`/api/instances/${id}`);
  }

  checkSlug(slug: string): Observable<SlugCheck> {
    return this.http.get<SlugCheck>('/api/instances/check-slug', { params: { slug } });
  }

  addOrganizer(instanceId: string, request: AddOrganizerRequest): Observable<Organizer> {
    return this.http.post<Organizer>(`/api/instances/${instanceId}/organizers`, request);
  }

  removeOrganizer(instanceId: string, userId: string): Observable<void> {
    return this.http.delete<void>(`/api/instances/${instanceId}/organizers/${userId}`);
  }

  resendInvitation(instanceId: string, userId: string): Observable<void> {
    return this.http.post<void>(
      `/api/instances/${instanceId}/organizers/${userId}/resend-invitation`,
      {},
    );
  }

  hares(instanceId: string): Observable<AdminHare[]> {
    return this.http.get<AdminHare[]>(`/api/instances/${instanceId}/hares`);
  }

  createHare(instanceId: string, request: SaveHareRequest): Observable<AdminHare> {
    return this.http.post<AdminHare>(`/api/instances/${instanceId}/hares`, request);
  }

  updateHare(instanceId: string, hareId: string, request: SaveHareRequest): Observable<AdminHare> {
    return this.http.put<AdminHare>(`/api/instances/${instanceId}/hares/${hareId}`, request);
  }

  deleteHare(instanceId: string, hareId: string): Observable<void> {
    return this.http.delete<void>(`/api/instances/${instanceId}/hares/${hareId}`);
  }

  resetHare(instanceId: string, hareId: string): Observable<AdminHare> {
    return this.http.post<AdminHare>(`/api/instances/${instanceId}/hares/${hareId}/reset`, {});
  }

  gpxPreview(instanceId: string, file: File): Observable<GpxPreview> {
    return this.http.post<GpxPreview>(`/api/instances/${instanceId}/gpx/preview`, formData(file));
  }

  gpxImport(instanceId: string, file: File, mode: GpxImportMode): Observable<GpxImportResult> {
    return this.http.post<GpxImportResult>(`/api/instances/${instanceId}/gpx`, formData(file), {
      params: new HttpParams().set('mode', mode),
    });
  }

  setCustomDomain(instanceId: string, domain: string): Observable<InstanceDetail> {
    return this.http.put<InstanceDetail>(`/api/instances/${instanceId}/custom-domain`, { domain });
  }

  verifyCustomDomain(instanceId: string): Observable<CustomDomainCheck> {
    return this.http.post<CustomDomainCheck>(
      `/api/instances/${instanceId}/custom-domain/verify`,
      {},
    );
  }

  removeCustomDomain(instanceId: string): Observable<InstanceDetail> {
    return this.http.delete<InstanceDetail>(`/api/instances/${instanceId}/custom-domain`);
  }

  users(): Observable<User[]> {
    return this.http.get<User[]>('/api/users');
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<User> {
    return this.http.patch<User>(`/api/users/${id}`, request);
  }
}

function formData(file: File): FormData {
  const data = new FormData();
  data.append('file', file, file.name);
  return data;
}
