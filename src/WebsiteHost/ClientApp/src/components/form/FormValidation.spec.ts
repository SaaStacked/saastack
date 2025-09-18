import { describe, expect, it } from 'vitest';
import { getNestedField } from './FormValidation';

describe('getNestedField', () => {
  it('returns value for simple property', () => {
    const obj = { name: 'John' };
    expect(getNestedField(obj, 'name')).toBe('John');
  });

  it('returns value for nested property', () => {
    const obj = { user: { profile: { name: 'John' } } };
    expect(getNestedField(obj, 'user.profile.name')).toBe('John');
  });

  it('returns undefined for non-existent property', () => {
    const obj = { name: 'John' };
    expect(getNestedField(obj, 'age')).toBeUndefined();
  });

  it('returns undefined for non-existent nested property', () => {
    const obj = { user: { name: 'John' } };
    expect(getNestedField(obj, 'user.profile.age')).toBeUndefined();
  });

  it('when object is null/undefined, returns undefined', () => {
    expect(getNestedField(null, 'name')).toBeUndefined();
    expect(getNestedField(undefined, 'name')).toBeUndefined();
  });

  it('when path is empty path, returns object', () => {
    const obj = { name: 'John' };
    expect(getNestedField(obj, '')).toBe(obj);
  });

  it('handles deeply nested objects', () => {
    const obj = { a: { b: { c: { d: { e: 'deep value' } } } } };
    expect(getNestedField(obj, 'a.b.c.d.e')).toBe('deep value');
  });

  it('handles arrays in nested path', () => {
    const obj = { users: [{ name: 'John' }, { name: 'Jane' }] };
    expect(getNestedField(obj, 'users.0.name')).toBe('John');
    expect(getNestedField(obj, 'users.1.name')).toBe('Jane');
  });
});
