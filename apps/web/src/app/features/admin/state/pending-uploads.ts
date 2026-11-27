import { Injectable } from '@angular/core';

/**
 * Holds chosen files outside the store (File objects are not serializable, NgRx forbids them in actions).
 * Actions carry only the upload id.
 */
@Injectable({ providedIn: 'root' })
export class PendingUploads {
  private readonly files = new Map<string, File>();
  private next = 0;

  add(file: File): string {
    const id = `upload-${++this.next}`;
    this.files.set(id, file);
    return id;
  }

  get(id: string): File | undefined {
    return this.files.get(id);
  }

  remove(id: string): void {
    this.files.delete(id);
  }
}
