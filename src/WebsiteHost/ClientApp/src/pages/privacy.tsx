import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

export function PrivacyPage() {
  const { t: translate } = useTranslation('common');

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="bg-white shadow-sm rounded-lg p-8">
          <div className="text-center mb-8">
            <h1 className="text-4xl font-bold text-gray-900 mb-2">{translate('pages.home.privacy.title')}</h1>
            <p className="text-gray-600">Last updated: January 1, 2024</p>
          </div>

          <div className="prose prose-lg max-w-none">
            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">1. Introduction</h2>
              <p className="text-gray-700 mb-4">
                SaaStack ("we", "us", or "our") is committed to protecting your privacy. This Privacy Policy explains
                how we collect, use, disclose, and safeguard your information when you use our software-as-a-service
                platform and related services ("Service").
              </p>
              <p className="text-gray-700 mb-4">
                This policy applies to information we collect through our Service, website, and communications with you.
                By using our Service, you consent to the data practices described in this policy.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">2. Information We Collect</h2>

              <h3 className="text-xl font-medium text-gray-900 mb-3">2.1 Information You Provide</h3>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>
                  <strong>Account Information:</strong> Name, email address, phone number, time zone, locale
                </li>
                <li>
                  <strong>Billing Information:</strong> Billing address, payment information (provided by third-party
                  payment processors)
                </li>
                <li>
                  <strong>Profile Information:</strong> User preferences, settings, and profile data
                </li>
                <li>
                  <strong>Customer Data:</strong> Any data, content, or information you upload or input into our Service
                </li>
                <li>
                  <strong>Communications:</strong> Messages, feedback, and support requests you send to us
                </li>
              </ul>

              <h3 className="text-xl font-medium text-gray-900 mb-3">2.2 Information We Collect Automatically</h3>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>
                  <strong>Usage Data:</strong> How you interact with our Service, features used, time spent
                </li>
                <li>
                  <strong>Device Information:</strong> IP address, browser type, operating system, device identifiers
                </li>
                <li>
                  <strong>Log Data:</strong> Server logs, error reports, performance metrics
                </li>
                <li>
                  <strong>Cookies:</strong> Session cookies, preference cookies, and analytics cookies
                </li>
              </ul>

              <h3 className="text-xl font-medium text-gray-900 mb-3">2.3 Information from Third Parties</h3>
              <p className="text-gray-700 mb-4">
                We may receive information from business partners, service providers, or public sources to enhance our
                Service or verify information you provide.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">3. How We Use Your Information</h2>
              <p className="text-gray-700 mb-4">We use the information we collect to:</p>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>Provide, maintain, and improve our Service</li>
                <li>Process transactions and manage your account</li>
                <li>Communicate with you about your account and our Service</li>
                <li>Provide customer support and respond to inquiries</li>
                <li>Send important notices about changes to our terms or policies</li>
                <li>Analyze usage patterns to improve user experience</li>
                <li>Detect, prevent, and address security issues or fraud</li>
                <li>Comply with legal obligations and enforce our terms</li>
                <li>Send marketing communications (with your consent where required)</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">4. Legal Basis for Processing (GDPR)</h2>
              <p className="text-gray-700 mb-4">
                For users in the European Union, we process your personal data based on:
              </p>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>
                  <strong>Contract Performance:</strong> To provide our Service as agreed in our Terms of Service
                </li>
                <li>
                  <strong>Legitimate Interest:</strong> To improve our Service, ensure security, and communicate with
                  you
                </li>
                <li>
                  <strong>Legal Compliance:</strong> To comply with applicable laws and regulations
                </li>
                <li>
                  <strong>Consent:</strong> For marketing communications and optional features (where applicable)
                </li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">5. Information Sharing and Disclosure</h2>

              <h3 className="text-xl font-medium text-gray-900 mb-3">5.1 We Do Not Sell Your Data</h3>
              <p className="text-gray-700 mb-4">
                We do not sell, rent, or trade your personal information to third parties for their marketing purposes.
              </p>

              <h3 className="text-xl font-medium text-gray-900 mb-3">5.2 When We Share Information</h3>
              <p className="text-gray-700 mb-4">We may share your information in the following circumstances:</p>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>
                  <strong>Service Providers:</strong> Third-party vendors who help us operate our Service (hosting,
                  analytics, payment processing)
                </li>
                <li>
                  <strong>Business Transfers:</strong> In connection with mergers, acquisitions, or asset sales
                </li>
                <li>
                  <strong>Legal Requirements:</strong> When required by law, court order, or to protect our rights
                </li>
                <li>
                  <strong>Safety and Security:</strong> To protect the safety of our users or prevent fraud
                </li>
                <li>
                  <strong>With Your Consent:</strong> When you explicitly agree to share information
                </li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">6. Data Security</h2>
              <p className="text-gray-700 mb-4">
                We implement industry-standard security measures to protect your information:
              </p>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>Encryption of data in transit and at rest using AES-256</li>
                <li>Regular security audits and penetration testing</li>
                <li>Multi-factor authentication for administrative access</li>
                <li>Employee training on data protection and security practices</li>
                <li>Incident response procedures for security breaches</li>
              </ul>
              <p className="text-gray-700 mb-4">
                While we strive to protect your information, no method of transmission over the internet or electronic
                storage is 100% secure. We cannot guarantee absolute security but will notify you of any material
                breaches as required by law.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">7. Data Retention</h2>
              <p className="text-gray-700 mb-4">
                We retain your information for as long as necessary to provide our Service and fulfill the purposes
                outlined in this policy:
              </p>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>
                  <strong>Account Data:</strong> Retained while your account is active and for 90 days after termination
                </li>
                <li>
                  <strong>Customer Data:</strong> Retained according to your subscription terms and deleted upon request
                </li>
                <li>
                  <strong>Usage Logs:</strong> Typically retained for 12 months for security and analytics purposes
                </li>
                <li>
                  <strong>Legal Requirements:</strong> Some data may be retained longer to comply with legal obligations
                </li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">8. Your Privacy Rights</h2>
              <p className="text-gray-700 mb-4">Depending on your location, you may have the following rights:</p>

              <h3 className="text-xl font-medium text-gray-900 mb-3">8.1 General Rights</h3>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>
                  <strong>Access:</strong> Request a copy of the personal information we hold about you
                </li>
                <li>
                  <strong>Correction:</strong> Request correction of inaccurate or incomplete information
                </li>
                <li>
                  <strong>Deletion:</strong> Request deletion of your personal information (subject to legal
                  requirements)
                </li>
                <li>
                  <strong>Portability:</strong> Request your data in a machine-readable format
                </li>
                <li>
                  <strong>Opt-out:</strong> Unsubscribe from marketing communications
                </li>
              </ul>

              <h3 className="text-xl font-medium text-gray-900 mb-3">8.2 GDPR Rights (EU Users)</h3>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>Right to restrict processing</li>
                <li>Right to object to processing</li>
                <li>Right to withdraw consent</li>
                <li>Right to lodge a complaint with supervisory authorities</li>
              </ul>

              <p className="text-gray-700 mb-4">
                To exercise these rights, contact us at privacy@saastack.com. We will respond within 30 days.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">9. International Data Transfers</h2>
              <p className="text-gray-700 mb-4">
                Our Service is hosted in the United States. If you are accessing our Service from outside the US, your
                information may be transferred to, stored, and processed in the US where our servers are located.
              </p>
              <p className="text-gray-700 mb-4">
                For EU users, we ensure adequate protection through Standard Contractual Clauses approved by the
                European Commission and other appropriate safeguards.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">10. Cookies and Tracking</h2>
              <p className="text-gray-700 mb-4">We use cookies and similar technologies to:</p>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>Remember your preferences and settings</li>
                <li>Analyze how our Service is used</li>
                <li>Provide security features</li>
                <li>Improve user experience</li>
              </ul>
              <p className="text-gray-700 mb-4">
                You can control cookies through your browser settings. However, disabling cookies may affect the
                functionality of our Service.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">11. Children's Privacy</h2>
              <p className="text-gray-700 mb-4">
                Our Service is not intended for children under 16 years of age. We do not knowingly collect personal
                information from children under 16. If we become aware that we have collected such information, we will
                take steps to delete it promptly.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">12. Changes to This Privacy Policy</h2>
              <p className="text-gray-700 mb-4">
                We may update this Privacy Policy from time to time. We will notify you of any material changes by:
              </p>
              <ul className="list-disc pl-6 text-gray-700 mb-4">
                <li>Posting the updated policy on our website</li>
                <li>Sending an email notification to your registered email address</li>
                <li>Providing notice through our Service</li>
              </ul>
              <p className="text-gray-700 mb-4">
                Changes will be effective 30 days after notification for material changes, or immediately for
                non-material changes.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold text-gray-900 mb-4">13. Contact Us</h2>
              <p className="text-gray-700 mb-4">
                If you have questions about this Privacy Policy or our privacy practices, please contact us:
              </p>
              <div className="bg-gray-50 p-6 rounded-lg">
                <p className="text-gray-700">
                  <strong>Privacy Officer</strong>
                  <br />
                  <strong>Email:</strong> privacy@saastack.com
                  <br />
                  <strong>Address:</strong> SaaStack, 100 Enterprise Way, Suite 200, San Francisco, CA 94105
                  <br />
                  <strong>Phone:</strong> +1 (555) 123-4567
                </p>
              </div>
            </section>
          </div>

          <div className="mt-12 pt-8 border-t border-gray-200 text-center">
            <Link
              to="/"
              className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
            >
              Back to Home
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
