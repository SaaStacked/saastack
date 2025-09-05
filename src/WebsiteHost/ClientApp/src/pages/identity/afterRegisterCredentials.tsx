import React from 'react';
import { Link } from 'react-router-dom';

export const AfterRegisterCredentials: React.FC = () => (
  <div className="after-registration-page">
    <div className="after-registration-container">
      <div className="success-icon">
        <svg width="64" height="64" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <circle cx="12" cy="12" r="10" stroke="#28a745" strokeWidth="2" fill="none" />
          <path d="m9 12 2 2 4-4" stroke="#28a745" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </div>

      <h1>Registration Successful!</h1>

      <div className="confirmation-message">
        <h2>Check Your Email</h2>
        <p>
          We've sent a confirmation email to your inbox. Please check your email and click the confirmation link to
          activate your account.
        </p>

        <div className="email-instructions">
          <h3>What to do next:</h3>
          <ol>
            <li>Check your email inbox (and spam folder)</li>
            <li>Look for an email from SaaStack</li>
            <li>Click the confirmation link in the email</li>
          </ol>
        </div>

        <div className="help-text">
          <p>
            <strong>Didn't receive the email?</strong> It may take a few minutes to arrive. If you still don't see it,
            check your spam folder or contact support.
          </p>
        </div>
      </div>

      <div className="action-buttons">
        <Link to="/" className="btn btn-secondary">
          Back to Home
        </Link>
      </div>
    </div>
  </div>
);
