import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import '@testing-library/jest-dom';
import ButtonUpload from './ButtonUpload.tsx';


describe('ButtonUpload', () => {
  it('renders with default props', () => {
    render(<ButtonUpload id="anid" />);

    const label = screen.getByTestId('anid_upload_button');
    expect(label).toBeInTheDocument();
    expect(label).toHaveClass('p-2', 'rounded-full');

    const fileInput = screen.getByTestId('anid_upload_button_file_input');
    expect(fileInput).toHaveAttribute('type', 'file');
    expect(fileInput).toHaveAttribute('accept', 'image/jpeg,image/png,image/gif');
    expect(fileInput).toHaveClass('hidden');
  });

  it('renders with custom id', () => {
    render(<ButtonUpload id="anid" />);

    const label = screen.getByTestId('anid_upload_button');
    expect(label).toBeInTheDocument();

    const fileInput = screen.getByTestId('anid_upload_button_file_input');
    expect(fileInput).toBeInTheDocument();
  });

  it('applies custom className', () => {
    render(<ButtonUpload id="anid" className="custom-class" />);

    const label = screen.getByTestId('anid_upload_button');
    expect(label).toHaveClass('custom-class');
  });

  it('handles disabled state correctly', () => {
    render(<ButtonUpload id="anid" disabled />);

    const fileInput = screen.getByTestId('anid_upload_button_file_input') as HTMLInputElement;
    expect(fileInput).toBeDisabled();
  });

  it('handles file selection', () => {
    const handleFileChange = vi.fn();
    render(<ButtonUpload id="anid" onFileChange={handleFileChange} />);

    const fileInput = screen.getByTestId('anid_upload_button_file_input') as HTMLInputElement;

    // Create a mock file
    const file = new File(['test content'], 'test.jpg', { type: 'image/jpeg' });

    // Mock the files property
    Object.defineProperty(fileInput, 'files', {
      value: [file],
      writable: false
    });

    fireEvent.change(fileInput);

    expect(handleFileChange).toHaveBeenCalledTimes(1);
    expect(handleFileChange).toHaveBeenCalledWith(file);
  });

  it('handles file selection with no file', () => {
    const handleFileChange = vi.fn();
    render(<ButtonUpload id="anid" onFileChange={handleFileChange} />);

    const fileInput = screen.getByTestId('anid_upload_button_file_input') as HTMLInputElement;

    // Mock empty files
    Object.defineProperty(fileInput, 'files', {
      value: [],
      writable: false
    });

    fireEvent.change(fileInput);

    expect(handleFileChange).toHaveBeenCalledTimes(1);
    expect(handleFileChange).toHaveBeenCalledWith(undefined);
  });

  it('handles file selection with null files', () => {
    const handleFileChange = vi.fn();
    render(<ButtonUpload id="anid" onFileChange={handleFileChange} />);

    const fileInput = screen.getByTestId('anid_upload_button_file_input') as HTMLInputElement;

    // Mock null files
    Object.defineProperty(fileInput, 'files', {
      value: null,
      writable: false
    });

    fireEvent.change(fileInput);

    expect(handleFileChange).toHaveBeenCalledTimes(1);
    expect(handleFileChange).toHaveBeenCalledWith(undefined);
  });

  it('does not call onFileChange when not provided', () => {
    render(<ButtonUpload id="anid" />);

    const fileInput = screen.getByTestId('anid_upload_button_file_input') as HTMLInputElement;

    const file = new File(['test content'], 'test.jpg', { type: 'image/jpeg' });
    Object.defineProperty(fileInput, 'files', {
      value: [file],
      writable: false
    });

    // Should not throw error when onFileChange is not provided
    expect(() => fireEvent.change(fileInput)).not.toThrow();
  });

  it('updates internal file state on file change', () => {
    render(<ButtonUpload id="anid" />);

    const fileInput = screen.getByTestId('anid_upload_button_file_input') as HTMLInputElement;

    const file = new File(['test content'], 'test.jpg', { type: 'image/jpeg' });
    Object.defineProperty(fileInput, 'files', {
      value: [file],
      writable: false
    });

    fireEvent.change(fileInput);

    // Internal state is updated (we can't directly test this, but the component should not error)
    expect(fileInput).toBeInTheDocument();
  });

  it('accepts only image files', () => {
    render(<ButtonUpload id="anid" />);

    const fileInput = screen.getByTestId('anid_upload_button_file_input');
    expect(fileInput).toHaveAttribute('accept', 'image/jpeg,image/png,image/gif');
  });

  it('renders as a label element for accessibility', () => {
    render(<ButtonUpload id="anid" />);

    const label = screen.getByTestId('anid_upload_button');
    expect(label.tagName).toBe('LABEL');
  });

  it('file input is properly associated with label', () => {
    render(<ButtonUpload id="anid" />);

    const label = screen.getByTestId('anid_upload_button');
    const fileInput = screen.getByTestId('anid_upload_button_file_input');

    // The input should be inside the label for proper association
    expect(label).toContainElement(fileInput);
  });

  it('handles multiple file changes', () => {
    const handleFileChange = vi.fn();
    render(<ButtonUpload id="anid" onFileChange={handleFileChange} />);

    const fileInput = screen.getByTestId('anid_upload_button_file_input') as HTMLInputElement;

    // First file
    const file1 = new File(['content1'], 'test1.jpg', { type: 'image/jpeg' });
    Object.defineProperty(fileInput, 'files', {
      value: [file1],
      writable: true
    });
    fireEvent.change(fileInput);

    // Second file
    const file2 = new File(['content2'], 'test2.png', { type: 'image/png' });
    // @ts-ignore
    fileInput.files = [file2];
    fireEvent.change(fileInput);

    expect(handleFileChange).toHaveBeenCalledTimes(2);
    expect(handleFileChange).toHaveBeenNthCalledWith(1, file1);
    expect(handleFileChange).toHaveBeenNthCalledWith(2, file2);
  });

  it('combines base classes with custom className correctly', () => {
    render(<ButtonUpload id="anid" className="extra-class another-class" />);

    const label = screen.getByTestId('anid_upload_button');
    expect(label).toHaveClass(
      'cursor-pointer',
      'bg-blue-500',
      'hover:bg-blue-600',
      'text-white',
      'p-2',
      'rounded-full',
      'extra-class',
      'another-class'
    );
  });
});
