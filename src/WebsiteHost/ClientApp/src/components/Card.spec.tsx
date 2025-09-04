import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import Card from './Card';

describe('Card', () => {
  it('renders with default props', () => {
    render(<Card>Card content</Card>);
    expect(screen.getByText('Card content')).toBeInTheDocument();
  });

  it('renders with title', () => {
    render(<Card title="Card Title">Card content</Card>);
    expect(screen.getByText('Card Title')).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 3 })).toHaveTextContent('Card Title');
  });

  it('renders with title and subtitle', () => {
    render(
      <Card title="Card Title" subtitle="Card subtitle">
        Card content
      </Card>
    );
    expect(screen.getByText('Card Title')).toBeInTheDocument();
    expect(screen.getByText('Card subtitle')).toBeInTheDocument();
  });

  it('renders different variants correctly', () => {
    const { rerender } = render(<Card variant="default">Content</Card>);
    let cardElement = screen.getByText('Content').parentElement;
    expect(cardElement).toHaveClass('border', 'border-gray-200');

    rerender(<Card variant="outlined">Content</Card>);
    cardElement = screen.getByText('Content').parentElement;
    expect(cardElement).toHaveClass('border-2', 'border-gray-300');

    rerender(<Card variant="elevated">Content</Card>);
    cardElement = screen.getByText('Content').parentElement;
    expect(cardElement).toHaveClass('shadow-lg', 'border-gray-100');
  });

  it('renders different padding sizes correctly', () => {
    const { rerender } = render(<Card padding="none">Content</Card>);
    let cardElement = screen.getByText('Content').parentElement;
    expect(cardElement).not.toHaveClass('p-3', 'p-4', 'p-6');

    rerender(<Card padding="sm">Content</Card>);
    cardElement = screen.getByText('Content').parentElement;
    expect(cardElement).toHaveClass('p-3');

    rerender(<Card padding="md">Content</Card>);
    cardElement = screen.getByText('Content').parentElement;
    expect(cardElement).toHaveClass('p-4');

    rerender(<Card padding="lg">Content</Card>);
    cardElement = screen.getByText('Content').parentElement;
    expect(cardElement).toHaveClass('p-6');
  });

  it('renders as clickable when clickable prop is true', () => {
    const handleClick = vi.fn();
    render(
      <Card clickable onClick={handleClick}>
        Clickable content
      </Card>
    );

    const cardElement = screen.getByRole('button');
    expect(cardElement).toBeInTheDocument();
    expect(cardElement).toHaveClass('cursor-pointer');

    fireEvent.click(cardElement);
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('renders as div when not clickable', () => {
    render(<Card>Non-clickable content</Card>);

    const cardElement = screen.getByText('Non-clickable content').closest('div');
    expect(cardElement).not.toHaveAttribute('type');
    expect(cardElement?.tagName).toBe('DIV');
  });

  it('applies hover styles when clickable', () => {
    render(<Card clickable>Clickable content</Card>);

    const cardElement = screen.getByRole('button');
    expect(cardElement).toHaveClass('hover:shadow-md', 'hover:border-gray-300');
  });

  it('applies custom className', () => {
    render(<Card className="custom-class">Content</Card>);

    const cardElement = screen.getByText('Content').parentElement;
    expect(cardElement).toHaveClass('custom-class');
  });

  it('handles complex content structure', () => {
    render(
      <Card title="Complex Card" subtitle="With multiple elements">
        <div>
          <p>First paragraph</p>
          <p>Second paragraph</p>
          <button>Action button</button>
        </div>
      </Card>
    );

    expect(screen.getByText('Complex Card')).toBeInTheDocument();
    expect(screen.getByText('With multiple elements')).toBeInTheDocument();
    expect(screen.getByText('First paragraph')).toBeInTheDocument();
    expect(screen.getByText('Second paragraph')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Action button' })).toBeInTheDocument();
  });

  it('handles title without subtitle', () => {
    render(<Card title="Only Title">Content</Card>);

    expect(screen.getByText('Only Title')).toBeInTheDocument();
    expect(screen.getByText('Only Title')).toHaveClass('mb-1');
  });

  it('handles subtitle without title', () => {
    render(<Card subtitle="Only Subtitle">Content</Card>);

    expect(screen.getByText('Only Subtitle')).toBeInTheDocument();
    expect(screen.getByText('Only Subtitle')).toHaveClass('text-gray-600');
  });

  it('applies correct padding with title and no padding', () => {
    render(
      <Card title="Title" padding="none">
        Content
      </Card>
    );

    // Title should have padding even when card padding is none
    const titleContainer = screen.getByText('Title').closest('div');
    expect(titleContainer).toHaveClass('p-4', 'pb-3');
  });

  it('does not call onClick when not clickable', () => {
    const handleClick = vi.fn();
    render(<Card onClick={handleClick}>Non-clickable content</Card>);

    const cardElement = screen.getByText('Non-clickable content').closest('div');
    if (cardElement) {
      fireEvent.click(cardElement);
    }
    expect(handleClick).not.toHaveBeenCalled();
  });
});
