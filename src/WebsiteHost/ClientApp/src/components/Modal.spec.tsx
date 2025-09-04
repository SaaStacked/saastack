import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import Modal from './Modal';

// Mock body style changes
const originalBodyStyle = document.body.style.overflow;

describe('Modal', () => {
  beforeEach(() => {
    document.body.style.overflow = originalBodyStyle;
  });

  afterEach(() => {
    document.body.style.overflow = originalBodyStyle;
  });

  it('does not render when open is false', () => {
    render(
      <Modal open={false} onClose={() => {}}>
        Modal content
      </Modal>
    );

    expect(screen.queryByText('Modal content')).not.toBeInTheDocument();
  });

  it('renders when open is true', () => {
    render(
      <Modal open={true} onClose={() => {}}>
        Modal content
      </Modal>
    );

    expect(screen.getByText('Modal content')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('renders with title', () => {
    render(
      <Modal open={true} title="Modal Title" onClose={() => {}}>
        Modal content
      </Modal>
    );

    expect(screen.getByText('Modal Title')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toHaveAttribute('aria-labelledby', 'modal-title');
  });

  it('renders different sizes correctly', () => {
    const { rerender } = render(
      <Modal open={true} size="sm" onClose={() => {}}>
        Content
      </Modal>
    );
    let modal = screen.getByRole('dialog');
    expect(modal).toHaveClass('max-w-md');

    rerender(
      <Modal open={true} size="md" onClose={() => {}}>
        Content
      </Modal>
    );
    modal = screen.getByRole('dialog');
    expect(modal).toHaveClass('max-w-lg');

    rerender(
      <Modal open={true} size="lg" onClose={() => {}}>
        Content
      </Modal>
    );
    modal = screen.getByRole('dialog');
    expect(modal).toHaveClass('max-w-2xl');

    rerender(
      <Modal open={true} size="xl" onClose={() => {}}>
        Content
      </Modal>
    );
    modal = screen.getByRole('dialog');
    expect(modal).toHaveClass('max-w-4xl');
  });

  it('shows close button by default', () => {
    render(
      <Modal open={true} title="Modal Title" onClose={() => {}}>
        Modal content
      </Modal>
    );

    expect(screen.getByRole('button')).toBeInTheDocument();
  });

  it('hides close button when showCloseButton is false', () => {
    render(
      <Modal open={true} title="Modal Title" showCloseButton={false} onClose={() => {}}>
        Modal content
      </Modal>
    );

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('calls onClose when close button is clicked', () => {
    const handleClose = vi.fn();
    render(
      <Modal open={true} title="Modal Title" onClose={handleClose}>
        Modal content
      </Modal>
    );

    fireEvent.click(screen.getByRole('button'));
    expect(handleClose).toHaveBeenCalledTimes(1);
  });

  it('calls onClose when overlay is clicked by default', () => {
    const handleClose = vi.fn();
    render(
      <Modal open={true} onClose={handleClose}>
        Modal content
      </Modal>
    );

    const overlay = screen.getByRole('dialog').parentElement;
    if (overlay) {
      fireEvent.click(overlay);
      expect(handleClose).toHaveBeenCalledTimes(1);
    }
  });

  it('does not call onClose when overlay is clicked and closeOnOverlayClick is false', () => {
    const handleClose = vi.fn();
    render(
      <Modal open={true} closeOnOverlayClick={false} onClose={handleClose}>
        Modal content
      </Modal>
    );

    const overlay = screen.getByRole('dialog').parentElement;
    if (overlay) {
      fireEvent.click(overlay);
      expect(handleClose).not.toHaveBeenCalled();
    }
  });

  it('does not call onClose when modal content is clicked', () => {
    const handleClose = vi.fn();
    render(
      <Modal open={true} onClose={handleClose}>
        Modal content
      </Modal>
    );

    fireEvent.click(screen.getByRole('dialog'));
    expect(handleClose).not.toHaveBeenCalled();
  });

  it('calls onClose when Escape key is pressed', async () => {
    const handleClose = vi.fn();
    render(
      <Modal open={true} onClose={handleClose}>
        Modal content
      </Modal>
    );

    fireEvent.keyDown(document, { key: 'Escape' });
    await waitFor(() => expect(handleClose).toHaveBeenCalledTimes(1));
  });

  it('does not call onClose when other keys are pressed', () => {
    const handleClose = vi.fn();
    render(
      <Modal open={true} onClose={handleClose}>
        Modal content
      </Modal>
    );

    fireEvent.keyDown(document, { key: 'Enter' });
    fireEvent.keyDown(document, { key: 'Space' });
    expect(handleClose).not.toHaveBeenCalled();
  });

  it('sets body overflow to hidden when open', () => {
    render(
      <Modal open={true} onClose={() => {}}>
        Modal content
      </Modal>
    );

    expect(document.body.style.overflow).toBe('hidden');
  });

  it('resets body overflow when closed', () => {
    const { rerender } = render(
      <Modal open={true} onClose={() => {}}>
        Modal content
      </Modal>
    );

    expect(document.body.style.overflow).toBe('hidden');

    rerender(
      <Modal open={false} onClose={() => {}}>
        Modal content
      </Modal>
    );

    expect(document.body.style.overflow).toBe('unset');
  });

  it('applies custom className', () => {
    render(
      <Modal open={true} className="custom-class" onClose={() => {}}>
        Modal content
      </Modal>
    );

    expect(screen.getByRole('dialog')).toHaveClass('custom-class');
  });

  it('has correct accessibility attributes', () => {
    render(
      <Modal open={true} title="Accessible Modal" onClose={() => {}}>
        Modal content
      </Modal>
    );

    const modal = screen.getByRole('dialog');
    expect(modal).toHaveAttribute('aria-modal', 'true');
    expect(modal).toHaveAttribute('aria-labelledby', 'modal-title');
  });

  it('renders complex content correctly', () => {
    render(
      <Modal open={true} title="Complex Modal" onClose={() => {}}>
        <div>
          <p>First paragraph</p>
          <button>Action button</button>
          <input placeholder="Input field" />
        </div>
      </Modal>
    );

    expect(screen.getByText('Complex Modal')).toBeInTheDocument();
    expect(screen.getByText('First paragraph')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Action button' })).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Input field')).toBeInTheDocument();
  });

  it('handles rapid open/close state changes', async () => {
    const handleClose = vi.fn();
    const { rerender } = render(
      <Modal open={false} onClose={handleClose}>
        Modal content
      </Modal>
    );

    rerender(
      <Modal open={true} onClose={handleClose}>
        Modal content
      </Modal>
    );

    expect(screen.getByText('Modal content')).toBeInTheDocument();

    rerender(
      <Modal open={false} onClose={handleClose}>
        Modal content
      </Modal>
    );

    expect(screen.queryByText('Modal content')).not.toBeInTheDocument();
  });
});
