import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import Badge from './Badge';

describe('Badge', () => {
  it('renders with default props', () => {
    render(<Badge>Badge text</Badge>);
    expect(screen.getByText('Badge text')).toBeInTheDocument();
  });

  it('renders different variants correctly', () => {
    const { rerender } = render(<Badge variant="default">Default</Badge>);
    let badge = screen.getByText('Default');
    expect(badge).toHaveClass('bg-gray-100', 'text-gray-800');

    rerender(<Badge variant="primary">Primary</Badge>);
    badge = screen.getByText('Primary');
    expect(badge).toHaveClass('bg-primary-600', 'text-white');

    rerender(<Badge variant="secondary">Secondary</Badge>);
    badge = screen.getByText('Secondary');
    expect(badge).toHaveClass('bg-secondary-600', 'text-white');

    rerender(<Badge variant="success">Success</Badge>);
    badge = screen.getByText('Success');
    expect(badge).toHaveClass('bg-green-600', 'text-white');

    rerender(<Badge variant="warning">Warning</Badge>);
    badge = screen.getByText('Warning');
    expect(badge).toHaveClass('bg-yellow-600', 'text-white');

    rerender(<Badge variant="danger">Danger</Badge>);
    badge = screen.getByText('Danger');
    expect(badge).toHaveClass('bg-red-600', 'text-white');

    rerender(<Badge variant="info">Info</Badge>);
    badge = screen.getByText('Info');
    expect(badge).toHaveClass('bg-blue-600', 'text-white');
  });

  it('renders different sizes correctly', () => {
    const { rerender } = render(<Badge size="sm">Small</Badge>);
    let badge = screen.getByText('Small');
    expect(badge).toHaveClass('px-2', 'py-0.5', 'text-xs');

    rerender(<Badge size="md">Medium</Badge>);
    badge = screen.getByText('Medium');
    expect(badge).toHaveClass('px-2.5', 'py-0.5', 'text-sm');

    rerender(<Badge size="lg">Large</Badge>);
    badge = screen.getByText('Large');
    expect(badge).toHaveClass('px-3', 'py-1', 'text-sm');
  });

  it('renders different styles correctly', () => {
    const { rerender } = render(
      <Badge style="filled" variant="primary">
        Filled
      </Badge>
    );
    let badge = screen.getByText('Filled');
    expect(badge).toHaveClass('bg-primary-600', 'text-white');

    rerender(
      <Badge style="outlined" variant="primary">
        Outlined
      </Badge>
    );
    badge = screen.getByText('Outlined');
    expect(badge).toHaveClass('border', 'border-primary-600', 'text-primary-600', 'bg-white');

    rerender(
      <Badge style="soft" variant="primary">
        Soft
      </Badge>
    );
    badge = screen.getByText('Soft');
    expect(badge).toHaveClass('bg-primary-100', 'text-primary-800');
  });

  it('renders removable badge with remove button', () => {
    const handleRemove = vi.fn();
    render(
      <Badge removable onRemove={handleRemove}>
        Removable
      </Badge>
    );

    expect(screen.getByText('Removable')).toBeInTheDocument();
    const removeButton = screen.getByRole('button');
    expect(removeButton).toBeInTheDocument();

    fireEvent.click(removeButton);
    expect(handleRemove).toHaveBeenCalledTimes(1);
  });

  it('does not render remove button when not removable', () => {
    render(<Badge>Not removable</Badge>);

    expect(screen.getByText('Not removable')).toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('applies custom className', () => {
    render(<Badge className="custom-class">Custom</Badge>);
    expect(screen.getByText('Custom')).toHaveClass('custom-class');
  });

  it('renders with complex content', () => {
    render(
      <Badge>
        <span>Complex</span> content
      </Badge>
    );

    expect(screen.getByText('Complex')).toBeInTheDocument();
    expect(screen.getByText('content')).toBeInTheDocument();
  });

  it('handles remove button hover and focus states', () => {
    const handleRemove = vi.fn();
    render(
      <Badge removable onRemove={handleRemove}>
        Test
      </Badge>
    );

    const removeButton = screen.getByRole('button');
    expect(removeButton).toHaveClass('hover:bg-black', 'hover:bg-opacity-10');
    expect(removeButton).toHaveClass('focus:outline-none', 'focus:bg-black', 'focus:bg-opacity-10');
  });

  it('renders outlined style for all variants', () => {
    const variants = ['default', 'primary', 'secondary', 'success', 'warning', 'danger', 'info'] as const;

    variants.forEach((variant) => {
      const { unmount } = render(
        <Badge style="outlined" variant={variant}>
          {variant}
        </Badge>
      );
      const badge = screen.getByText(variant);
      expect(badge).toHaveClass('border', 'bg-white');
      unmount();
    });
  });

  it('renders soft style for all variants', () => {
    const variants = ['default', 'primary', 'secondary', 'success', 'warning', 'danger', 'info'] as const;

    variants.forEach((variant) => {
      const { unmount } = render(
        <Badge style="soft" variant={variant}>
          {variant}
        </Badge>
      );
      const badge = screen.getByText(variant);
      // All soft variants should have light background colors
      expect(badge.className).toMatch(/bg-\w+-100/);
      unmount();
    });
  });

  it('maintains accessibility for remove button', () => {
    const handleRemove = vi.fn();
    render(
      <Badge removable onRemove={handleRemove}>
        Accessible
      </Badge>
    );

    const removeButton = screen.getByRole('button');
    expect(removeButton).toHaveAttribute('type', 'button');

    // Test keyboard interaction
    fireEvent.keyDown(removeButton, { key: 'Enter' });
    fireEvent.click(removeButton);
    expect(handleRemove).toHaveBeenCalledTimes(1);
  });

  it('renders remove icon correctly', () => {
    render(
      <Badge removable onRemove={() => {}}>
        With Icon
      </Badge>
    );

    const removeButton = screen.getByRole('button');
    const icon = removeButton.querySelector('svg');
    expect(icon).toBeInTheDocument();
    expect(icon).toHaveClass('w-2', 'h-2');
  });
});
