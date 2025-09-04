import type { Meta, StoryObj } from '@storybook/react';
import Badge from './Badge';

const meta: Meta<typeof Badge> = {
  title: 'Components/Badge',
  component: Badge,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: { type: 'select' },
      options: ['default', 'primary', 'secondary', 'success', 'warning', 'danger', 'info']
    },
    size: {
      control: { type: 'select' },
      options: ['sm', 'md', 'lg']
    },
    style: {
      control: { type: 'select' },
      options: ['filled', 'outlined', 'soft']
    },
    removable: {
      control: { type: 'boolean' }
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    children: 'Badge'
  }
};

export const Primary: Story = {
  args: {
    children: 'Primary',
    variant: 'primary'
  }
};

export const Success: Story = {
  args: {
    children: 'Success',
    variant: 'success'
  }
};

export const Warning: Story = {
  args: {
    children: 'Warning',
    variant: 'warning'
  }
};

export const Danger: Story = {
  args: {
    children: 'Danger',
    variant: 'danger'
  }
};

export const Info: Story = {
  args: {
    children: 'Info',
    variant: 'info'
  }
};

export const Outlined: Story = {
  args: {
    children: 'Outlined',
    variant: 'primary',
    style: 'outlined'
  }
};

export const Soft: Story = {
  args: {
    children: 'Soft',
    variant: 'primary',
    style: 'soft'
  }
};

export const Small: Story = {
  args: {
    children: 'Small',
    size: 'sm'
  }
};

export const Large: Story = {
  args: {
    children: 'Large',
    size: 'lg'
  }
};

export const Removable: Story = {
  args: {
    children: 'Removable',
    variant: 'primary',
    removable: true,
    onRemove: () => alert('Badge removed!')
  }
};

export const AllVariants: Story = {
  render: () => (
    <div className="space-y-4">
      <div className="space-x-2">
        <Badge variant="default">Default</Badge>
        <Badge variant="primary">Primary</Badge>
        <Badge variant="secondary">Secondary</Badge>
        <Badge variant="success">Success</Badge>
        <Badge variant="warning">Warning</Badge>
        <Badge variant="danger">Danger</Badge>
        <Badge variant="info">Info</Badge>
      </div>
      <div className="space-x-2">
        <Badge variant="primary" style="filled">
          Filled
        </Badge>
        <Badge variant="primary" style="outlined">
          Outlined
        </Badge>
        <Badge variant="primary" style="soft">
          Soft
        </Badge>
      </div>
      <div className="space-x-2">
        <Badge size="sm">Small</Badge>
        <Badge size="md">Medium</Badge>
        <Badge size="lg">Large</Badge>
      </div>
      <div className="space-x-2">
        <Badge variant="primary" removable onRemove={() => {}}>
          Removable
        </Badge>
        <Badge variant="success" removable onRemove={() => {}}>
          Tag
        </Badge>
        <Badge variant="info" removable onRemove={() => {}}>
          Label
        </Badge>
      </div>
    </div>
  ),
  parameters: {
    layout: 'padded'
  }
};
