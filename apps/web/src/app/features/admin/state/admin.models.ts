import { GpxImportResult, GpxPreview } from '../../../core/api/api-types';

export type LoadStatus = 'idle' | 'loading' | 'loaded' | 'error';

export interface AdminMessage {
  kind: 'success' | 'error';
  /** Translation key, e.g. `admin.saved` or `errors.instance.slug_taken`. */
  key: string;
}

export interface GpxState {
  status: 'idle' | 'previewing' | 'preview' | 'importing' | 'done';
  uploadId: string | null;
  fileName: string | null;
  preview: GpxPreview | null;
  result: GpxImportResult | null;
}
