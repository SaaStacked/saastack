import React from 'react';
import { createComponentId, toClasses } from '../Components';


export interface CardProps {
  className?: string;
  id?: string;
  children: React.ReactNode;
  title?: string;
  align?: 'middle' | 'top';
  width?: 'default' | 'wide' | 'full';
  'title-align'?: 'left' | 'center' | 'right';
}

// Creates a page to display a form in the middle of the screen
function FormPage({
  className,
  id,
  children,
  title,
  align = 'middle',
  width = 'default',
  'title-align': titleAlign = 'center'
}: CardProps) {
  const baseClasses = 'flex items-center justify-center';
  const sizeClasses = align === 'top' ? 'items-start pt-16' : 'items-center';
  const widthClasses = {
    default: 'w-11/12 md:w-3/5',
    wide: 'w-11/12 md:w-4/5',
    full: 'w-12/12'
  };

  const classes = toClasses([baseClasses, sizeClasses, className]);
  const titleClasses = `text-xl text-${titleAlign} font-bold text-neutral-900 dark:text-neutral-50 text-center mb-6`;
  const componentId = createComponentId('card', id);

  return (
    <>
      <div className={classes} data-testid={componentId}>
        <div className={`rounded-lg ${widthClasses[width]}`}>
          {title && <h1 className={titleClasses}>{title}</h1>}
          {children}
        </div>
      </div>
    </>
  );
}

export default FormPage;
