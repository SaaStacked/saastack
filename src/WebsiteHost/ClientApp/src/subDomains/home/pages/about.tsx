import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';


export function AboutPage() {
  const { t: translate } = useTranslation();

  return (
    <article className="prose dark:prose-invert max-w-4xl mx-auto">
      <div className="text-center mb-12">
        <img src="/images/mascot/mascot.png" alt="SaaStack Mascot" className="w-24 h-24 mx-auto mb-6" />
        <h1 className="text-4xl font-bold text-accent-dark dark:text-accent-light mb-4">
          {translate('pages.about.title')}
        </h1>
        <p className="text-xl text-gray-600">The complete SaaS platform codebase template for modern product teams</p>
      </div>

      <div className="space-y-8">
        <section>
          <h2 className="text-2xl font-semibold text-accent-dark dark:text-accent-light mb-4">What is SaaStack?</h2>
          <p>
            SaaStack is a comprehensive, production-ready and deployable codebase template designed to accelerate the
            development of Software-as-a-Service (SaaS) platforms. Built as a monorepo, it contains all the essential
            components you need to launch your SaaS business quickly and efficiently, on day one.
          </p>
          <h2 className="text-2xl font-semibold text-accent-dark dark:text-accent-light mb-4">Why SaaStack?</h2>
          <p>
            In a few months to years from starting with any codebase, even one from scratch, you will want to keep
            moving at a fast pace changing and adapting your codebase to meet the needs of your market. To do that
            effectively, you need to have designed your system have managed complexity very well.
          </p>
          <p>
            Then, at some point later, you will want to split up the codebase and scale out your backends, and to do
            that, you will have needed to have designed your system to split up into separately deployable units,
            without having to reengineer everything at just the time your business needs to take off.
          </p>
          <p>
            From user authentication and billing subscription management to multi-tenancy and API integrations, SaaStack
            provides the solid foundation and technical practices that allows you to continue you to innovate on your
            product and scale your product to meet market needs.
          </p>
        </section>

        <section>
          <h2 className="text-2xl font-semibold text-accent-dark dark:text-accent-light mb-4">
            Who Should Use SaaStack?
          </h2>
          <div className="grid md:grid-cols-2 gap-6">
            <div className="flex items-center space-x-3">
              <div className="w-8 h-8 text-blue-500 flex-shrink-0">
                <svg fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clipRule="evenodd" />
                </svg>
              </div>
              <div>
                <h3 className="text-lg font-medium text-accent-dark dark:text-accent-light mb-2">
                  Startup Technical Founders
                </h3>
                <p>
                  Skip months of foundational development getting the basics in place and focus on building your unique
                  value proposition on day one. Do not waste your time and precious resources building it all again, all
                  over again from scratch! Just adapt this codebase as you see fit.
                </p>
              </div>
            </div>
            <div className="flex items-center space-x-3">
              <div className="w-8 h-8 text-blue-500 flex-shrink-0">
                <svg fill="currentColor" viewBox="0 0 20 20">
                  <path d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v3h8v-3zM6 8a2 2 0 11-4 0 2 2 0 014 0zM16 18v-3a5.972 5.972 0 00-.75-2.906A3.005 3.005 0 0119 15v3h-3zM4.75 12.094A5.973 5.973 0 004 15v3H1v-3a3 3 0 013.75-2.906z" />
                </svg>
              </div>
              <div>
                <h3 className="text-lg font-medium text-accent-dark dark:text-accent-light mb-2">Product Teams</h3>
                <p>
                  Leverage proven patterns, architectures and technical practices to build scalable SaaS applications
                  faster. Follow the established patterns, continue to write the layers of tests, and use the providing
                  tooling and your favorite AI agents to build your product faster and consistently.
                </p>
              </div>
            </div>
            <div className="flex items-center space-x-3">
              <div className="w-8 h-8 text-blue-500 flex-shrink-0">
                <svg fill="currentColor" viewBox="0 0 20 20">
                  <path d="M10.394 2.08a1 1 0 00-.788 0l-7 3a1 1 0 000 1.84L5.25 8.051a.999.999 0 01.356-.257l4-1.714a1 1 0 11.788 1.838L7.667 9.088l1.94.831a1 1 0 00.787 0l7-3a1 1 0 000-1.838l-7-3zM3.31 9.397L5 10.12v4.102a8.969 8.969 0 00-1.05-.174 1 1 0 01-.89-.89 11.115 11.115 0 01.25-3.762zM9.3 16.573A9.026 9.026 0 007 14.935v-3.957l1.818.78a3 3 0 002.364 0l5.508-2.361a11.026 11.026 0 01.25 3.762 1 1 0 01-.89.89 8.968 8.968 0 00-5.35 2.524 1 1 0 01-1.4 0zM6 18a1 1 0 001-1v-2.065a8.935 8.935 0 00-2-.712V17a1 1 0 001 1z" />
                </svg>
              </div>
              <div>
                <h3 className="text-lg font-medium text-accent-dark dark:text-accent-light mb-2">Learning Engineers</h3>
                <p>
                  Study real-world SaaS architecture, architectural styles, and implementation patterns for a complete
                  and production ready codebase.
                </p>
              </div>
            </div>
          </div>
        </section>

        <section>
          <h2 className="text-2xl font-semibold text-accent-dark dark:text-accent-light mb-4">Why Choose SaaStack?</h2>
          <div className="space-y-4">
            <div className="flex items-center space-x-3">
              <div className="w-6 h-6 bg-blue-500 rounded-full flex items-center justify-center flex-shrink-0">
                <svg className="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                    clipRule="evenodd"
                  />
                </svg>
              </div>
              <div>
                <h3 className="font-medium text-accent-dark dark:text-accent-light">Complete Authentication System</h3>
                <p>
                  Multi-factor authentication, Single-Sign-On, OAuth2/OIDC, API Key, password reset, and user management
                  - out of the box.
                </p>
              </div>
            </div>

            <div className="flex items-center space-x-3">
              <div className="w-6 h-6 bg-blue-500 rounded-full flex items-center justify-center flex-shrink-0">
                <svg className="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                    clipRule="evenodd"
                  />
                </svg>
              </div>
              <div>
                <h3 className="font-medium text-accent-dark dark:text-accent-light">Multi-Tenant Architecture</h3>
                <p>
                  B2C or B2B. Globally unique users, and multiple organizations/companies/projects. User roles, and
                  permissions system designed for scalable multi-tenancy.
                </p>
              </div>
            </div>

            <div className="flex items-center space-x-3">
              <div className="w-6 h-6 bg-blue-500 rounded-full flex items-center justify-center flex-shrink-0">
                <svg className="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                    clipRule="evenodd"
                  />
                </svg>
              </div>
              <div>
                <h3 className="font-medium text-accent-dark dark:text-accent-light">Subscription & Billing</h3>
                <p>
                  Built-in subscription plan management, feature flags, and self-serve payment integration ready for
                  your pricing model.
                </p>
              </div>
            </div>

            <div className="flex items-center space-x-3">
              <div className="w-6 h-6 bg-blue-500 rounded-full flex items-center justify-center flex-shrink-0">
                <svg className="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                    clipRule="evenodd"
                  />
                </svg>
              </div>
              <div>
                <h3 className="font-medium text-accent-dark dark:text-accent-light">Modern Tech Stack</h3>
                <p>Built with React, TypeScript, .NET, and modern development practices including testing and CI/CD.</p>
              </div>
            </div>
          </div>
        </section>

        <section className="bg-gray-50 dark:bg-gray-800 p-6 rounded-lg">
          <h2 className="text-2xl font-semibold text-accent-dark dark:text-accent-light mb-4">Ready to Get Started?</h2>
          <p className="text-gray-700 mb-4">
            SaaStack provides everything you need to build, deploy, and scale your SaaS platform. Focus on what makes
            your product unique while we handle the foundation.
          </p>
          <Link
            to="/"
            className="inline-block bg-blue-600 text-white px-6 py-3 rounded-lg font-medium hover:bg-blue-700 transition-colors no-underline"
          >
            Start Building Today
          </Link>
        </section>
      </div>
    </article>
  );
}
