import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import Alert from './Alert';


describe('Alert', () => {
  it('when no message and no children are provided, renders nothing', () => {
    const { container } = render(<Alert id="anid" />);
    expect(container.firstChild).toBeNull();
  });

  it('renders alert with message', () => {
    render(<Alert id="anid" message="amessage" />);

    expect(screen.getByTestId('anid_alert')).toBeDefined();
    expect(screen.getByTestId('anid_alert_message')).toBeDefined();
    expect(screen.getByTestId('anid_alert_message').textContent).toBe('amessage');
  });

  it('renders alert with children and message', () => {
    render(
      <Alert id="anid" message="amessage">
        <span>Child content</span>
      </Alert>
    );

    expect(screen.getByTestId('anid_alert_children')).toBeDefined();
    expect(screen.getByTestId('anid_alert_children').textContent).toBe('Child content');
    expect(screen.getByTestId('anid_alert_message')).toBeDefined();
    expect(screen.getByTestId('anid_alert_message').textContent).toBe('amessage');
  });

  it('renders alert with title', () => {
    render(<Alert id="anid" title="atitle" message="amessage" />);

    expect(screen.getByTestId('anid_alert_title')).toBeDefined();
    expect(screen.getByTestId('anid_alert_title').textContent).toBe('atitle');
  });

  it('when not provided, does not render title', () => {
    render(<Alert id="anid" message="amessage" />);

    expect(screen.queryByTestId('anid_alert_title')).toBeNull();
  });

  it('when no type is specified, defaults to info type', () => {
    render(<Alert id="anid" message="amessage" />);

    const alertElement = screen.getByTestId('anid_alert');
    expect(alertElement.className).toContain('border-info-600');
    expect(alertElement.className).toContain('bg-info-100');
  });

  it('applies correct styles for error type', () => {
    render(<Alert id="anid" type="error" message="Error message" />);

    const alertElement = screen.getByTestId('anid_alert');
    expect(alertElement.className).toContain('border-error-600');
    expect(alertElement.className).toContain('bg-error-100');
  });

  it('applies correct styles for success type', () => {
    render(<Alert id="anid" type="success" message="Success message" />);

    const alertElement = screen.getByTestId('anid_alert');
    expect(alertElement.className).toContain('border-success-600');
    expect(alertElement.className).toContain('bg-success-100');
  });

  it('applies correct styles for warning type', () => {
    render(<Alert id="anid" type="warning" message="Warning message" />);

    const alertElement = screen.getByTestId('anid_alert');
    expect(alertElement.className).toContain('border-warning-600');
    expect(alertElement.className).toContain('bg-warning-100');
  });

  it('handles null message', () => {
    render(<Alert id="anid" message={null} />);
    expect(screen.queryByTestId('anid_alert')).toBeNull();
  });

  it('renders with children only (no message)', () => {
    render(
      <Alert id="anid">
        <div>achild</div>
      </Alert>
    );

    expect(screen.getByTestId('anid_alert')).toBeDefined();
    expect(screen.getByTestId('anid_alert_children')).toBeDefined();
    expect(screen.queryByTestId('anid_alert_message')).toBeNull();
  });

  it('renders complex children content', () => {
    render(
      <Alert id="anid" type="success">
        <div>
          <p>somecontent</p>
          <button>abutton</button>
        </div>
      </Alert>
    );

    const childrenElement = screen.getByTestId('anid_alert_children');
    expect(childrenElement.textContent).toContain('somecontent');
    expect(childrenElement.textContent).toContain('abutton');
  });
});
