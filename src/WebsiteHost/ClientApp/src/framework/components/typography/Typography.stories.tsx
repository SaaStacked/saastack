import type { Meta, StoryObj } from '@storybook/react';

const meta: Meta = {
  title: 'Components/Typography',
  parameters: {
    layout: 'padded'
  },
  tags: ['autodocs']
};

export default meta;
type Story = StoryObj<typeof meta>;

export const AllTypography: Story = {
  render: () => (
    <div className="space-y-8 max-w-4xl">
      {/* Headings */}
      <section>
        <h2 className="text-2xl font-bold mb-4 text-gray-900">Headings</h2>
        <div className="space-y-4">
          <h1 className="text-6xl font-bold text-gray-900">Heading 1 - 6xl</h1>
          <h2 className="text-5xl font-bold text-gray-900">Heading 2 - 5xl</h2>
          <h3 className="text-4xl font-bold text-gray-900">Heading 3 - 4xl</h3>
          <h4 className="text-3xl font-bold text-gray-900">Heading 4 - 3xl</h4>
          <h5 className="text-2xl font-bold text-gray-900">Heading 5 - 2xl</h5>
          <h6 className="text-xl font-bold text-gray-900">Heading 6 - xl</h6>
        </div>
      </section>

      {/* Body Text */}
      <section>
        <h2 className="text-2xl font-bold mb-4 text-gray-900">Body Text</h2>
        <div className="space-y-4">
          <p className="text-lg text-gray-700">
            Large body text (text-lg) - Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
            incididunt ut labore et dolore magna aliqua.
          </p>
          <p className="text-base text-gray-700">
            Regular body text (text-base) - Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod
            tempor incididunt ut labore et dolore magna aliqua.
          </p>
          <p className="text-sm text-gray-600">
            Small body text (text-sm) - Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
            incididunt ut labore et dolore magna aliqua.
          </p>
          <p className="text-xs text-gray-500">
            Extra small text (text-xs) - Lorem ipsum dolor sit amet, consectetur adipiscing elit.
          </p>
        </div>
      </section>

      {/* Font Weights */}
      <section>
        <h2 className="text-2xl font-bold mb-4 text-gray-900">Font Weights</h2>
        <div className="space-y-2">
          <p className="text-base font-light text-gray-700">Light weight text</p>
          <p className="text-base font-normal text-gray-700">Normal weight text</p>
          <p className="text-base font-medium text-gray-700">Medium weight text</p>
          <p className="text-base font-semibold text-gray-700">Semibold weight text</p>
          <p className="text-base font-bold text-gray-700">Bold weight text</p>
        </div>
      </section>

      {/* Colors */}
      <section>
        <h2 className="text-2xl font-bold mb-4 text-gray-900">Text Colors</h2>
        <div className="space-y-2">
          <p className="text-base text-primary">Primary color text</p>
          <p className="text-base text-secondary">Secondary color text</p>
          <p className="text-base text-gray-900">Dark gray text</p>
          <p className="text-base text-gray-700">Medium gray text</p>
          <p className="text-base text-gray-500">Light gray text</p>
          <p className="text-base text-red-600">Error/danger text</p>
          <p className="text-base text-green-600">Success text</p>
          <p className="text-base text-yellow-600">Warning text</p>
        </div>
      </section>

      {/* Links */}
      <section>
        <h2 className="text-2xl font-bold mb-4 text-gray-900">Links</h2>
        <div className="space-y-2">
          <p className="text-base text-gray-700">
            This is a paragraph with a{' '}
            <a href="#" className="text-primary hover:text-primary/80 underline">
              standard link
            </a>{' '}
            in it.
          </p>
          <p className="text-base text-gray-700">
            This is a paragraph with a{' '}
            <a href="#" className="text-primary hover:text-primary/80 underline visited:text-purple-400">
              visited link
            </a>{' '}
            in it.
          </p>
        </div>
      </section>

      {/* Lists */}
      <section>
        <h2 className="text-2xl font-bold mb-4 text-gray-900">Lists</h2>
        <div className="grid md:grid-cols-2 gap-8">
          <div>
            <h3 className="text-lg font-semibold mb-2 text-gray-900">Unordered List</h3>
            <ul className="list-disc list-inside space-y-1 text-gray-700">
              <li>First item</li>
              <li>Second item</li>
              <li>Third item with longer text to show wrapping</li>
              <li>Fourth item</li>
            </ul>
          </div>
          <div>
            <h3 className="text-lg font-semibold mb-2 text-gray-900">Ordered List</h3>
            <ol className="list-decimal list-inside space-y-1 text-gray-700">
              <li>First item</li>
              <li>Second item</li>
              <li>Third item with longer text to show wrapping</li>
              <li>Fourth item</li>
            </ol>
          </div>
        </div>
      </section>
    </div>
  )
};

export const Headings: Story = {
  render: () => (
    <div className="space-y-4">
      <h1 className="text-6xl font-bold text-gray-900">Heading 1</h1>
      <h2 className="text-5xl font-bold text-gray-900">Heading 2</h2>
      <h3 className="text-4xl font-bold text-gray-900">Heading 3</h3>
      <h4 className="text-3xl font-bold text-gray-900">Heading 4</h4>
      <h5 className="text-2xl font-bold text-gray-900">Heading 5</h5>
      <h6 className="text-xl font-bold text-gray-900">Heading 6</h6>
    </div>
  )
};

export const BodyText: Story = {
  render: () => (
    <div className="space-y-4 max-w-2xl">
      <p className="text-lg text-gray-700">
        Large body text - Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut
        labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.
      </p>
      <p className="text-base text-gray-700">
        Regular body text - Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut
        labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.
      </p>
      <p className="text-sm text-gray-600">
        Small body text - Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut
        labore et dolore magna aliqua.
      </p>
    </div>
  )
};
