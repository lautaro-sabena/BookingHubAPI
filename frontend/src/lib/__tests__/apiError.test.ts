import { describe, it, expect } from 'vitest';
import { getApiErrorMessage } from '../apiError';

const failure = (data: unknown) => ({ response: { data } });

describe('getApiErrorMessage', () => {
  it('prefers problem details detail', () => {
    expect(getApiErrorMessage(failure({ title: 'Conflict', detail: 'Time slot is not available' })))
      .toBe('Time slot is not available');
  });

  it('uses the first validation message when there is no detail', () => {
    expect(getApiErrorMessage(failure({
      title: 'One or more validation errors occurred.',
      errors: { Email: ['The Email field is required.'] },
    }))).toBe('The Email field is required.');
  });

  it('falls back to title when there is no detail', () => {
    expect(getApiErrorMessage(failure({ title: 'Not Found', status: 404 }))).toBe('Not Found');
  });

  it('reads the legacy error field', () => {
    expect(getApiErrorMessage(failure({ error: 'Email already registered' }))).toBe('Email already registered');
  });

  it('ignores blank values and uses the fallback', () => {
    expect(getApiErrorMessage(failure({ detail: '  ', title: '' }), 'Nope')).toBe('Nope');
  });

  it('uses the fallback for network errors and non-object bodies', () => {
    expect(getApiErrorMessage(new Error('Network Error'), 'Nope')).toBe('Nope');
    expect(getApiErrorMessage(failure('Bad gateway'), 'Nope')).toBe('Nope');
    expect(getApiErrorMessage(undefined, 'Nope')).toBe('Nope');
  });

  it('has a generic default fallback', () => {
    expect(getApiErrorMessage(null)).toBe('Something went wrong. Please try again.');
  });
});
