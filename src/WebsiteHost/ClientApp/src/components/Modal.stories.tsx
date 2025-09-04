import type { Meta, StoryObj } from '@storybook/react';
import { useState } from 'react';
import Modal from './Modal';
import Button from './Button';

const meta: Meta<typeof Modal> = {
  title: 'Components/Modal',
  component: Modal,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    size: {
      control: { type: 'select' },
      options: ['sm', 'md', 'lg', 'xl']
    },
    closeOnOverlayClick: {
      control: { type: 'boolean' }
    },
    showCloseButton: {
      control: { type: 'boolean' }
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

const ModalWrapper = ({ children, ...args }: any) => {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button onClick={() => setOpen(true)}>Open Modal</Button>
      <Modal {...args} open={open} onClose={() => setOpen(false)}>
        {children}
      </Modal>
    </>
  );
};

export const Default: Story = {
  render: (args) => (
    <ModalWrapper {...args}>
      <p className="text-gray-600">This is a basic modal with some content inside it.</p>
    </ModalWrapper>
  )
};

export const WithTitle: Story = {
  render: (args) => (
    <ModalWrapper {...args} title="Modal Title">
      <p className="text-gray-600">This modal has a title and some content.</p>
    </ModalWrapper>
  )
};

export const Small: Story = {
  render: (args) => (
    <ModalWrapper {...args} title="Small Modal" size="sm">
      <p className="text-gray-600">This is a small modal.</p>
    </ModalWrapper>
  )
};

export const Large: Story = {
  render: (args) => (
    <ModalWrapper {...args} title="Large Modal" size="lg">
      <div className="space-y-4">
        <p className="text-gray-600">This is a large modal with more content.</p>
        <p className="text-gray-600">It can contain multiple paragraphs and other elements.</p>
        <div className="bg-gray-100 p-4 rounded">
          <p className="text-sm text-gray-700">This is some additional content in a highlighted box.</p>
        </div>
      </div>
    </ModalWrapper>
  )
};

export const WithActions: Story = {
  render: (args) => {
    const [open, setOpen] = useState(false);

    return (
      <>
        <Button onClick={() => setOpen(true)}>Open Modal with Actions</Button>
        <Modal {...args} title="Confirm Action" open={open} onClose={() => setOpen(false)}>
          <div className="space-y-4">
            <p className="text-gray-600">Are you sure you want to perform this action? This cannot be undone.</p>
            <div className="flex justify-end space-x-2">
              <Button variant="outline" onClick={() => setOpen(false)}>
                Cancel
              </Button>
              <Button variant="danger" onClick={() => setOpen(false)}>
                Confirm
              </Button>
            </div>
          </div>
        </Modal>
      </>
    );
  }
};

export const NoCloseButton: Story = {
  render: (args) => (
    <ModalWrapper {...args} title="No Close Button" showCloseButton={false}>
      <div className="space-y-4">
        <p className="text-gray-600">This modal doesn't have a close button in the header.</p>
        <Button variant="outline" onClick={() => {}}>
          Close Modal
        </Button>
      </div>
    </ModalWrapper>
  )
};

export const NoOverlayClose: Story = {
  render: (args) => (
    <ModalWrapper {...args} title="No Overlay Close" closeOnOverlayClick={false}>
      <div className="space-y-4">
        <p className="text-gray-600">This modal cannot be closed by clicking the overlay.</p>
        <p className="text-sm text-gray-500">Use the close button or press Escape to close.</p>
      </div>
    </ModalWrapper>
  )
};
