import type { Meta, StoryObj } from '@storybook/react';
import Loader from './Loader.tsx';


const meta: Meta<typeof Loader> = {
  title: 'Components/Loader',
  component: Loader,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    type: {
      control: 'select',
      options: ['page', 'inline']
    },
    message: {
      control: 'text'
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Page: Story = {
  args: {
    type: 'page',
    message: 'Loading the things...'
  }
};

export const Inline: Story = {
  render: () => (
    <div className="space-y-4 border-2 p-4">
      <Loader type="inline" message="Loading the things..." />
    </div>
  )
};

export const InlineSmall: Story = {
  render: () => (
    <div className="space-y-4 border-2 h-1/6">
      <Loader type="inline" message="Loading the things..." />
    </div>
  )
};
