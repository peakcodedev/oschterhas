import { HttpErrorResponse } from '@angular/common/http';
import { problemCode } from './problem-code';

describe('problemCode', () => {
  it.each([
    [
      new HttpErrorResponse({ status: 409, error: { code: 'instance.slug_taken' } }),
      'instance.slug_taken',
    ],
    [
      new HttpErrorResponse({ status: 422, error: { code: 'gpx.too_many_waypoints' } }),
      'gpx.too_many_waypoints',
    ],
    [new HttpErrorResponse({ status: 500, error: { code: 'something.internal' } }), 'unknown'],
    [new HttpErrorResponse({ status: 401 }), 'unauthorized'],
    [new HttpErrorResponse({ status: 413 }), 'gpx.file_too_large'],
    [new HttpErrorResponse({ status: 0 }), 'network'],
    [new Error('boom'), 'unknown'],
  ])('maps %o to %s', (error, expected) => {
    expect(problemCode(error)).toBe(expected);
  });
});
