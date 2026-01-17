import type { Meta, StoryObj } from '@storybook/react';
import { Tabs } from './Tabs.tsx';


const meta: Meta<typeof Tabs> = {
  title: 'Components/Navigation/Tabs',
  component: Tabs,
  parameters: {
    layout: 'padded'
  },
  tags: ['autodocs'],
  argTypes: {
    tabs: {
      control: 'object',
      description: 'Array of tabs with id, label, and content properties'
    },
    defaultTab: {
      control: 'text',
      description: 'ID of the default tab to show'
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    tabs: [
      {
        id: 'dashboard',
        label: 'Dashboard',
        content: (
          <div className="p-4 bg-neutral-50 dark:bg-neutral-800 rounded-lg">
            <h3 className="text-lg font-semibold mb-2">Dashboard Content</h3>
            <p>This is the dashboard tab content. It shows when the dashboard tab is active.</p>
          </div>
        )
      },
      {
        id: 'settings',
        label: 'Settings',
        content: (
          <div className="p-4 bg-neutral-50 dark:bg-neutral-800 rounded-lg">
            <h3 className="text-lg font-semibold mb-2">Settings Content</h3>
            <p>This is the settings tab content. Configure your preferences here.</p>
          </div>
        )
      },
      {
        id: 'profile',
        label: 'Profile',
        content: (
          <div className="p-4 bg-neutral-50 dark:bg-neutral-800 rounded-lg">
            <h3 className="text-lg font-semibold mb-2">Profile Content</h3>
            <p>This is the profile tab content. View and edit your profile information.</p>
          </div>
        )
      }
    ]
  }
};

export const TwoTabs: Story = {
  args: {
    tabs: [
      {
        id: 'overview',
        label: 'Overview',
        content: (
          <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
            <h3 className="text-lg font-semibold mb-2">Overview</h3>
            <p>High-level summary of your data and metrics.</p>
          </div>
        )
      },
      {
        id: 'details',
        label: 'Details',
        content: (
          <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
            <h3 className="text-lg font-semibold mb-2">Details</h3>
            <p>Detailed information and analytics.</p>
          </div>
        )
      }
    ]
  }
};

export const WithDefaultTab: Story = {
  args: {
    defaultTab: 'settings',
    tabs: [
      {
        id: 'account',
        label: 'Account',
        content: <div className="p-4">Account settings and information</div>
      },
      {
        id: 'settings',
        label: 'Settings',
        content: <div className="p-4">Application settings (this is the default tab)</div>
      },
      {
        id: 'notifications',
        label: 'Notifications',
        content: <div className="p-4">Notification preferences</div>
      }
    ]
  }
};

export const RealWorldExample: Story = {
  args: {
    tabs: [
      {
        id: 'rfs',
        label: 'Request for Service',
        content: (
          <div className="p-6">
            <h2 className="text-2xl font-bold mb-4">Request for Service</h2>
            <p className="mb-4">Create and manage service requests.</p>
            <button className="px-4 py-2 bg-brand-primary text-white rounded-md">New Request</button>
          </div>
        )
      },
      {
        id: 'program',
        label: 'Programme',
        content: (
          <div className="p-6">
            <h2 className="text-2xl font-bold mb-4">Programme Dashboard</h2>
            <p className="mb-4">View and manage your programmes.</p>
            <div className="grid grid-cols-2 gap-4">
              <div className="p-4 border rounded-lg">Active Programmes: 5</div>
              <div className="p-4 border rounded-lg">Pending Accruals: 12</div>
            </div>
          </div>
        )
      }
    ]
  }
};
