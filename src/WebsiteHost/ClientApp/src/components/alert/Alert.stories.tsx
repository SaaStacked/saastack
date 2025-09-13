import { Meta, StoryObj } from '@storybook/react';
import Button from '../Button.tsx';
import Alert from './Alert';

export default {
  title: 'Components/Alert',
  component: Alert,
  parameters: {
    layout: 'padded'
  },
  tags: ['autodocs'],
  argTypes: {
    type: {
      control: 'select',
      options: ['info', 'error', 'warning', 'success']
    },
    message: {
      control: 'text'
    },
    title: {
      control: 'text'
    }
  }
} as Meta<typeof Alert>;

type Story = StoryObj<typeof Alert>;

export const Error: Story = {
  args: {
    id: 'aprefix',
    type: 'error',
    message:
      'Danger! This is a danger alert - check it out! Danger! This is a danger alert - check it out! Danger! This is a danger alert - check it out! Danger! This is a danger alert - check it out! Danger! This is a danger alert - check it out!'
  }
};

export const Info: Story = {
  args: {
    id: 'aprefix',
    type: 'info',
    message: 'This is informational'
  }
};

export const Success: Story = {
  args: {
    id: 'aprefix',
    type: 'success',
    message: 'This is a success message'
  }
};

export const Warning: Story = {
  args: {
    id: 'aprefix',
    type: 'warning',
    message: 'This is a warning message'
  }
};

export const WithChildren: Story = {
  args: {
    id: 'aprefix',
    type: 'success',
    message: 'This is a message',
    children: (
      <>
        <p>This is a plain text child element</p>
        <div>
          <p style={{ fontWeight: 'bold' }}>This is a bolded text child element</p>
          <p>This is a button child element:</p>
          <Button id="abutton" variant="outline">
            Action
          </Button>
        </div>
      </>
    )
  }
};

export const WithTitle: Story = {
  args: {
    id: 'aprefix',
    type: 'success',
    title: 'This is the title',
    message:
      'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi faucibus commodo leo a porta. Cras hendrerit ac ipsum vel pulvinar. Suspendisse sit amet ex ac nulla mattis vestibulum nec nec velit. Sed dui odio, fringilla sed purus nec, ornare dapibus purus. Nunc vitae scelerisque lorem, a dapibus ipsum. Aliquam vulputate finibus lobortis. Cras bibendum, lorem id ultrices tempus, diam est porttitor massa, ac fermentum ligula augue eu nisl. Vestibulum eget turpis condimentum, blandit metus condimentum, vehicula nisi. Sed accumsan, turpis non sagittis tempor, massa leo aliquet mi, in suscipit quam quam vel ligula. Nulla ac massa massa. Aliquam aliquet ut nisi sit amet euismod. Praesent hendrerit tristique velit, eu tincidunt odio feugiat ut.'
  }
};
