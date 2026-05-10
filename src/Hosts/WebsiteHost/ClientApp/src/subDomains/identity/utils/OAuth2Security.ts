/**
 * OAuth2 Security utilities for PKCE (Proof Key for Code Exchange) implementation
 * Following RFC 7636 specification
 */

import { SessionStorageKeys } from '../../../framework/constants.ts';

/**
 * Generates a cryptographically secure code verifier for PKCE
 * @returns Base64URL-encoded random string (43-128 characters)
 */
export const generateCodeVerifier = (): string => {
  const array = new Uint8Array(32);
  crypto.getRandomValues(array);
  return btoa(String.fromCharCode(...array))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
};

/**
 * Generates a code challenge from the code verifier using SHA256
 * @param verifier The code verifier string
 * @returns Base64URL-encoded SHA256 hash of the verifier
 */
export const generateCodeChallenge = async (verifier: string): Promise<string> => {
  const encoder = new TextEncoder();
  const data = encoder.encode(verifier);
  const digest = await crypto.subtle.digest('SHA-256', data);
  return btoa(String.fromCharCode(...new Uint8Array(digest)))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
};

/**
 * Generates a cryptographically secure state parameter for OAuth2
 * @returns Hex-encoded random string
 */
export const generateOAuth2State = (): string =>
  crypto.getRandomValues(new Uint8Array(16)).reduce((acc, byte) => acc + byte.toString(16).padStart(2, '0'), '');

/**
 * Stores PKCE parameters in session storage for later verification
 * @param state OAuth2 state parameter
 * @param codeVerifier PKCE code verifier
 */
export const storePKCEParameters = (state: string, codeVerifier: string): void => {
  sessionStorage.setItem(SessionStorageKeys.OAuth2PkceState, state);
  sessionStorage.setItem(SessionStorageKeys.OAuth2PkceVerifier, codeVerifier);
};

/**
 * Retrieves and validates PKCE parameters from session storage
 * @param returnedState State parameter returned from OAuth2 provider
 * @returns Object containing validation result and code verifier
 */
export const validatePKCEParametersFromStorage = (
  returnedState: string | null
): {
  isValid: boolean;
  codeVerifier: string | null;
  error?: string;
} => {
  const storedState = sessionStorage.getItem(SessionStorageKeys.OAuth2PkceState);
  const codeVerifier = sessionStorage.getItem(SessionStorageKeys.OAuth2PkceVerifier);

  if (!storedState || !returnedState || storedState !== returnedState) {
    return {
      isValid: false,
      codeVerifier: null,
      error: 'OAuth2 session mismatch - possible CSRF attack!'
    };
  }

  if (!codeVerifier) {
    return {
      isValid: false,
      codeVerifier: null,
      error: 'SSO code verifier was not found'
    };
  }

  return {
    isValid: true,
    codeVerifier
  };
};

/**
 * Cleans up PKCE parameters from session storage
 */
export const cleanupStoredPKCEParameters = (): void => {
  sessionStorage.removeItem(SessionStorageKeys.OAuth2PkceState);
  sessionStorage.removeItem(SessionStorageKeys.OAuth2PkceVerifier);
};
