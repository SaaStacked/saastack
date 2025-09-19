import { Meta, StoryObj } from '@storybook/react';
import Button from '../button/Button.tsx';
import Alert from './Alert';


export default {
  title: 'Components/Alert',
  component: Alert,
  parameters: {
    layout: 'centered'
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
    title: 'Error Title',
    type: 'error',
    message:
      'Danger! This is a danger alert - check it out! Danger! This is a danger alert - check it out! Danger! This is a danger alert - check it out! Danger! This is a danger alert - check it out! Danger! This is a danger alert - check it out!'
  }
};

export const Warning: Story = {
  args: {
    title: 'Warning Title',
    type: 'warning',
    message: 'This is a warning message, for something to watch out for!'
  }
};

export const Success: Story = {
  args: {
    title: 'Success Title',
    type: 'success',
    message: 'This is a success message, for some successful event!'
  }
};

export const Info: Story = {
  args: {
    title: 'Information Title',
    type: 'info',
    message: 'This is an informational message, that gives you some information'
  }
};

export const MessageOnly: Story = {
  args: {
    type: 'error',
    message: 'Danger! This is a danger alert - check it out! Danger!'
  }
};

export const WithChildren: Story = {
  args: {
    title: 'This is the title',
    type: 'success',
    message: 'This is a success message, for some successful event!',
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

export const WithLotsOfContent: Story = {
  args: {
    title: 'This is the title',
    type: 'success',
    message:
      'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi faucibus commodo leo a porta. Cras hendrerit ac ipsum vel pulvinar. Suspendisse sit amet ex ac nulla mattis vestibulum nec nec velit. Sed dui odio, fringilla sed purus nec, ornare dapibus purus. Nunc vitae scelerisque lorem, a dapibus ipsum. Aliquam vulputate finibus lobortis. Cras bibendum, lorem id ultrices tempus, diam est porttitor massa, ac fermentum ligula augue eu nisl. Vestibulum eget turpis condimentum, blandit metus condimentum, vehicula nisi. Sed accumsan, turpis non sagittis tempor, massa leo aliquet mi, in suscipit quam quam vel ligula. Nulla ac massa massa. Aliquam aliquet ut nisi sit amet euismod. Praesent hendrerit tristique velit, eu tincidunt odio feugiat ut.'
  }
};
