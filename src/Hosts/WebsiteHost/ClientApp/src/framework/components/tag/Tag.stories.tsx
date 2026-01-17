import type { Meta, StoryObj } from '@storybook/react';
import Tag from './Tag';


const meta: Meta<typeof Tag> = {
  title: 'Components/Tag',
  component: Tag,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    color: {
      control: 'select',
      options: [
        'brand-primary',
        'brand-secondary',
        'success',
        'warning',
        'info',
        'red',
        'orange',
        'amber',
        'yellow',
        'lime',
        'green',
        'emerald',
        'teal',
        'cyan',
        'sky',
        'blue',
        'indigo',
        'violet',
        'purple',
        'fuchsia',
        'pink',
        'rose',
        'slate',
        'gray',
        'zinc',
        'neutral',
        'stone'
      ]
    },
    label: {
      control: 'text'
    },
    title: {
      control: 'text'
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    label: 'Default Tag',
    color: 'brand-primary'
  }
};

export const AllSizes: Story = {
  render: () => (
    <div>
      <Tag className="text-xs" label="X Small" color="brand-primary" />
      <Tag className="text-sm" label="Small" color="brand-primary" />
      <Tag className="text-base" label="Base" color="brand-primary" />
      <Tag className="text-lg" label="Large" color="brand-primary" />
      <Tag className="text-xl" label="X Large" color="brand-primary" />
      <Tag className="text-2xl" label="2X Large" color="brand-primary" />
      <Tag className="text-3xl" label="3X Large" color="brand-primary" />
      <Tag className="text-4xl" label="4X Large" color="brand-primary" />
      <Tag className="text-5xl" label="5X Large" color="brand-primary" />
      <Tag className="text-6xl" label="6X Large" color="brand-primary" />
      <Tag className="text-7xl" label="7X Large" color="brand-primary" />
      <Tag className="text-8xl" label="8X Large" color="brand-primary" />
      <Tag className="text-9xl" label="9X Large" color="brand-primary" />
    </div>
  )
};

export const AllColors: Story = {
  render: () => (
    <div className="grid grid-cols-4 gap-4 max-w-4xl">
      <Tag label="Brand-Primary" color="brand-primary" />
      <Tag label="Brand-Secondary" color="brand-secondary" />
      <Tag label="Success" color="success" />
      <Tag label="Warning" color="warning" />
      <Tag label="Info" color="info" />
      <Tag label="Red" color="red" />
      <Tag label="Orange" color="orange" />
      <Tag label="Amber" color="amber" />
      <Tag label="Yellow" color="yellow" />
      <Tag label="Lime" color="lime" />
      <Tag label="Green" color="green" />
      <Tag label="Emerald" color="emerald" />
      <Tag label="Teal" color="teal" />
      <Tag label="Cyan" color="cyan" />
      <Tag label="Sky" color="sky" />
      <Tag label="Blue" color="blue" />
      <Tag label="Indigo" color="indigo" />
      <Tag label="Violet" color="violet" />
      <Tag label="Purple" color="purple" />
      <Tag label="Fuchsia" color="fuchsia" />
      <Tag label="Pink" color="pink" />
      <Tag label="Rose" color="rose" />
      <Tag label="Slate" color="slate" />
      <Tag label="Gray" color="gray" />
      <Tag label="Zinc" color="zinc" />
      <Tag label="Neutral" color="neutral" />
      <Tag label="Stone" color="stone" />
    </div>
  )
};

export const WithTitle: Story = {
  args: {
    label: 'Hover me',
    color: 'blue',
    title: 'This is a tooltip title'
  }
};

export const WithChildren: Story = {
  args: {
    label: 'This will be ignored',
    color: 'green'
  },
  render: (args) => (
    <Tag {...args}>
      <span>some content</span>&nbsp;<span className="font-bold">some more content</span>
    </Tag>
  )
};

export const TagGroup: Story = {
  render: () => (
    <div className="flex flex-wrap gap-2">
      <Tag label="React" color="blue" />
      <Tag label="TypeScript" color="indigo" />
      <Tag label="Tailwind" color="cyan" />
      <Tag label="Storybook" color="pink" />
      <Tag label="Frontend" color="green" />
      <Tag label="UI/UX" color="purple" />
    </div>
  )
};
