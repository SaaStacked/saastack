import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { RoutePaths } from '../../../framework/constants.ts';


export function TermsPage() {
  const { t: translate } = useTranslation();

  return (
    <article className="prose dark:prose-invert max-w-4xl mx-auto">
      <div>
        <h1>{translate('pages.terms.title')}</h1>
        <p>Last updated: January 1, 2024</p>
      </div>

      <div>
        <section>
          <h2>1. Acceptance of Terms</h2>
          <p>
            By accessing and using SaaStack ("the Service"), you accept and agree to be bound by the terms and provision
            of this agreement. If you do not agree to abide by the above, please do not use this service.
          </p>
        </section>

        <section>
          <h2>2. Description of Service</h2>
          <p>
            SaaStack provides a comprehensive software-as-a-service platform that enables users to build, deploy, and
            manage web applications. The Service includes but is not limited to:
          </p>
          <ul>
            <li>User authentication and authorization</li>
            <li>Data storage and management</li>
            <li>API endpoints and integrations</li>
            <li>Analytics and monitoring tools</li>
            <li>Customer support services</li>
          </ul>
        </section>

        <section>
          <h2>3. User Accounts</h2>
          <p>To access certain features of the Service, you must register for an account. You agree to:</p>
          <ul>
            <li>Provide accurate, current, and complete information during registration</li>
            <li>Maintain and update your account information</li>
            <li>Keep your password secure and confidential</li>
            <li>Accept responsibility for all activities under your account</li>
            <li>Notify us immediately of any unauthorized use</li>
          </ul>
        </section>

        <section>
          <h2>4. Acceptable Use</h2>
          <p>You agree not to use the Service to:</p>
          <ul>
            <li>Violate any applicable laws or regulations</li>
            <li>Infringe on intellectual property rights</li>
            <li>Transmit harmful, offensive, or inappropriate content</li>
            <li>Attempt to gain unauthorized access to our systems</li>
            <li>Interfere with or disrupt the Service</li>
            <li>Use the Service for commercial purposes without authorization</li>
          </ul>
        </section>

        <section>
          <h2>5. Privacy and Data Protection</h2>
          <p>
            Your privacy is important to us. Our Privacy Policy explains how we collect, use, and protect your
            information when you use our Service. By using the Service, you agree to the collection and use of
            information in accordance with our Privacy Policy.
          </p>
        </section>

        <section>
          <h2>6. Intellectual Property</h2>
          <p>
            The Service and its original content, features, and functionality are and will remain the exclusive property
            of SaaStack and its licensors. The Service is protected by copyright, trademark, and other laws. You may not
            reproduce, distribute, or create derivative works without our express written permission.
          </p>
        </section>

        <section>
          <h2>7. Limitation of Liability</h2>
          <p>
            In no event shall SaaStack, its directors, employees, or agents be liable for any indirect, incidental,
            special, consequential, or punitive damages, including without limitation, loss of profits, data, use,
            goodwill, or other intangible losses, resulting from your use of the Service.
          </p>
        </section>

        <section>
          <h2>8. Termination</h2>
          <p>
            We may terminate or suspend your account and bar access to the Service immediately, without prior notice or
            liability, under our sole discretion, for any reason whatsoever, including without limitation if you breach
            the Terms.
          </p>
        </section>

        <section>
          <h2>9. Changes to Terms</h2>
          <p>
            We reserve the right to modify or replace these Terms at any time. If a revision is material, we will
            provide at least 30 days notice prior to any new terms taking effect.
          </p>
        </section>

        <section>
          <h2>10. Contact Information</h2>
          <p>If you have any questions about these Terms and Conditions, please contact us at:</p>
          <div>
            <p>
              <strong>Email:</strong> legal@saastack.com
              <br />
              <strong>Address:</strong> 123 Tech Street, San Francisco, CA 94105
              <br />
              <strong>Phone:</strong> +1 (555) 123-4567
            </p>
          </div>
        </section>
      </div>

      <div>
        <Link to={RoutePaths.Home}>Back to Home</Link>
      </div>
    </article>
  );
}
