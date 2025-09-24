import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import '@testing-library/jest-dom';
import Select from './Select';

const mockOptions = [
  { value: 'anoption1', label: 'avalue1' },
  { value: 'anoption2', label: 'avalue2' },
  { value: 'anoption3', label: 'avalue3' }
];

describe('Select', () => {
  it('renders with default props', () => {
    render(<Select options={mockOptions} />);

    const select = screen.getByRole('combobox');
    expect(select).toBeInTheDocument();
    expect(select).toHaveClass('p-0', 'text-sm');
  });

  it('renders with label', () => {
    render(<Select label="alabel" options={mockOptions} />);

    expect(screen.getByLabelText(/alabel:/)).toBeInTheDocument();
  });

  it('renders with placeholder', () => {
    render(<Select placeholder="aplaceholder" value="" options={mockOptions} />);

    expect(screen.getByDisplayValue('aplaceholder')).toBeInTheDocument();
  });

  it('renders different sizes correctly', () => {
    const { rerender } = render(<Select size="sm" options={mockOptions} />);

    expect(screen.getByRole('combobox')).toHaveClass('px-3', 'py-1.5', 'text-sm');

    rerender(<Select size="md" options={mockOptions} />);

    expect(screen.getByRole('combobox')).toHaveClass('p-0', 'text-sm');

    rerender(<Select size="lg" options={mockOptions} />);

    expect(screen.getByRole('combobox')).toHaveClass('px-4', 'py-3', 'text-base');
  });

  it('handles disabled state correctly', () => {
    render(<Select disabled options={mockOptions} />);

    const select = screen.getByRole('combobox');
    expect(select).toBeDisabled();
    expect(select).toHaveClass('disabled:opacity-50', 'disabled:cursor-not-allowed');
  });

  it('handles required state correctly', () => {
    render(<Select label="alabel" required options={mockOptions} />);

    const select = screen.getByRole('combobox');
    expect(select).toBeRequired();
    expect(screen.getByText('*')).toBeInTheDocument();
  });

  it('handles error state correctly', () => {
    render(<Select errorMessage="anerrormessage" options={mockOptions} />);

    const select = screen.getByRole('combobox');
    expect(select).toHaveClass('border-red-300', 'focus:border-red-500');
    expect(screen.getByText('anerrormessage')).toBeInTheDocument();
    expect(screen.getByText('anerrormessage')).toHaveClass('text-red-600');
  });

  it('shows help text when provided', () => {
    render(<Select hintText="ahint" options={mockOptions} />);

    expect(screen.getByText('ahint')).toBeInTheDocument();
    expect(screen.getByText('ahint')).toHaveClass('text-gray-500');
  });

  it('prioritizes error message over help text', () => {
    render(<Select errorMessage="anerrormessage" hintText="ahint" options={mockOptions} />);

    expect(screen.getByText('anerrormessage')).toBeInTheDocument();
    expect(screen.queryByText('ahint')).not.toBeInTheDocument();
  });

  it('handles change events', () => {
    const handleChange = vi.fn();
    render(<Select onChange={handleChange} options={mockOptions} />);

    const select = screen.getByRole('combobox');
    fireEvent.change(select, { target: { value: 'anoption2' } });

    expect(handleChange).toHaveBeenCalledTimes(1);
    expect(handleChange).toHaveBeenCalledWith(
      expect.objectContaining({
        target: expect.objectContaining({ value: 'anoption2' })
      })
    );
  });

  it('handles focus and blur events', () => {
    const handleFocus = vi.fn();
    const handleBlur = vi.fn();
    render(<Select onFocus={handleFocus} onBlur={handleBlur} options={mockOptions} />);

    const select = screen.getByRole('combobox');
    fireEvent.focus(select);
    expect(handleFocus).toHaveBeenCalledTimes(1);

    fireEvent.blur(select);
    expect(handleBlur).toHaveBeenCalledTimes(1);
  });

  it('applies custom className', () => {
    render(<Select className="aclass" options={mockOptions} />);

    expect(screen.getByRole('combobox')).toHaveClass('aclass');
  });

  it('sets name and id attributes correctly', () => {
    render(<Select id="anid" name="aname" options={mockOptions} />);

    const select = screen.getByRole('combobox');
    expect(select).toHaveAttribute('name', 'aname');
    expect(select).toHaveAttribute('id', 'anid_select');
  });

  it('uses random id when id is not provided', () => {
    render(<Select name="aname" label="alabel" options={mockOptions} />);

    const select = screen.getByRole('combobox');
    expect(select.getAttribute('id')).toMatch(/^select\([\w\d]+\)$/);
  });

  it('handles controlled select with value', () => {
    const { rerender } = render(<Select value="anoption1" onChange={() => {}} options={mockOptions} />);
    expect(screen.getByDisplayValue('avalue1')).toBeInTheDocument();

    rerender(<Select value="anoption2" onChange={() => {}} options={mockOptions} />);

    expect(screen.getByDisplayValue('avalue2')).toBeInTheDocument();
  });

  it('renders all options correctly', () => {
    render(<Select options={mockOptions} />);

    mockOptions.forEach((option) => expect(screen.getByText(option.label)).toBeInTheDocument());
  });

  it('handles fullWidth prop correctly', () => {
    render(<Select fullWidth options={mockOptions} />);

    expect(screen.getByRole('combobox')).toHaveClass('w-full');
  });
});
