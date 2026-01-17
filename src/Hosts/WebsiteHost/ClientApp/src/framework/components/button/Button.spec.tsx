import { fireEvent, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import '@testing-library/jest-dom';
import { renderWithTestingProviders } from '../../testing/TestingProviders.tsx';
import Button from './Button';

describe('Button', () => {
  it('renders with default props', () => {
    renderWithTestingProviders(<Button>Click me</Button>);
    const button = screen.getByRole('button', { name: /click me/i });
    expect(button).toBeInTheDocument();
    expect(button).toHaveClass('bg-brand-primary', 'text-white');
  });

  it('when brand-primary, then renders', () => {
    renderWithTestingProviders(<Button variant="brand-primary">brand-primary</Button>);
    expect(screen.getByRole('button')).toHaveClass('bg-brand-primary');
  });

  it('when brand-secondary, then renders', () => {
    renderWithTestingProviders(<Button variant="brand-secondary">Secondary</Button>);
    expect(screen.getByRole('button')).toHaveClass('bg-brand-secondary');
  });

  it('when outline, then renders', () => {
    renderWithTestingProviders(<Button variant="outline">Outline</Button>);
    expect(screen.getByRole('button')).toHaveClass('border', 'bg-white');
  });

  it('when ghost, then renders', () => {
    renderWithTestingProviders(<Button variant="ghost">Ghost</Button>);
    expect(screen.getByRole('button')).toHaveClass('text-neutral-700');
  });

  it('when danger, then renders', () => {
    renderWithTestingProviders(<Button variant="danger">Danger</Button>);
    expect(screen.getByRole('button')).toHaveClass('bg-red-700');
  });

  it('handles disabled state correctly', () => {
    renderWithTestingProviders(<Button disabled>Disabled</Button>);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
    expect(button).toHaveClass('disabled:opacity-60', 'disabled:cursor-not-allowed');
  });

  it('handles loading state correctly', () => {
    renderWithTestingProviders(<Button busy>Loading</Button>);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
    expect(screen.getByRole('button')).toContainHTML('svg');
  });

  it('renders full width correctly', () => {
    renderWithTestingProviders(<Button fullWidth>Full Width</Button>);
    expect(screen.getByRole('button')).toHaveClass('w-full');
  });

  it('handles click events', () => {
    const handleClick = vi.fn();
    renderWithTestingProviders(<Button onClick={handleClick}>Click me</Button>);

    fireEvent.click(screen.getByRole('button'));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('when disabled, does not call onClick', () => {
    const handleClick = vi.fn();
    renderWithTestingProviders(
      <Button disabled onClick={handleClick}>
        Disabled
      </Button>
    );

    fireEvent.click(screen.getByRole('button'));
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('when busy, does not call onClick', () => {
    const handleClick = vi.fn();
    renderWithTestingProviders(
      <Button busy onClick={handleClick}>
        Loading
      </Button>
    );

    fireEvent.click(screen.getByRole('button'));
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('when button type, then renders', () => {
    renderWithTestingProviders(<Button type="button">Button</Button>);
    expect(screen.getByRole('button')).toHaveAttribute('type', 'button');
  });
  it('when submit type, then renders', () => {
    renderWithTestingProviders(<Button type="submit">Submit</Button>);
    expect(screen.getByRole('button')).toHaveAttribute('type', 'submit');
  });
  it('when reset type, then renders', () => {
    renderWithTestingProviders(<Button type="reset">Reset</Button>);
    expect(screen.getByRole('button')).toHaveAttribute('type', 'reset');
  });

  it('applies custom className', () => {
    renderWithTestingProviders(<Button className="custom-class">Custom</Button>);
    expect(screen.getByRole('button')).toHaveClass('custom-class');
  });

  it('when busy, shows loading spinner', () => {
    renderWithTestingProviders(<Button busy>Loading Button</Button>);
    const spinner = screen.getByRole('button').querySelector('svg');
    expect(spinner).toBeInTheDocument();
    expect(spinner).toHaveClass('animate-spin');
  });
});
