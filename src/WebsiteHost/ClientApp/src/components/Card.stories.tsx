import type { Meta, StoryObj } from '@storybook/react';
import Card from './Card';
import Button from './Button';

const meta: Meta<typeof Card> = {
  title: 'Components/Card',
  component: Card,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: { type: 'select' },
      options: ['default', 'outlined', 'elevated']
    },
    padding: {
      control: { type: 'select' },
      options: ['none', 'sm', 'md', 'lg']
    },
    clickable: {
      control: { type: 'boolean' }
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    children: <p className="text-gray-600">This is a basic card with some content inside it.</p>
  }
};

export const WithTitle: Story = {
  args: {
    title: 'Card Title',
    children: <p className="text-gray-600">This card has a title and some content.</p>
  }
};

export const WithTitleAndSubtitle: Story = {
  args: {
    title: 'Card Title',
    subtitle: 'This is a subtitle',
    children: <p className="text-gray-600">This card has both a title and subtitle.</p>
  }
};

export const Outlined: Story = {
  args: {
    variant: 'outlined',
    title: 'Outlined Card',
    children: <p className="text-gray-600">This card has an outlined variant.</p>
  }
};

export const Elevated: Story = {
  args: {
    variant: 'elevated',
    title: 'Elevated Card',
    children: <p className="text-gray-600">This card has an elevated variant with shadow.</p>
  }
};

export const Clickable: Story = {
  args: {
    title: 'Clickable Card',
    clickable: true,
    onClick: () => alert('Card clicked!'),
    children: <p className="text-gray-600">This card is clickable. Try clicking on it!</p>
  }
};

export const WithActions: Story = {
  args: {
    title: 'Card with Actions',
    subtitle: 'This card contains action buttons',
    children: (
      <div className="space-y-4">
        <p className="text-gray-600">This card contains some content and action buttons.</p>
        <div className="flex space-x-2">
          <Button variant="primary" size="sm">
            Primary Action
          </Button>
          <Button variant="outline" size="sm">
            Secondary Action
          </Button>
        </div>
      </div>
    )
  }
};

export const NoPadding: Story = {
  args: {
    padding: 'none',
    children: (
      <div className="p-4">
        <h3 className="text-lg font-semibold mb-2">Custom Padding</h3>
        <p className="text-gray-600">This card has no default padding, allowing for custom content layout.</p>
      </div>
    )
  }
};

export const AllVariants: Story = {
  render: () => (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 w-full max-w-4xl">
      <Card variant="default" title="Default Card">
        <p className="text-gray-600">Default variant</p>
      </Card>
      <Card variant="outlined" title="Outlined Card">
        <p className="text-gray-600">Outlined variant</p>
      </Card>
      <Card variant="elevated" title="Elevated Card">
        <p className="text-gray-600">Elevated variant</p>
      </Card>
    </div>
  ),
  parameters: {
    layout: 'padded'
  }
};
