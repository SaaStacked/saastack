import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import Input from './Input';

describe('Input', () => {
  it('renders with default props', () => {
    render(<Input />);
    const input = screen.getByRole('textbox');
    expect(input).toBeInTheDocument();
    expect(input).toHaveAttribute('type', 'text');
  });

  it('renders with label', () => {
    render(<Input label="Email Address" />);
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument();
    expect(screen.getByText('Email Address')).toBeInTheDocument();
  });

  it('renders with placeholder', () => {
    render(<Input placeholder="Enter your email" />);
    expect(screen.getByPlaceholderText('Enter your email')).toBeInTheDocument();
  });

  it('renders different input types correctly', () => {
    const { rerender } = render(<Input type="email" />);
    expect(screen.getByRole('textbox')).toHaveAttribute('type', 'email');

    rerender(<Input label="Password" type="password" />);
    expect(screen.getByLabelText(/Password/i).closest('input')).toHaveAttribute('type', 'password');

    rerender(<Input type="number" />);
    expect(screen.getByRole('spinbutton')).toHaveAttribute('type', 'number');
  });

  it('renders different sizes correctly', () => {
    const { rerender } = render(<Input size="sm" />);
    expect(screen.getByRole('textbox')).toHaveClass('px-3', 'py-1.5', 'text-sm');

    rerender(<Input size="md" />);
    expect(screen.getByRole('textbox')).toHaveClass('px-3', 'py-2', 'text-sm');

    rerender(<Input size="lg" />);
    expect(screen.getByRole('textbox')).toHaveClass('px-4', 'py-3', 'text-base');
  });

  it('handles disabled state correctly', () => {
    render(<Input disabled />);
    const input = screen.getByRole('textbox');
    expect(input).toBeDisabled();
    expect(input).toHaveClass('disabled:opacity-50', 'disabled:cursor-not-allowed');
  });

  it('handles required state correctly', () => {
    render(<Input label="Required Field" required />);
    const input = screen.getByRole('textbox');
    expect(input).toBeRequired();
    expect(screen.getByText('*')).toBeInTheDocument();
  });

  it('handles error state correctly', () => {
    render(<Input error errorMessage="This field is required" />);
    const input = screen.getByRole('textbox');
    expect(input).toHaveClass('border-red-300', 'focus:border-red-500');
    expect(screen.getByText('This field is required')).toBeInTheDocument();
    expect(screen.getByText('This field is required')).toHaveClass('text-red-600');
  });

  it('shows help text when provided', () => {
    render(<Input helpText="Enter a valid email address" />);
    expect(screen.getByText('Enter a valid email address')).toBeInTheDocument();
    expect(screen.getByText('Enter a valid email address')).toHaveClass('text-gray-500');
  });

  it('prioritizes error message over help text', () => {
    render(<Input error errorMessage="Invalid email" helpText="Enter a valid email address" />);
    expect(screen.getByText('Invalid email')).toBeInTheDocument();
    expect(screen.queryByText('Enter a valid email address')).not.toBeInTheDocument();
  });

  it('renders full width correctly', () => {
    render(<Input fullWidth />);
    const container = screen.getByRole('textbox').parentElement;
    expect(container).toHaveClass('w-full');
    expect(screen.getByRole('textbox')).toHaveClass('w-full');
  });

  it('handles change events', () => {
    const handleChange = vi.fn();
    render(<Input onChange={handleChange} />);

    const input = screen.getByRole('textbox');
    fireEvent.change(input, { target: { value: 'test value' } });

    expect(handleChange).toHaveBeenCalledTimes(1);
    expect(handleChange).toHaveBeenCalledWith(
      expect.objectContaining({
        target: expect.objectContaining({ value: 'test value' })
      })
    );
  });

  it('handles focus and blur events', () => {
    const handleFocus = vi.fn();
    const handleBlur = vi.fn();
    render(<Input onFocus={handleFocus} onBlur={handleBlur} />);

    const input = screen.getByRole('textbox');
    fireEvent.focus(input);
    expect(handleFocus).toHaveBeenCalledTimes(1);

    fireEvent.blur(input);
    expect(handleBlur).toHaveBeenCalledTimes(1);
  });

  it('applies custom className', () => {
    render(<Input className="custom-class" />);
    expect(screen.getByRole('textbox')).toHaveClass('custom-class');
  });

  it('sets name and id attributes correctly', () => {
    render(<Input name="email" id="email-input" />);
    const input = screen.getByRole('textbox');
    expect(input).toHaveAttribute('name', 'email');
    expect(input).toHaveAttribute('id', 'email-input');
  });

  it('uses random as id when id is not provided', () => {
    render(<Input name="email" label="Email" />);
    const input = screen.getByRole('textbox');
    expect(input.getAttribute('id')).toMatch(/^input-\w+$/);
    expect(screen.getByLabelText('Email')).toBe(input);
  });

  it('handles controlled input with value', () => {
    const { rerender } = render(<Input value="initial value" onChange={() => {}} />);
    expect(screen.getByDisplayValue('initial value')).toBeInTheDocument();

    rerender(<Input value="updated value" onChange={() => {}} />);
    expect(screen.getByDisplayValue('updated value')).toBeInTheDocument();
  });

  it('handles uncontrolled input with defaultValue', () => {
    render(<Input defaultValue="default value" />);
    expect(screen.getByDisplayValue('default value')).toBeInTheDocument();
  });
});
