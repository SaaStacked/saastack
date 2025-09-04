import React from 'react';

export interface CardProps {
  /** Card content */
  children: React.ReactNode;
  /** Card title */
  title?: string;
  /** Card subtitle */
  subtitle?: string;
  /** Card variant */
  variant?: 'default' | 'outlined' | 'elevated';
  /** Padding size */
  padding?: 'none' | 'sm' | 'md' | 'lg';
  /** Clickable card */
  clickable?: boolean;
  /** Click handler */
  onClick?: () => void;
  /** Additional CSS classes */
  className?: string;
}

const Card: React.FC<CardProps> = ({
  children,
  title,
  subtitle,
  variant = 'default',
  padding = 'md',
  clickable = false,
  onClick,
  className = ''
}) => {
  const baseClasses = 'bg-white rounded-lg transition-all';

  const variantClasses = {
    default: 'border border-gray-200',
    outlined: 'border-2 border-gray-300',
    elevated: 'shadow-lg border border-gray-100'
  };

  const paddingClasses = {
    none: '',
    sm: 'p-3',
    md: 'p-4',
    lg: 'p-6'
  };

  const clickableClasses = clickable ? 'cursor-pointer hover:shadow-md hover:border-gray-300' : '';

  const classes = [baseClasses, variantClasses[variant], paddingClasses[padding], clickableClasses, className]
    .filter(Boolean)
    .join(' ');

  const CardComponent = clickable ? 'button' : 'div';

  return (
    <CardComponent
      className={classes}
      onClick={clickable ? onClick : undefined}
      type={clickable ? 'button' : undefined}
    >
      {(title || subtitle) && (
        <div className={`${padding !== 'none' ? 'mb-3' : 'p-4 pb-3'}`}>
          {title && <h3 className="text-lg font-semibold text-gray-900 mb-1">{title}</h3>}
          {subtitle && <p className="text-sm text-gray-600">{subtitle}</p>}
        </div>
      )}
      <div className={title || subtitle ? (padding === 'none' ? 'px-4 pb-4' : '') : ''}>{children}</div>
    </CardComponent>
  );
};

export default Card;
