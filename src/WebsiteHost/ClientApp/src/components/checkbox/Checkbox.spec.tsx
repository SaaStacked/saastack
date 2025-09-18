import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import '@testing-library/jest-dom';
import Checkbox from './Checkbox';


describe('Checkbox', () => {
  it('renders with default props', () => {
    render(<Checkbox label="alabel" />);

    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).toBeInTheDocument();
    expect(checkbox).toHaveAttribute('type', 'checkbox');
    expect(checkbox).not.toBeChecked();
  });

  it('renders with label', () => {
    render(<Checkbox label="alabel" />);

    expect(screen.getByLabelText(/alabel/)).toBeInTheDocument();
    expect(screen.getByText('alabel')).toBeInTheDocument();
  });

  it('renders different sizes correctly', () => {
    const { rerender } = render(<Checkbox label="alabel" size="sm" />);

    expect(screen.getByRole('checkbox')).toHaveClass('px-3', 'py-1.5', 'text-sm');

    rerender(<Checkbox label="alabel" size="md" />);

    expect(screen.getByRole('checkbox')).toHaveClass('px-3', 'py-2', 'text-sm');

    rerender(<Checkbox label="alabel" size="lg" />);

    expect(screen.getByRole('checkbox')).toHaveClass('px-4', 'py-3', 'text-base');
  });

  it('handles disabled state correctly', () => {
    render(<Checkbox label="alabel" disabled />);

    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).toBeDisabled();
    expect(checkbox).toHaveClass('disabled:opacity-50', 'disabled:cursor-not-allowed');
  });

  it('handles checked state correctly', () => {
    render(<Checkbox label="alabel" value={true} />);

    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).toBeChecked();
  });

  it('handles unchecked state correctly', () => {
    render(<Checkbox label="alabel" value={false} />);

    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).not.toBeChecked();
  });

  it('handles error state correctly', () => {
    render(<Checkbox label="alabel" errorMessage="anerrormessage" />);

    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).toHaveClass('border-red-300', 'focus:border-red-500');
    expect(screen.getByText('anerrormessage')).toBeInTheDocument();
    expect(screen.getByText('anerrormessage')).toHaveClass('text-red-600');
  });

  it('handles change events', () => {
    const handleChange = vi.fn();
    render(<Checkbox label="alabel" onChange={handleChange} />);

    const checkbox = screen.getByRole('checkbox');
    fireEvent.click(checkbox);

    expect(handleChange).toHaveBeenCalledTimes(1);
    expect(handleChange).toHaveBeenCalledWith(
      expect.objectContaining({
        target: expect.objectContaining({ checked: true })
      })
    );
  });

  it('handles focus and blur events', () => {
    const handleFocus = vi.fn();
    const handleBlur = vi.fn();
    render(<Checkbox label="alabel" onFocus={handleFocus} onBlur={handleBlur} />);

    const checkbox = screen.getByRole('checkbox');
    fireEvent.focus(checkbox);
    expect(handleFocus).toHaveBeenCalledTimes(1);

    fireEvent.blur(checkbox);
    expect(handleBlur).toHaveBeenCalledTimes(1);
  });

  it('applies custom className', () => {
    render(<Checkbox label="alabel" className="aclass" />);

    expect(screen.getByRole('checkbox')).toHaveClass('aclass');
  });

  it('sets name and id attributes correctly', () => {
    render(<Checkbox id="anid" name="aname" label="alabel" />);

    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).toHaveAttribute('name', 'aname');
    expect(checkbox).toHaveAttribute('id', 'anid_checkbox');
  });

  it('uses random id when id is not provided', () => {
    render(<Checkbox name="aname" label="alabel" />);

    const checkbox = screen.getByRole('checkbox');
    expect(checkbox.getAttribute('id')).toMatch(/^checkbox\([\w\d]+\)$/);
  });

  it('handles controlled checkbox with value', () => {
    const { rerender } = render(<Checkbox label="alabel" value={false} onChange={() => {}} />);
    expect(screen.getByRole('checkbox')).not.toBeChecked();

    rerender(<Checkbox label="alabel" value={true} onChange={() => {}} />);
    expect(screen.getByRole('checkbox')).toBeChecked();
  });
});
