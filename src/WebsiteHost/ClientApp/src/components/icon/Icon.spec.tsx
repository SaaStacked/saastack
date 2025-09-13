import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import Icon from './Icon';

describe('Icon', () => {
  it('renders FontAwesome icon with default props', () => {
    render(<Icon symbol="camera" />);

    const fontAwesomeIcon = screen.getByTestId('icon_symbol');
    expect(fontAwesomeIcon).toBeDefined();
  });

  it('renders all FontAwesome icons correctly', () => {
    const fontAwesomeIcons = [
      'camera',
      'car',
      'calendar',
      'check-circle-fill',
      'info-circle-fill',
      'exclamation-triangle-fill',
      'logout',
      'avatar',
      'bars',
      'chevron-left',
      'chevron-right',
      'chevron-up',
      'chevron-down',
      'check',
      'cross',
      'arrow-left-circle',
      'show-password',
      'hide-password',
      'moon',
      'user',
      'building',
      'plus'
    ];

    fontAwesomeIcons.forEach((icon) => {
      const { container } = render(<Icon symbol={icon as any} />);
      const svgIcon = container.querySelector('svg[data-icon]');
      expect(svgIcon).toBeDefined();
    });
  });

  it('renders placeholder icon correctly', () => {
    render(<Icon symbol="placeholder" size={30} />);

    const svgElement = screen.getByTestId('icon_placeholder');
    expect(svgElement?.getAttribute('width')).toBe('30');
    expect(svgElement?.getAttribute('height')).toBe('30');

    const rectElement = svgElement?.querySelector('rect');
    expect(rectElement?.getAttribute('width')).toBe('30');
    expect(rectElement?.getAttribute('height')).toBe('30');
  });

  it('renders bigCross icon correctly', () => {
    render(<Icon symbol="bigCross" size={25} />);

    const svgElement = screen.getByTestId('icon_bigCross');
    expect(svgElement?.getAttribute('width')).toBe('25');
    expect(svgElement?.getAttribute('height')).toBe('25');
    expect(svgElement?.getAttribute('viewBox')).toBe('0 0 12 11');

    const pathElement = svgElement?.querySelector('path');
    expect(pathElement).toBeDefined();
  });

  it('returns null for unknown symbol', () => {
    const { container } = render(<Icon symbol={'unknown' as any} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders FontAwesome icon with correct attributes', () => {
    render(<Icon symbol="camera" size={40} />);

    const svgIcon = screen.getByTestId('icon_symbol');
    expect(svgIcon?.getAttribute('width')).toBe('40');
    expect(svgIcon?.getAttribute('height')).toBe('40');
    expect(svgIcon?.style.height).toBe('40px');
    expect(svgIcon?.style.display).toBe('block');
  });
});
