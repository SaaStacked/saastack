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

export const Headings: Story = {
  render: () => (
    <article className="prose dark:prose-invert max-w-4xl mx-auto">
      <section>
        <h2 className="text-2xl font-bold mb-4">Headings</h2>
        <div className="space-y-4">
          <h1 className="text-6xl font-bold">Heading 1 - 6xl</h1>
          <h2 className="text-5xl font-bold">Heading 2 - 5xl</h2>
          <h3 className="text-4xl font-bold">Heading 3 - 4xl</h3>
          <h4 className="text-3xl font-bold">Heading 4 - 3xl</h4>
          <h5 className="text-2xl font-bold">Heading 5 - 2xl</h5>
          <h6 className="text-xl font-bold">Heading 6 - xl</h6>
        </div>
      </section>
    </article>
  )
};

export const BodyText: Story = {
  render: () => (
    <article className="prose dark:prose-invert max-w-4xl mx-auto">
      <section>
        <h2 className="text-2xl font-bold mb-4">Body Text</h2>
        <div className="space-y-4">
          <p className="text-lg">
            Large body text (text-lg) - Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
            incididunt ut labore et dolore magna aliqua.
          </p>
          <p className="text-base">
            Regular body text (text-base) - Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod
            tempor incididunt ut labore et dolore magna aliqua.
          </p>
          <p className="text-sm">
            Small body text (text-sm) - Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
            incididunt ut labore et dolore magna aliqua.
          </p>
          <p className="text-xs">
            Extra small text (text-xs) - Lorem ipsum dolor sit amet, consectetur adipiscing elit.
          </p>
        </div>
      </section>
    </article>
  )
};

export const WeightsAndColors: Story = {
  render: () => (
    <article className="prose dark:prose-invert max-w-4xl mx-auto">
      <section>
        <div className="grid md:grid-cols-2 gap-8">
          <div>
            <h2 className="text-2xl font-bold mb-4">Font Weights</h2>
            <p className="text-base font-light">Light weight text</p>
            <p className="text-base font-normal">Normal weight text</p>
            <p className="text-base font-medium">Medium weight text</p>
            <p className="text-base font-semibold">Semibold weight text</p>
            <p className="text-base font-bold">Bold weight text</p>
          </div>
          <div>
            <h2 className="text-2xl font-bold mb-4">Text Colors</h2>
            <p className="text-base text-brand-primary">Primary color text</p>
            <p className="text-base text-brand-secondary">Secondary color text</p>
            <p className="text-base text-info-600">Info text</p>
            <p className="text-base text-success-600">Success text</p>
            <p className="text-base text-error-600">Error/danger text</p>
            <p className="text-base text-warning-600">Warning text</p>
            <p className="text-base text-neutral">Neutral text</p>
            <p className="text-base text-neutral-900">Dark neutral text</p>
            <p className="text-base text-neutral-700">Medium neutral text</p>
            <p className="text-base text-neutral-500">Light neutral text</p>
          </div>
        </div>
      </section>
    </article>
  )
};

export const Lists: Story = {
  render: () => (
    <article className="prose dark:prose-invert max-w-4xl mx-auto">
      <section>
        <h2 className="text-2xl font-bold mb-4">Lists</h2>
        <div className="grid md:grid-cols-2 gap-8">
          <div>
            <h3 className="text-lg font-semibold mb-2">Unordered List</h3>
            <ul>
              <li>First item</li>
              <li>Second item</li>
              <li>Third item with longer text to show wrapping</li>
              <li>Fourth item</li>
            </ul>
          </div>
          <div>
            <h3 className="text-lg font-semibold mb-2">Ordered List</h3>
            <ol>
              <li>First item</li>
              <li>Second item</li>
              <li>Third item with longer text to show wrapping</li>
              <li>Fourth item</li>
            </ol>
          </div>
        </div>
      </section>
    </article>
  )
};

export const Links: Story = {
  render: () => (
    <article className="prose dark:prose-invert max-w-4xl mx-auto">
      <section>
        <h2 className="text-2xl font-bold mb-4">Links</h2>
        <div className="space-y-2">
          <p className="text-base">
            This is a paragraph with a <a href="#">standard link</a> in it.
          </p>
          <p className="text-base">
            This is a paragraph with a <a href="#">visited link</a> in it.
          </p>
        </div>
      </section>
    </article>
  )
};
