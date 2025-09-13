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
      options: ['xs', 'sm', 'base', 'lg', 'xl', '2xl', '3xl']
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
  }
};

export const LargeCenter: Story = {
  args: {
    size: '2xl',
    weight: 'bold',
    align: 'center',
    children: 'Large Bold Label'
  }
};

export const SmallRight: Story = {
  args: {
    size: 'xs',
    weight: 'light',
    align: 'right',
    children: 'Small light label'
  }
};

export const AllSizes: Story = {
  render: () => (
    <div className="space-y-2">
      Bold
      <Label id="label1" size="xs" weight="normal" align="left">
        Extra Small (xs) Normal Left
      </Label>
      <Label id="label2" size="xs" weight="normal" align="center">
        Extra Small (xs) Normal Center
      </Label>
      <Label id="label3" size="xs" weight="normal" align="right">
        Extra Small (xs) Normal Right
      </Label>
      <Label id="label4" size="xs" weight="bold" align="left">
        Extra Small (xs) Bold Left
      </Label>
      <Label id="label5" size="xs" weight="bold" align="center">
        Extra Small (xs) Bold Center
      </Label>
      <Label id="label6" size="xs" weight="bold" align="right">
        Extra Small (xs) Bold Right
      </Label>
      <Label id="label7" size="sm" weight="normal" align="left">
        Small (sm) Normal Left
      </Label>
      <Label id="label8" size="sm" weight="normal" align="center">
        Small (sm) Normal Center
      </Label>
      <Label id="label9" size="sm" weight="normal" align="right">
        Small (sm) Normal Right
      </Label>
      <Label id="label10" size="sm" weight="bold" align="left">
        Small (sm) Bold Left
      </Label>
      <Label id="label11" size="sm" weight="bold" align="center">
        Small (sm) Bold Center
      </Label>
      <Label id="label12" size="sm" weight="bold" align="right">
        Small (sm) Bold Right
      </Label>
      <Label id="label13" size="base" weight="normal" align="left">
        Base Normal Left
      </Label>
      <Label id="label14" size="base" weight="normal" align="center">
        Base Normal Center
      </Label>
      <Label id="label15" size="base" weight="normal" align="right">
        Base Normal Right
      </Label>
      <Label id="label16" size="base" weight="bold" align="left">
        Base Bold Left
      </Label>
      <Label id="label17" size="base" weight="bold" align="center">
        Base Bold Center
      </Label>
      <Label id="label18" size="base" weight="bold" align="right">
        Base Bold Right
      </Label>
      <Label id="label19" size="lg" weight="normal" align="left">
        Large (lg) Normal Left
      </Label>
      <Label id="label20" size="lg" weight="normal" align="center">
        Large (lg) Normal Center
      </Label>
      <Label id="label21" size="lg" weight="normal" align="right">
        Large (lg) Normal Right
      </Label>
      <Label id="label22" size="lg" weight="bold" align="left">
        Large (lg) Bold Left
      </Label>
      <Label id="label23" size="lg" weight="bold" align="center">
        Large (lg) Bold Center
      </Label>
      <Label id="label24" size="lg" weight="bold" align="right">
        Large (lg) Bold Right
      </Label>
      <Label id="label25" size="xl" weight="normal" align="left">
        Extra Large (xl) Normal Left
      </Label>
      <Label id="label26" size="xl" weight="normal" align="center">
        Extra Large (xl) Normal Center
      </Label>
      <Label id="label27" size="xl" weight="normal" align="right">
        Extra Large (xl) Normal Right
      </Label>
      <Label id="label28" size="xl" weight="bold" align="left">
        Extra Large (xl) Bold Left
      </Label>
      <Label id="label29" size="xl" weight="bold" align="center">
        Extra Large (xl) Bold Center
      </Label>
      <Label id="label30" size="xl" weight="bold" align="right">
        Extra Large (xl) Bold Right
      </Label>
      <Label id="label31" size="2xl" weight="normal" align="left">
        2X Large (2xl) Normal Left
      </Label>
      <Label id="label32" size="2xl" weight="normal" align="center">
        2X Large (2xl) Normal Center
      </Label>
      <Label id="label33" size="2xl" weight="normal" align="right">
        2X Large (2xl) Normal Right
      </Label>
      <Label id="label34" size="2xl" weight="bold" align="left">
        2X Large (2xl) Bold Left
      </Label>
      <Label id="label35" size="2xl" weight="bold" align="center">
        2X Large (2xl) Bold Center
      </Label>
      <Label id="label36" size="2xl" weight="bold" align="right">
        2X Large (2xl) Bold Right
      </Label>
      <Label id="label37" size="3xl" weight="normal" align="left">
        3X Large (3xl) Normal Left
      </Label>
      <Label id="label38" size="3xl" weight="normal" align="center">
        3X Large (3xl) Normal Center
      </Label>
      <Label id="label39" size="3xl" weight="normal" align="right">
        3X Large (3xl) Normal Left
      </Label>
      <Label id="label40" size="3xl" weight="bold" align="left">
        3X Large (3xl) Bold Left
      </Label>
      <Label id="label41" size="3xl" weight="bold" align="center">
        3X Large (3xl) Bold Center
      </Label>
      <Label id="label42" size="3xl" weight="bold" align="right">
        3X Large (3xl) Bold Left
      </Label>
    </div>
  )
};
