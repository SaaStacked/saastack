import type { Meta, StoryObj } from '@storybook/react';
import Label from './Label';

const meta: Meta<typeof Label> = {
  title: 'Components/Label',
  component: Label,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    size: {
      control: 'select',
      options: ['xs', 'sm', 'base', 'lg', 'xl', '2xl', '3xl', '4xl', '5xl', '6xl', '7xl', '8xl', '9xl']
    },
    weight: {
      control: 'select',
      options: ['light', 'normal', 'medium', 'semibold', 'bold']
    },
    align: {
      control: 'select',
      options: ['left', 'center', 'right', 'justify', 'start', 'end']
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    size: 'base',
    weight: 'normal',
    align: 'left',
    children: 'Hello World!'
  },
  render: (args) => (
    <Label {...args} className="border-1 border-dotted border-gray-300">
      {args.children}
    </Label>
  )
};

export const LargeCenter: Story = {
  args: {
    size: '2xl',
    weight: 'bold',
    align: 'center',
    children: 'Large Bold Label'
  },
  render: (args) => (
    <Label {...args} className="border-1 border-dotted border-gray-300">
      {args.children}
    </Label>
  )
};

export const SmallRight: Story = {
  args: {
    size: 'xs',
    weight: 'light',
    align: 'right',
    children: 'Small light label'
  },
  render: (args) => (
    <Label {...args} className="border-1 border-dotted border-gray-300">
      {args.children}
    </Label>
  )
};

export const AllSizes: Story = {
  render: () => (
    <div className="grid grid-cols-3 gap-4 max-w-2xl">
      <Label size="xs" weight="normal" align="left" className="border-1 border-dotted border-gray-300">
        Extra Small (xs) Normal Left
      </Label>
      <Label size="xs" weight="normal" align="center" className="border-1 border-dotted border-gray-300">
        Extra Small (xs) Normal Center
      </Label>
      <Label size="xs" weight="normal" align="right" className="border-1 border-dotted border-gray-300">
        Extra Small (xs) Normal Right
      </Label>
      <Label size="xs" weight="bold" align="left" className="border-1 border-dotted border-gray-300">
        Extra Small (xs) Bold Left
      </Label>
      <Label size="xs" weight="bold" align="center" className="border-1 border-dotted border-gray-300">
        Extra Small (xs) Bold Center
      </Label>
      <Label size="xs" weight="bold" align="right" className="border-1 border-dotted border-gray-300">
        Extra Small (xs) Bold Right
      </Label>
      <Label size="sm" weight="normal" align="left" className="border-1 border-dotted border-gray-300">
        Small (sm) Normal Left
      </Label>
      <Label size="sm" weight="normal" align="center" className="border-1 border-dotted border-gray-300">
        Small (sm) Normal Center
      </Label>
      <Label size="sm" weight="normal" align="right" className="border-1 border-dotted border-gray-300">
        Small (sm) Normal Right
      </Label>
      <Label size="sm" weight="bold" align="left" className="border-1 border-dotted border-gray-300">
        Small (sm) Bold Left
      </Label>
      <Label size="sm" weight="bold" align="center" className="border-1 border-dotted border-gray-300">
        Small (sm) Bold Center
      </Label>
      <Label size="sm" weight="bold" align="right" className="border-1 border-dotted border-gray-300">
        Small (sm) Bold Right
      </Label>
      <Label size="base" weight="normal" align="left" className="border-1 border-dotted border-gray-300">
        Base Normal Left
      </Label>
      <Label size="base" weight="normal" align="center" className="border-1 border-dotted border-gray-300">
        Base Normal Center
      </Label>
      <Label size="base" weight="normal" align="right" className="border-1 border-dotted border-gray-300">
        Base Normal Right
      </Label>
      <Label size="base" weight="bold" align="left" className="border-1 border-dotted border-gray-300">
        Base Bold Left
      </Label>
      <Label size="base" weight="bold" align="center" className="border-1 border-dotted border-gray-300">
        Base Bold Center
      </Label>
      <Label size="base" weight="bold" align="right" className="border-1 border-dotted border-gray-300">
        Base Bold Right
      </Label>
      <Label size="lg" weight="normal" align="left" className="border-1 border-dotted border-gray-300">
        Large (lg) Normal Left
      </Label>
      <Label size="lg" weight="normal" align="center" className="border-1 border-dotted border-gray-300">
        Large (lg) Normal Center
      </Label>
      <Label size="lg" weight="normal" align="right" className="border-1 border-dotted border-gray-300">
        Large (lg) Normal Right
      </Label>
      <Label size="lg" weight="bold" align="left" className="border-1 border-dotted border-gray-300">
        Large (lg) Bold Left
      </Label>
      <Label size="lg" weight="bold" align="center" className="border-1 border-dotted border-gray-300">
        Large (lg) Bold Center
      </Label>
      <Label size="lg" weight="bold" align="right" className="border-1 border-dotted border-gray-300">
        Large (lg) Bold Right
      </Label>
      <Label size="xl" weight="normal" align="left" className="border-1 border-dotted border-gray-300">
        Extra Large (xl) Normal Left
      </Label>
      <Label size="xl" weight="normal" align="center" className="border-1 border-dotted border-gray-300">
        Extra Large (xl) Normal Center
      </Label>
      <Label size="xl" weight="normal" align="right" className="border-1 border-dotted border-gray-300">
        Extra Large (xl) Normal Right
      </Label>
      <Label size="xl" weight="bold" align="left" className="border-1 border-dotted border-gray-300">
        Extra Large (xl) Bold Left
      </Label>
      <Label size="xl" weight="bold" align="center" className="border-1 border-dotted border-gray-300">
        Extra Large (xl) Bold Center
      </Label>
      <Label size="xl" weight="bold" align="right" className="border-1 border-dotted border-gray-300">
        Extra Large (xl) Bold Right
      </Label>
      <Label size="2xl" weight="normal" align="left" className="border-1 border-dotted border-gray-300">
        2X Large (2xl) Normal Left
      </Label>
      <Label size="2xl" weight="normal" align="center" className="border-1 border-dotted border-gray-300">
        2X Large (2xl) Normal Center
      </Label>
      <Label size="2xl" weight="normal" align="right" className="border-1 border-dotted border-gray-300">
        2X Large (2xl) Normal Right
      </Label>
      <Label size="2xl" weight="bold" align="left" className="border-1 border-dotted border-gray-300">
        2X Large (2xl) Bold Left
      </Label>
      <Label size="2xl" weight="bold" align="center" className="border-1 border-dotted border-gray-300">
        2X Large (2xl) Bold Center
      </Label>
      <Label size="2xl" weight="bold" align="right" className="border-1 border-dotted border-gray-300">
        2X Large (2xl) Bold Right
      </Label>
      <Label size="3xl" weight="normal" align="left" className="border-1 border-dotted border-gray-300">
        3X Large (3xl) Normal Left
      </Label>
      <Label size="3xl" weight="normal" align="center" className="border-1 border-dotted border-gray-300">
        3X Large (3xl) Normal Center
      </Label>
      <Label size="3xl" weight="normal" align="right" className="border-1 border-dotted border-gray-300">
        3X Large (3xl) Normal Right
      </Label>
      <Label size="3xl" weight="bold" align="left" className="border-1 border-dotted border-gray-300">
        3X Large (3xl) Bold Left
      </Label>
      <Label size="3xl" weight="bold" align="center" className="border-1 border-dotted border-gray-300">
        3X Large (3xl) Bold Center
      </Label>
      <Label size="3xl" weight="bold" align="right" className="border-1 border-dotted border-gray-300">
        3X Large (3xl) Bold Right
      </Label>
      <Label size="4xl" weight="normal" align="left" className="border-1 border-dotted border-gray-300">
        4X Large (4xl) Normal Left
      </Label>
      <Label size="4xl" weight="normal" align="center" className="border-1 border-dotted border-gray-300">
        4X Large (4xl) Normal Center
      </Label>
      <Label size="4xl" weight="normal" align="right" className="border-1 border-dotted border-gray-300">
        4X Large (4xl) Normal Right
      </Label>
      <Label size="4xl" weight="bold" align="left" className="border-1 border-dotted border-gray-300">
        4X Large (4xl) Bold Left
      </Label>
      <Label size="4xl" weight="bold" align="center" className="border-1 border-dotted border-gray-300">
        4X Large (4xl) Bold Center
      </Label>
      <Label size="4xl" weight="bold" align="right" className="border-1 border-dotted border-gray-300">
        4X Large (4xl) Bold Right
      </Label>
    </div>
  )
};
