import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
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
    render(<Input label="alabel" type="email" value="auser@company.com" />);

    expect(screen.getByLabelText(/alabel:/)).toBeInTheDocument();
  });

  it('renders with placeholder', () => {
    render(<Input placeholder="aplaceholder" />);

    expect(screen.getByPlaceholderText('aplaceholder')).toBeInTheDocument();
  });

  it('renders different input types correctly', () => {
    const { rerender } = render(<Input type="email" />);

    expect(screen.getByRole('textbox')).toHaveAttribute('type', 'email');

    rerender(<Input label="alabel" type="password" />);

    expect(screen.getByLabelText(/alabel/i).closest('input')).toHaveAttribute('type', 'password');

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
    render(<Input label="alabel" required />);

    const input = screen.getByRole('textbox');
    expect(input).toBeRequired();
    expect(screen.getByText('*')).toBeInTheDocument();
  });

  it('handles error state correctly', () => {
    render(<Input errorMessage="anerrormessage" />);

    const input = screen.getByRole('textbox');
    expect(input).toHaveClass('border-red-300', 'focus:border-red-500');
    expect(screen.getByText('anerrormessage')).toBeInTheDocument();
    expect(screen.getByText('anerrormessage')).toHaveClass('text-red-600');
  });

  it('shows help text when provided', () => {
    render(<Input hintText="ahint" />);

    expect(screen.getByText('ahint')).toBeInTheDocument();
    expect(screen.getByText('ahint')).toHaveClass('text-gray-500');
  });

  it('prioritizes error message over help text', () => {
    render(<Input errorMessage="anerrormessage" hintText="ahint" />);

    expect(screen.getByText('anerrormessage')).toBeInTheDocument();
    expect(screen.queryByText('ahint')).not.toBeInTheDocument();
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
    render(<Input className="aclass" />);

    expect(screen.getByRole('textbox')).toHaveClass('aclass');
  });

  it('sets name and id attributes correctly', () => {
    render(<Input id="anid" name="aname" />);

    const input = screen.getByRole('textbox');
    expect(input).toHaveAttribute('name', 'aname');
    expect(input).toHaveAttribute('id', 'anid_input');
  });

  it('uses random as id when id is not provided', () => {
    render(<Input name="aname" label="alabel" />);

    const input = screen.getByRole('textbox');
    expect(input.getAttribute('id')).toMatch(/^input\([\w\d]+\)$/);
  });

  it('handles controlled input with value', () => {
    const { rerender } = render(<Input value="avalue" onChange={() => {}} />);
    expect(screen.getByDisplayValue('avalue')).toBeInTheDocument();

    rerender(<Input value="anothervalue" onChange={() => {}} />);

    expect(screen.getByDisplayValue('anothervalue')).toBeInTheDocument();
  });
});
